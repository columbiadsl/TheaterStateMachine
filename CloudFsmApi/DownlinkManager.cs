#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using CloudFsmApi.Config;
using Microsoft.Azure.Devices;
using Microsoft.Extensions.Options;
using Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CloudFsmApi
{
    public class DownlinkManager : IDownlinkManager
    {
        private const string DEVICE_ID = "proxy-multiplexor";
        private readonly ServiceClient _serviceClient;

        public DownlinkManager(IOptions<DownlinkManagerConfig> config)
        {
            //load the transition table from blob
            _serviceClient = ServiceClient.CreateFromConnectionString(config.Value.IotHubSvcCnxnString);
        }

        /// <summary>
        /// Message explicitly addressed to Lantern
        /// </summary>
        /// <param name="lanternId"></param>
        /// <param name="commands"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public async Task SendCloudToLanternMethodAsync(string lanternId, List<Command> commands, int? table = null)
        {
            // Nothing to do if no lanternId or no commands
            if (commands == null)
                return;
            if (commands.Count == 0)
                return;
            if (string.IsNullOrEmpty(lanternId))
                return;

            try
            {
                foreach (var cmd in commands)
                {
                    cmd.LanternID = lanternId;
                }

                // Convert commands to json to send to IOT hub

                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };

                var body = JsonConvert.SerializeObject(commands, new JsonSerializerSettings
                {
                    ContractResolver = contractResolver,
                    Formatting = Formatting.None
                });

#if DEBUG
                Debug.WriteLine(body);
#else
                var c2l = new CloudToDeviceMethod("C2L");
                c2l.SetPayloadJson(body);
                await _serviceClient.InvokeDeviceMethodAsync(DEVICE_ID, c2l).ConfigureAwait(false);
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Other devices in the show, such as addressable speakers
        /// </summary>
        /// <param name="commands"></param>
        /// <returns></returns>
        public async Task SendCloudToOtherdeviceMethodAsync(List<Command> commands)
        {
            if (commands == null)
                return;
            if (commands.Count == 0)
                return;
            try
            {
                // Since these commands don't apply to lanterns, LanternID needs a default value
                // Instead of lanternID: null use lanternID: "allID"
                //  allID is how we call all lanterns, so it’s a safer thing for us to read than a null field.
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

#if DEBUG
                Debug.WriteLine(body);

#else
                var c2l = new CloudToDeviceMethod("C2L");
                c2l.SetPayloadJson(body);
                await _serviceClient.InvokeDeviceMethodAsync(DEVICE_ID, c2l).ConfigureAwait(false);
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}
