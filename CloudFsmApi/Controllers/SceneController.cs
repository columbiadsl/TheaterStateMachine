#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CloudFsmApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{api-version:apiVersion}/[controller]")]
    public class SceneController : Controller
    {
        private readonly IDownlinkManager _downlinkManager;
        private readonly ILogger<SceneController> _log;
        private readonly ISceneMgr _sceneMgr;

        public SceneController(ILogger<SceneController> log, ISceneMgr sceneMgr, IDownlinkManager downlinkManager)
        {
            _log = log;
            _sceneMgr = sceneMgr;
            _downlinkManager = downlinkManager;
        }


        /// <summary>
        /// Get the character data
        /// </summary>
        /// <returns>json with character data</returns>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("characterconfig")]
        public async Task<IActionResult> GetConfigAsync()
        {
            string resp = await _sceneMgr.ReadCharactersFromBlobAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, Character>>(resp);

            return Json(json);
        }

        /// <summary>
        /// Get the scene scene and step that are currently executing.
        /// </summary>
        /// <returns>json with CurrentScene and CurrentStep; if show isn't running, CurrentScene = "Application is not running"</returns>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("currentscene")]
        public IActionResult GetCurrentScene()
        {
            var scene = _sceneMgr.CurrentSceneName;
            if (string.IsNullOrEmpty(scene))
                scene = "Application is not running";
            string step = _sceneMgr.CurrentStepDescription;

            var resp = new Dictionary<string, string>
            {
                { "CurrentScene", scene },
                { "CurrentStep" , step}
            };

            return Json(resp);
        }

        /// <summary>
        /// Get the json defining the show scenes and steps (the main state machine definition file).
        /// </summary>
        /// <returns>json file defining the scenes and steps for the state machine.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("scene")]
        public async Task<IActionResult> GetScene()
        {
            var body = await _sceneMgr.ReadScenesFromBlobAsync();
            var scene = JsonConvert.DeserializeObject<Dictionary<string, Scene>>(body);
            return Json(scene);
        }

        /// <summary>
        /// Jump immediately to the specified scene and step.
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="step"></param>
        /// <returns>json with scene and step executing after the jump</returns>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("jump/{scene}/{step}")]
        public IActionResult JumpTo(string scene, string step)
        {
            DoScene(scene, step);
            return GetCurrentScene();
        }

        /// <summary>
        /// Report when a lantern enters a beacon area.
        /// </summary>
        /// <param name="lanternID"></param>
        /// <param name="beaconID"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("onBeaconChange/{lanternID}/{beaconID}")]
        public async Task<IActionResult> OnBeaconChange(string lanternID, string beaconID)
        {
            try
            {
                await OnBeaconChangeInternal(lanternID, beaconID);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get the json data file mapping lanterns to characters.
        /// </summary>
        /// <returns>json file containing lantern to character mappings</returns>
        [HttpGet]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("lanternToCharacter")]
        public async Task<IActionResult> ReadLanternTriggerAsync()
        {
            var resp = await _sceneMgr.ReadLanternToCharacterFromBlobAsync();
            var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);

            return Json(json);
        }


        /// <summary>
        /// Start running the state machine
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("run")]
        public async Task<IActionResult> RunAsync()
        {
            string scenes = await _sceneMgr.ReadScenesFromBlobAsync();

            //Scenes
            if (string.IsNullOrEmpty(scenes))
                scenes = System.IO.File.ReadAllText(@"Data\scene.json");
            _sceneMgr.LoadScenes(JsonConvert.DeserializeObject<Dictionary<string, Scene>>(scenes));

            //Characters
            string characters = await _sceneMgr.ReadCharactersFromBlobAsync();
            if (string.IsNullOrEmpty(characters))
                characters = System.IO.File.ReadAllText(@"Data\characters.json");
            _sceneMgr.LoadCharacters(characters);

            //Lantern2Characters
            string l2c = await _sceneMgr.ReadLanternToCharacterFromBlobAsync();
            _sceneMgr.LoadLanternToCharacter(l2c);

            _ = _sceneMgr.RunAsync();

            await Task.Delay(5000); //let it spin to get the CurrentState

            var resp = new Dictionary<string, string>
            {
                { "Scene", _sceneMgr.CurrentSceneName }
            };

            return Json(resp);
        }

        /// <summary>
        /// Deprecated.  Takes json to be executed immediately.
        /// </summary>
        /// <param name="script">json list containing these possible properties as instructions: R|Restart the play, T|Send beacon event with T.BeaconId, T.LanternId, S|Jump
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("simulate")]
        public async Task<IActionResult> Simulate([FromBody] List<Script> script)
        {
            //Read lines and for each line execute one of the following action
            //R|Restart the play
            //E|Expecting a script with BeaconId, LanternIn
            //S|Jump
            var resp = new List<string>();

            foreach (var inst in script)
            {
                if (inst.T != null)
                    resp.Add(await DoEvent(inst.T.LanternId, inst.T.BeaconId));
                else if (!string.IsNullOrEmpty(inst.R))
                {
                    switch (inst.R)
                    {
                        case "R":
                            resp.Add(DoReset("Reset")); break;
                        case "S":
                            resp.Add(DoScene(inst.Scene, inst.Step)); break;
                    }
                }
            }

            return Json(resp);
        }

        /// <summary>
        /// Same as "run" but operating on the provided scene definition json file.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("test")]
        public async Task<IActionResult> TestAsync([FromBody] Dictionary<string, Scene> json)
        {
            Dictionary<string, Scene> scenes = json;

            _sceneMgr.LoadScenes(scenes);

            string characters = await _sceneMgr.ReadCharactersFromBlobAsync();
            if (string.IsNullOrEmpty(characters))
                characters = System.IO.File.ReadAllText(@"Data\characters.json");
            _sceneMgr.LoadCharacters(characters);

            //this needs to be after loading characters
            string l2c = await _sceneMgr.ReadLanternToCharacterFromBlobAsync();
            _sceneMgr.LoadLanternToCharacter(l2c);

            _ = _sceneMgr.RunAsync();

            await Task.Delay(5000); //let it spin to get the CurrentState

            var resp = new Dictionary<string, string>
            {
                { "Scene", _sceneMgr.CurrentSceneName }
            };

            return Json(resp);
        }

        /// <summary>
        /// Change (or create new) a character's mapping to a lantern id
        /// </summary>
        /// <param name="characterName"></param>
        /// <param name="lanternId"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("update/{characterName}/{lanternId}")]
        public async Task<IActionResult> UpdateCharacterLantern(string characterName, string lanternId)
        {
            try
            {
                // get current character map from memory
                var json = await _sceneMgr.ReadLanternToCharacterFromBlobAsync();
                Dictionary<string, string> l2c = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                //check if the entered key exists, reverse lookup the dictionary
                foreach (var kvp in l2c)
                {
                    if (kvp.Value == characterName)
                    {
                        l2c.Remove(kvp.Key);
                        break;
                    }
                }

                l2c.Add(lanternId, characterName);
                await _sceneMgr.WriteLanternToCharacterToBlobAsync(l2c);

                var characters = _sceneMgr.Characters;
                if (characters != null && characters.Any())
                {
                    characters[characterName].LanternId = lanternId;
                    await _sceneMgr.WriteCharactersToBlobAsync(characters);
                }

                //return new map
                return Json(l2c);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Upload json with character config
        /// </summary>
        /// <param name="characters">Character configuration json file</param>
        /// <returns>"Ok" if successful</returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("characterconfig")]
        public async Task<IActionResult> WriteConfigAsync([FromBody] Dictionary<string, Character> characters)
        {
            if (characters == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "Invalid JSON format");
            if (characters.TryGetValue("additionalProp1", out Character value))
                return StatusCode(StatusCodes.Status500InternalServerError, "Don't use the sample provided by the swagger interface, read the characterconfig instead");

            await _sceneMgr.WriteCharactersToBlobAsync(characters);

            var resp = new Dictionary<string, string>
                {
                    { "Load", "OK" }
                };

            return Json(resp);
        }

        /// <summary>
        /// Upload json file mapping lanterns to characters
        /// </summary>
        /// <param name="lantern2Character">json file containing lantern to character mappings</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("lanternToCharacter")]
        public async Task<IActionResult> WriteLantern2Character([FromBody] Dictionary<string, string> lantern2Character)
        {
            if (lantern2Character == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "Invalid JSON format");

            await _sceneMgr.WriteLanternToCharacterToBlobAsync(lantern2Character);

            var resp = new Dictionary<string, string>
                {
                    { "Load", "OK" }
                };

            return Json(resp);
        }

        /// <summary>
        /// Upload a json file defining the scenes and steps for the show state machine.
        /// </summary>
        /// <param name="scenes">json file with scene definitions</param>
        /// <returns>Load Ok if upload succeeded.</returns>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 400)]
        [Produces("application/json")]
        [Route("scene")]
        public async Task<IActionResult> WriteScene([FromBody] Dictionary<string, Scene> scenes)
        {
            if (scenes == null)
                return StatusCode(StatusCodes.Status500InternalServerError, "Invalid JSON format");
            if (scenes.TryGetValue("additionalProp1", out Scene value))
                return StatusCode(StatusCodes.Status500InternalServerError, "Don't use the sample provided by the swagger interface, read the scene instead");

            await _sceneMgr.WriteScenesToBlobAsync(scenes);
            var resp = new Dictionary<string, string>
                {
                    { "Load", "OK" }
                };

            return Json(resp);
        }

        private async Task<string> DoEvent(string lanternId, string beaconId)
        {
            Debug.WriteLine($"{lanternId} {beaconId}");

            var state = await OnBeaconChangeInternal(lanternId, beaconId);
            return JsonConvert.SerializeObject(state);
        }

        private string DoReset(string v)
        {
            Debug.WriteLine("DoReset");

            _sceneMgr.Reset();
            _sceneMgr.RunAsync(false).Wait();

            return $"Scene changed to [{_sceneMgr.CurrentSceneName}]";
        }

        private string DoScene(string sceneName, string stepName)
        {
            Debug.WriteLine("DoScene");
            var from = _sceneMgr.CurrentSceneName;
            _sceneMgr.JumpTo(sceneName, stepName);
            return $"Scene changed from [{from}] to [{_sceneMgr.CurrentSceneName} {stepName}]";
        }

        //Swagger breaks if this is public
        private IActionResult Index()
        {
            return View();
        }

        private async Task<List<Command>> OnBeaconChangeInternal(string lanternId, string beaconId)
        {
            var sceneName = _sceneMgr.CurrentSceneName;
            if (string.IsNullOrEmpty(sceneName))
            {
                throw new Exception("Character FSM is not running");
            }

            //The Function needs to
            return await _sceneMgr.OnBeaconChange(lanternId, beaconId).ConfigureAwait(false);
        }
    }
}
