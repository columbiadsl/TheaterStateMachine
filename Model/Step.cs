#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;

namespace Model
{
    public enum LogicT
    {
        LanternTrigger = 0,
        WhenRequiredIDs = 1,
        CharacterTrigger = 2,
        TimerTrigger = 3,
    }

    /// <summary>
    /// Specifies a step within a scene. 
    /// Model also contains state information for the currently executing step.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class Step
    {
        private readonly Timer _timer;
        private readonly Dictionary<string, List<string>> _charactersPerBeacon;

        /// <summary>
        /// Metadata to decorate the JSON file, it is not used by the state machine
        /// </summary>
        [JsonProperty]
        public string Description { get; set; }

        /// <summary>
        /// array of character names/lantern ids
        /// The idea is that ALL users must trip ANY Beacon on the Beacon list to advance a beacon enabled step of a scene. So if users are 1,2,3 and Beacons are A, B,C, then 1A, 2A, 3A is a successful trigger, but so is 1A, 2B, 3C
        /// Example 1               "RequiredID": ["Lan001", "Lan002"],
        /// Example 2               "RequiredID": ["John Allan", "Poe"],
        /// Example 3               "RequiredID": ["ALL"],
        ///
        /// </summary>
        [JsonProperty]
        public List<string> RequiredID { get; set; }

        /// <summary>
        /// Array of beacons, when all the required users visit one of the listed elements
        /// Example                "RequiredBeacons": ["Beacon001", "Beacon002"],
        /// </summary>
        [JsonProperty]
        public List<string> RequiredBeacons { get; set; }

        [JsonProperty]
        public int TimeTriggerSec { get; set; }

        /// <summary>
        /// Name of the next step to jump to after this one completes
        /// </summary>
        [JsonProperty]
        public string OnTriggerNext { get; set; }

        /// <summary>
        /// List of Commands for this step
        /// </summary>
        [JsonProperty]
        public List<Command> Commands { get; set; }

        public bool IsBlocked
        {
            get
            {
                return _timer.Enabled;
            }
        }

        private Action<Step> _stepCompletedCallback;

        public Step()
        {
            _charactersPerBeacon = new Dictionary<string, List<string>>();
            _timer = new Timer
            {
                Enabled = false
            };
            _timer.Elapsed += _timer_Elapsed;
        }

        /// <summary>
        /// This starts executing the step.
        /// The state machine needs to Start each step during transition, otherwise the timers are not running
        /// Its called from the scene, everytime it changes step
        /// Note: Characters don't trigger by step, they are roaming (by scene)
        /// </summary>
        public void Start(Action<Step> stepCompletedCallback, List<string> sceneParticipants)
        {
            // First, stop the previous step
            Stop();

            _charactersPerBeacon.Clear();

            if (RequiredID != null && RequiredID.Count > 0 && RequiredID[0] == "ALL")
                RequiredID = sceneParticipants;

            _stepCompletedCallback = stepCompletedCallback;
            if (TimeTriggerSec > 0)
            {
                Debug.Write(Description);
                Debug.WriteLine($"\tStep expires in {TimeTriggerSec} sec");
#if DEBUG
                //run 10x faster during testing
                _timer.Interval = TimeTriggerSec * 100;
#else
                _timer.Interval = TimeTriggerSec * 1000;
#endif
                _timer.Enabled = true;
                _timer.AutoReset = false;
                _timer.Start();
            }
            else
            {
                _timer.Stop();
            }
        }

        public void Stop()
        {
            _timer.Stop();
            _timer.Enabled = false;
        }

        /// <summary>
        /// Test for condition to trigger a change of step
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="beaconId"></param>
        public string OnBeaconOccurred(string userId, string beaconId)
        {
            return WhenAvaRequiredOnBeaconOccurred(userId, beaconId);
        }

        private string WhenAvaRequiredOnBeaconOccurred(string userId, string beaconId)
        {
            //is this lantern is in the RequiredID conditional list?
            if (RequiredID == null || (!RequiredID.Contains(userId)))
                return string.Empty;

            //is this beacon is in the RequiredBeacons conditional list?
            if (RequiredBeacons == null || (!RequiredBeacons.Contains(beaconId)))
                return string.Empty;

            //if the visit condition completes, fire the trigger
            //pseudo add the beaconId to the lanternId collection (beacons visited by this character)
            //check if all the required users have seen this beacon
            List<string> visitors;
            if (_charactersPerBeacon.TryGetValue(beaconId, out visitors))
            {
                if (visitors.Contains(userId))
                {
                    //repeating a visit, nothing else to do
                    return "BLOCKED";
                }
                visitors.Add(userId);
                if (RequiredID.Count == visitors.Count)
                {
                    //force a short timeout to move to the next step

                    //condition met
                    return this.OnTriggerNext;
                }
            }
            else
            {
                visitors = new List<string>
                {
                    userId
                };
                _charactersPerBeacon.Add(beaconId, visitors);
                return "BLOCKED"; //this is to avoid processing
            }

            return string.Empty;
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Debug.WriteLine("Step expired!");
            //unblock the state
            _timer.Stop();
            _timer.Enabled = false;

            //move to the next step
            _stepCompletedCallback(this);
        }
    }
}
