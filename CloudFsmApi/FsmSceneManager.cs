#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using CloudFsmApi.Config;
using CloudFsmApi.Helpers;
using Microsoft.Extensions.Options;
using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace CloudFsmApi
{
    public sealed class FsmSceneManager : ISceneMgr
    {
        private readonly IDownlinkManager _downlink;

        public Dictionary<string, Character> Characters { get; private set; }
        public Dictionary<string, string> Lantern2Character { get; private set; }

        private readonly Timer _tmr;
        private Dictionary<string, Scene> _scenes;

        public string CurrentSceneName { get; private set; }
        public bool Running { get; private set; }

        private Step _currentStep;
        public string CurrentStepDescription { get { return _currentStep?.Description; } }

        private readonly BlobHelper _helper;

        private readonly StorageConfig _config;

        public FsmSceneManager(IDownlinkManager downlink, IOptions<StorageConfig> config)
        {
            _config = config.Value;
            _helper = new BlobHelper(_config);

            _downlink = downlink;
            Lantern2Character = new Dictionary<string, string>();

            //Load LanternToCharacter table if Onboarding is disabled

            // Prevent outside instantiation
            _tmr = new Timer
            {
                Enabled = false,
                AutoReset = false
            };
            _tmr.Elapsed += _tmr_Elapsed;

            Running = false;
        }

        /// <summary>
        /// To stop the Scene state machine pass a scene name that is not defined in the JSON
        /// Example1 Denial, Step0
        /// Example2 End, anything
        /// </summary>
        /// <param name="sceneName">END to terminate</param>
        /// <param name="stepName"></param>
        private void TriggerJumpTo(string onNextTrigger)
        {
            var sceneName = CurrentSceneName;
            string stepName;

            var tokens = onNextTrigger.Split(':');
            if (tokens.Length > 1)
            {
                sceneName = tokens[0];
                stepName = tokens[1];
            }
            else
            {
                stepName = _currentStep?.OnTriggerNext;
            }

            JumpTo(sceneName, stepName);
        }

        public void JumpTo(string sceneName, string stepName)
        {
            if (!Running)
            {
                return;
            }

            Debug.WriteLine($"Jump to {sceneName} {stepName}");
            bool sceneChanged = sceneName != CurrentSceneName;
            if (sceneChanged)
            {
                _tmr.Stop();
                _tmr.Enabled = false;

                foreach (var actor in Characters.Values)
                {
                    actor.OnSceneChanged(sceneName, stepName);
                }
            }
            else
            {
                foreach (var actor in Characters.Values)
                {
                    actor.OnStepChanged(stepName);
                }
            }

            if (_scenes.TryGetValue(sceneName, out Scene scene))
            {
                CurrentSceneName = sceneName;
                Running = ManageStepChangedAsync(scene, stepName).GetAwaiter().GetResult();
                if (!Running)
                {
                    return;
                }

                if (scene.DurationSec > 0 && sceneChanged)
                {
#if DEBUG
                    //run 10x faster during testing
                    _tmr.Interval = scene.DurationSec * 100;
#else
                    _tmr.Interval = scene.DurationSec * 1000;
#endif

                    Debug.WriteLine($"{sceneName}\tScene expires in {scene.DurationSec} sec");
                    _tmr.Enabled = true;
                    _tmr.Start();
                }
            }
            else
            {
                Running = false;
            }
        }

        private async Task<bool> ManageStepChangedAsync(Scene scene, string nextStep)
        {
            if (_currentStep != null)
                _currentStep.Stop();

            var step = scene.JumpToStep(nextStep);
            if (step == null)
            {
                Debug.WriteLine($"Trying to jump to setp {nextStep} failed");
                return false;
            }

            _currentStep = step;
            await _downlink.SendCloudToOtherdeviceMethodAsync(_currentStep.Commands);

            Debug.WriteLine($"ManageStepChangedAsync: {nextStep}");
            _currentStep.Start(OnStepExpired, scene.Participants);

            //We also need to send to each character the commands for the new step
            foreach (var kvp in Characters)
            {
                var character = kvp.Value;

                //Only characters that participate in AVA get commands back when a step changes
                if (character.IsRoaming)
                    continue;

                if (!character.Scenes.TryGetValue(CurrentSceneName, out CharacterScene sceneElement))
                    continue;

                if (null == sceneElement)
                    continue;

                if (null == sceneElement.Steps || !sceneElement.Steps.TryGetValue(nextStep, out AvaStep stepElement))
                    continue;

                if (null == stepElement)
                    continue;

                Debug.WriteLine($"Playing commands for: {kvp.Key}");
                await _downlink.SendCloudToLanternMethodAsync(character.LanternId, stepElement.Commands);
            }

            return true;
        }

        private void OnStepExpired(Step step)
        {
            Debug.WriteLine($"OnStepExpired {step.Description}");
            if (string.IsNullOrEmpty(step.OnTriggerNext))
                return;

            //move to the next scene
            TriggerJumpTo(step.OnTriggerNext);
        }

        /// <summary>
        /// Scene timer expired
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _tmr_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_currentStep == null || string.IsNullOrEmpty(_currentStep.OnTriggerNext))
                return;

            TriggerJumpTo(_currentStep.OnTriggerNext);
        }

        public void LoadScenes(Dictionary<string, Scene> json)
        {
            _scenes = json;
        }

        public void LoadCharacters(string characterJson)
        {
            if (string.IsNullOrEmpty(characterJson))
            {
                characterJson = System.IO.File.ReadAllText(@"Data\Characters.json");
            }
            Characters = JsonConvert.DeserializeObject<Dictionary<string, Character>>(characterJson);
        }

        /// <summary>
        /// forever loop in a background task
        /// </summary>
        /// <param name="continous"></param>
        /// <returns></returns>
        public async Task RunAsync(bool continous = true)
        {
            Running = true;
            if (_tmr.Enabled)
            {
                _tmr.Enabled = false;
            }

            var kvp = _scenes.First();
            JumpTo(kvp.Key, kvp.Value.Steps.First().Key);

            while (Running && continous)
            {
                await Task.Delay(1000);
            }

            if (continous)
                CurrentSceneName = string.Empty;
        }

        /// <summary>
        /// Every time the Proxy sends telemetry, forward the payload to the SCENE step state machine
        /// It returns the Collection of commands for the step
        /// </summary>

        public async Task<List<Command>> OnBeaconChange(string lanternId, string beaconId)
        {
            Debug.WriteLine($"Beacon Trigger {lanternId} {beaconId}");

            List<Command> scriptResponse = null;

            if (!Lantern2Character.TryGetValue(lanternId, out string character))
            {
                throw new Exception("Lantern not mapped");
            }

            //Check the When All condition, if it is true it will trigger the callback
            string triggerNext = _currentStep.OnBeaconOccurred(character, beaconId);
            if (string.IsNullOrEmpty(triggerNext))
            {
                if (beaconId == _scenes[CurrentSceneName].RequiredAVA)
                {
                    //for the character condition to stop roaming
                    beaconId = "AVA";
                }

                if (!Characters.TryGetValue(character, out Character cs))
                    return null;

                var characterCommands = cs.OnBeaconChange(beaconId);
                if (null != characterCommands)
                {
                    //command to the device
                    await _downlink.SendCloudToLanternMethodAsync(lanternId, characterCommands);
                    scriptResponse = characterCommands;
                }

                //this is roaming but also move the step
                if (_currentStep.RequiredBeacons != null && _currentStep.RequiredBeacons.Contains(beaconId))
                {
                    TriggerJumpTo(_currentStep.OnTriggerNext);
                }
            }
            else if (triggerNext != "BLOCKED")
            {
                TriggerJumpTo(triggerNext);
            }

            return scriptResponse;
        }

        public async Task<string> ReadCharactersFromBlobAsync()
        {
            var sCharacters = await _helper.ReadFromBlobAsync(_config.CharacterFilePath);
            return sCharacters;
        }

        public async Task WriteCharactersToBlobAsync(Dictionary<string, Character> characterConfig)
        {
            var newConfig = JsonConvert.SerializeObject(characterConfig, Formatting.None);
            await _helper.WriteToBlobAsync(_config.CharacterFilePath, newConfig);

            LoadCharacters(newConfig);
        }

        public async Task<string> ReadScenesFromBlobAsync()
        {
            var scenes = await _helper.ReadFromBlobAsync(_config.SceneFilePath);
            return scenes;
        }

        public async Task WriteScenesToBlobAsync(Dictionary<string, Scene> sceneConfig)
        {
            var newConfig = JsonConvert.SerializeObject(sceneConfig, Formatting.None);
            await _helper.WriteToBlobAsync(_config.SceneFilePath, newConfig);

            LoadCharacters(newConfig);
        }

        public async Task<string> ReadLanternToCharacterFromBlobAsync()
        {
            var lt = await _helper.ReadFromBlobAsync(_config.LanternToCharacterMapFilePath);
            return lt;
        }

        public async Task WriteLanternToCharacterToBlobAsync(Dictionary<string, string> lanternToCharacterConfig)
        {
            var newConfig = JsonConvert.SerializeObject(lanternToCharacterConfig, Formatting.None);
            Lantern2Character = lanternToCharacterConfig;
            await _helper.WriteToBlobAsync(_config.LanternToCharacterMapFilePath, newConfig);
        }

        public void LoadLanternToCharacter(string lanternToCharacterConfig)
        {
            Lantern2Character = JsonConvert.DeserializeObject<Dictionary<string, string>>(lanternToCharacterConfig);

            foreach (var kvp in Lantern2Character)
            {
                if (Characters.TryGetValue(kvp.Value, out Character character))
                {
                    character.LanternId = kvp.Key;
                    character.Name = kvp.Value;
                }
                else
                {
                    Debug.WriteLine($"Character {kvp.Value} is missing from the Character table");
                }
            }
        }

        public void Reset()
        {
            Characters.Clear();
            Lantern2Character.Clear();
            _scenes.Clear();
        }
    }
}
