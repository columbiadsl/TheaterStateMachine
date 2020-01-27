#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Newtonsoft.Json;

namespace Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Telemetry
    {
        [JsonProperty]
        public string BeaconId { get; set; }

        [JsonProperty]
        public string LanternId { get; set; }

        public static Telemetry FromString(string json)
        {
            return JsonConvert.DeserializeObject<Telemetry>(json);
        }

        public static string Random()
        {
            var tele = new Telemetry
            {
                LanternId = RandomString.Next(8),
                BeaconId = RandomString.Next(16)
            };
            return tele.ToJson();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}
