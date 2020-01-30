#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Newtonsoft.Json;

namespace Model
{
    /// <summary>
    /// Commands will be sent back to the show's local server when a step is executed.
    /// Each step can have a list of commands, and each command can trigger one or more
    /// effect, and has a duration and a padding time (extra time to wait after the effects
    /// are executed.)
    /// 
    /// A Command is specified in the json like so:
    /// {
    ///  "vibrate": {
    ///    "type": "count",
    ///    "value": 1
    ///  },
    ///  "cue": {
    ///    "type": 31001,
    ///    "value": "start"
    ///  },
    ///  "sound": null,
    ///  "light": {
    ///    "type": "on",
    ///    "value": 60
    ///  },
    ///  "specialText": null,
    ///  "durationSec": 0,
    ///  "paddingSec": 0
    /// }
    /// 
    /// This model handles "vibrate", "cue", "sound" and "light" as pre-defined effects. The 
    /// state machine simply sends a json string with the effects back to the proxy server, so
    /// you can add any additional named effect that you want, simply by adding a property 
    /// that name to this class. (See example below.)
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Command
    {
        public static Command Default { get { return new Command() { Sound = null, Light = null, Cue = null, Vibrate = null}; } }

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

        // The following code demonstrates how to add any additional type of effect that
        // you want to be able to specify in the json and send to the show's server.
        // Simply define a JsonProperty, make it public, give it the type Effect.
        /*
        [JsonProperty]
        public Effect AnyNewEffect { get; set; }
        */
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
