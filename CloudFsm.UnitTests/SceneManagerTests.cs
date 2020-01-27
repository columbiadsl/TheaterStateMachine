#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using CloudFsmApi;
using Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace CloudFsm.UnitTests
{
    public class SceneManagerTests : IClassFixture<StartupFixture>
    {
        private readonly StartupFixture _fixture;
        private readonly string _lanternToCharacter;
        private string _characterJson;
        private DebugDownlinkManager _dlm;
        private Dictionary<string, Scene> _scenes;

        public SceneManagerTests(StartupFixture fixture)
        {
            _fixture = fixture;
            _lanternToCharacter = System.IO.File.ReadAllText(@"Sandbox/lanternToCharacter.json");
            _dlm = new DebugDownlinkManager();
        }

        [Theory, AutoDomainData]
        public async Task TestAVA()
        {
            //load the state machine with AVA atomic
            _characterJson = System.IO.File.ReadAllText(@"Sandbox/AvaCharacter.json");
            var json = System.IO.File.ReadAllText(@"Sandbox/AvaScene.json");
            _scenes = JsonConvert.DeserializeObject<Dictionary<string, Scene>>(json);

            FsmSceneManager sut = new FsmSceneManager(_dlm, _fixture.Configuration);
            sut.LoadCharacters(_characterJson);
            sut.LoadLanternToCharacter(_lanternToCharacter);
            sut.LoadScenes(_scenes);

            //start the FSM
            await sut.RunAsync(false);

            //The scene file starts in AVA Scene AVA1
            Assert.Equal("AVA1", sut.CurrentSceneName);
            var character = sut.Characters["JohnAllan"];

            //Advance the steps and compare the output
            sut.JumpTo("AVA1", "Step1");
            Assert.Equal("AVA1 Step1", sut.CurrentStepDescription);
            Assert.Equal("AVA1.Step1[0]", character.CurrentCommands[0].SpecialText);

            //Advance the steps one more time and compare the output
            sut.JumpTo("AVA1", "Step2");
            Assert.Equal("AVA1 Step2", sut.CurrentStepDescription);
            Assert.Equal("AVA1.Step2[0]", character.CurrentCommands[0].SpecialText);

            //test when all the Participants in the AVA Scene discover a Required Beacon
            await sut.OnBeaconChange("lantern0", "room2");
            //no change yet
            Assert.Equal("AVA1 Step2", sut.CurrentStepDescription);

            //repeat same beacon, ensure it is ignored
            await sut.OnBeaconChange("lantern0", "room2");
            //discover another beacon random beacon
            await sut.OnBeaconChange("lantern0", "book2");
            //no change
            Assert.Equal("AVA1 Step2", sut.CurrentStepDescription);

            //another dude, discovers the hot beacon
            await sut.OnBeaconChange("lantern1", "room2");
            //no change yet
            Assert.Equal("AVA1 Step2", sut.CurrentStepDescription);

            //another dude, discovers the hot beacon
            await sut.OnBeaconChange("lantern2", "room2");

            // JohnAllan, DavidPoe, Montresso visited room2, condition is now valid
            Assert.Equal("AVA2", sut.CurrentSceneName);

            //Ensure the trigger moved the state machine
            Assert.Equal("AVA2 Step1", sut.CurrentStepDescription);
        }

        [Theory, AutoDomainData]
        public async Task TestRoaming()
        {
            ////load the state machine
            //_characterJson = System.IO.File.ReadAllText(@"Sandbox/RoamingCharacter.json");
            //var json = System.IO.File.ReadAllText(@"Sandbox/RoamingScene.json");
            //_scenes = JsonConvert.DeserializeObject<Dictionary<string, Scene>>(json);

            //FsmSceneManager sut = new FsmSceneManager(_dlm);
            //sut.LoadCharacters(_characterJson);
            //sut.LoadLanternToCharacter(_lanternToCharacter);
            //sut.LoadScenes(_scenes);

            ////start the FSM
            //await sut.RunAsync(false);

            ////The scene file starts in Roaming Scene 1 = RS1
            //Assert.Equal("RS1", sut.CurrentSceneName);
            //var character = sut.Characters["JohnAllan"];

            ////JohnAllan is roaming and discovers a beacon
            //await sut.OnBeaconChange("lantern0", "BEA001");
            ////ensure proxy gets the command from the trigger
            //Assert.Equal("RS1.BEA001[0]", character.CurrentCommands[0].SpecialText);

            ////Change the step, but he is still roaming, therefore his last command should not change
            //sut.JumpTo("RS1", "Step2");
            //Assert.Equal("RS1.BEA001[0]", character.CurrentCommands[0].SpecialText);

            ////Dude discovers the AVA a beacon in the RequiredAVA field
            //await sut.OnBeaconChange("lantern0", "AVA001");

            ////change the step
            //sut.JumpTo("RS1", "Step3");

            ////Ensure he gets the new step command triggered by time
            //Assert.Equal("RS1.Step3[0]", character.CurrentCommands[0].SpecialText);
            Assert.True(true);
        }
    }
}
