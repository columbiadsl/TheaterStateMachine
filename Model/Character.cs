#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AvaStep
    {
        private DateTime _blockingStart = DateTime.MinValue;

        [JsonProperty]
        public List<Command> Commands { get; set; }

        [JsonProperty]
        public int DurationSec { get; set; }

        public bool IsBlocked { get { return (_blockingStart.AddSeconds(DurationSec) > DateTime.UtcNow); } }

        public void Run()
        {
            _blockingStart = DateTime.UtcNow;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Character
    {
        private readonly List<string> _visited = new List<string>();

        private CharacterScene _currentScene = null;

        public List<Command> CurrentCommands { get; private set; }

        [JsonProperty]
        public string Description { get; set; }

        public bool IsRoaming { get; private set; }

        /// <summary>
        /// Not part of the Json and it gets loaded from the table LanternToCharacter
        /// </summary>
        public string LanternId { get; set; }

        /// <summary>
        /// Not part of the Json as it is the key for the dictionary,
        /// but we populate it during load to help with debug messages
        /// </summary>
        public string Name { get; set; }

        [JsonProperty]
        public Dictionary<string, CharacterScene> Scenes { get; set; }

        public List<Command> OnBeaconChange(string beaconId)
        {
            if (_visited.Contains(beaconId))
                return null;

            if (_currentScene == null)
                return null;

            if (beaconId == "AVA")
            {
                //Now this actor responds to the AVA steps
                IsRoaming = false;
            }

            _visited.Add(beaconId);

            //do we have triggers?
            if (_currentScene.Triggers == null)
                return null;

            if (!_currentScene.Triggers.TryGetValue(beaconId, out List<Command> commands))
                return null;

            CurrentCommands = commands;
            return CurrentCommands;
        }

        public List<Command> OnSceneChanged(string sceneName, string stepName)
        {
            Reset();

            if (!Scenes.TryGetValue(sceneName, out _currentScene))
                return null;

            Debug.WriteLine($"{Name} > Scene = {sceneName}");

            IsRoaming = (_currentScene.Type == SceneTypeT.Roaming);

            return OnStepChanged(stepName);
        }

        public List<Command> OnStepChanged(string stepName)
        {
            //only changes step on Ava
            if (IsRoaming)
                return null;

            if (_currentScene == null || _currentScene.Steps == null)
                return null;

            AvaStep step;
            if (!_currentScene.Steps.TryGetValue(stepName, out step))
                return null;

            Debug.WriteLine($"{Name} > Step = {stepName}");

            step.Run();

            CurrentCommands = step.Commands;
            return CurrentCommands;
        }

        public void Reset()
        {
            _visited.Clear();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class CharacterScene
    {
        [JsonProperty]
        public string Description { get; set; }

        /// <summary>
        /// If AVA occurs, then play these stepts when a timer triggers
        /// </summary>
        [JsonProperty]
        public Dictionary<string, AvaStep> Steps { get; set; }

        /// <summary>
        /// Beacons that trigger only during roaming
        /// </summary>
        [JsonProperty]
        public Dictionary<string, List<Command>> Triggers { get; set; }

        [JsonProperty]
        public SceneTypeT Type { get; set; }
    }
}
