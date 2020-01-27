#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace Model
{
    /// <summary>
    /// Two types of scenes: 
    /// Ava is our name for scenes that are attached to the main character
    /// Roaming is for scenes that participants can discover through their own explorations
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SceneTypeT
    {
        Ava,
        Roaming
    }

    /// <summary>
    /// Thin DTO
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class Scene
    {
        /// <summary>
        /// Help to document the JSON, but this field is ignored
        /// </summary>
        [JsonProperty]
        public string Description { get; set; }

        /// <summary>
        /// Total Duration of the scene in seconds (resolution is second to enable fast testing)
        /// </summary>
        [JsonProperty]
        public int DurationSec { get; set; }

        /// <summary>
        /// List of Lanterns or Characters that play in this scene
        /// </summary>
        [JsonProperty]
        public List<string> Participants { get; set; }

        /// <summary>
        /// For roaming characters, when they see this beacon
        /// They stop roaming and now they follow the AVA steps
        /// </summary>
        [JsonProperty]
        public string RequiredAVA { get; set; }

        /// <summary>
        /// Sequence of steps with pointer
        /// </summary>
        [JsonProperty]
        public Dictionary<string, Step> Steps { get; set; }

        /// <summary>
        /// AVA or roaming
        /// </summary>
        [JsonProperty]
        public SceneTypeT Type { get; set; }

        public Step First()
        {
            return Steps.First().Value;
        }

        public Step JumpToStep(string key)
        {
            Steps.TryGetValue(key, out Step step);
            return step;
        }
    }
}
