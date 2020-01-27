#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Newtonsoft.Json;

namespace Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Command
    {
        public static Command Default { get { return new Command() { Sound = null, Light = null, Cue = null, Vibrate = null }; } }

        [JsonProperty]
        public Effect Cue { get; set; }

        /// <summary>
        /// The duration for the sound and light commands, during this time,
        /// the state machine is blocked for this Lantern
        /// </summary>
        [JsonProperty]
        public int DurationSec { get; set; }

        [JsonProperty]
        public string LanternID { get; set; }

        /// <summary>
        /// An array of light levels and durations,
        /// The player of the script needs to invoke them sequentially
        /// </summary>
        [JsonProperty]
        public Effect Light { get; set; }

        /// <summary>
        /// Extra time to block after the Duration completes
        /// </summary>
        [JsonProperty]
        public int PaddingSec { get; set; }

        /// <summary>
        /// Label of the sound to play by the Lantern Manager
        /// </summary>
        [JsonProperty]
        public Effect Sound { get; set; }

        /// <summary>
        /// Use this property if the SOC Manager needs any other text that is not yet included
        /// </summary>
        [JsonProperty]
        public string SpecialText { get; set; }

        [JsonProperty]
        public Effect Vibrate { get; set; }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class Effect
    {
        [JsonProperty]
        public string Type { get; set; }

        [JsonProperty]
        public object Value { get; set; }
    }
}
