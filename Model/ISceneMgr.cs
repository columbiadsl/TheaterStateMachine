#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Model
{
    public interface ISceneMgr
    {
        Dictionary<string, Character> Characters { get; }

        string CurrentSceneName { get; }

        string CurrentStepDescription { get; }

        Dictionary<string, string> Lantern2Character { get; }

        void JumpTo(string sceneName, string stepName);

        void LoadCharacters(string characterJson);

        void LoadLanternToCharacter(string lantern2characterConfig);

        void LoadScenes(Dictionary<string, Scene> json);

        Task<List<Command>> OnBeaconChange(string lanternId, string beaconId);

        Task<string> ReadCharactersFromBlobAsync();

        Task<string> ReadLanternToCharacterFromBlobAsync();

        Task<string> ReadScenesFromBlobAsync();

        void Reset();

        Task RunAsync(bool continous = true);

        Task WriteCharactersToBlobAsync(Dictionary<string, Character> characterConfig);

        Task WriteLanternToCharacterToBlobAsync(Dictionary<string, string> lantern2characterConfig);

        Task WriteScenesToBlobAsync(Dictionary<string, Scene> sceneConfig);
    }
}
