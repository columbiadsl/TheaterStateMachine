#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CloudFsm.UnitTests
{
    internal class DebugDownlinkManager : IDownlinkManager
    {
        public Task SendCloudToLanternMethodAsync(string lanternId, List<Command> commands, int? table = null)
        {
            if (commands == null)
                return Task.FromResult(0);
            if (commands.Count == 0)
                return Task.FromResult(0);
            if (string.IsNullOrEmpty(lanternId))
                return Task.FromResult(0);

            foreach (var cmd in commands)
            {
                cmd.LanternID = lanternId;
            }

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var body = JsonConvert.SerializeObject(commands, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.None
            });

            Debug.WriteLine(body);
            return Task.FromResult(0);
        }

        /// <summary>
        /// Other devices in the show like addressable speakers
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public Task SendCloudToOtherdeviceMethodAsync(List<Command> commands)
        {
            if (commands == null)
                return Task.FromResult(0);
            if (commands.Count == 0)
                return Task.FromResult(0);

            // Instead of lanternID: null appearing before scene level commands, could that be lanternID: allID instead? allID is how we call all lanterns, so it’s a safer thing for us to read that a null field.
            foreach (var cmd in commands)
            {
                cmd.LanternID = "allID";
            }

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };

            var body = JsonConvert.SerializeObject(commands, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.None
            });

            Debug.WriteLine(body);
            return Task.FromResult(0);
        }
    }
}
