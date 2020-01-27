#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;

namespace CloudFsmProcessor
{
    public class Worker
    {
        private readonly HttpClient _client;
        private readonly WorkerConfig _settings;

        public Worker(IHttpClientFactory httpClientFactory, IOptions<WorkerConfig> settings)
        {
            _client = httpClientFactory.CreateClient();
            _settings = settings.Value;
        }

        [FunctionName("Worker")]
        public async Task Run([IoTHubTrigger("messages/events", Connection = "IotHubSvcCnxnString")]EventData[] messages, ILogger log)
        {
            //Call the API for each message
            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                log.LogInformation(data);

                Telemetry telemetry = JsonConvert.DeserializeObject<Telemetry>(data);

                var uri = $"https://{_settings.ApiHostname}/api/v1.0/Scene/onBeaconChange/{telemetry.LanternId}/{telemetry.BeaconId}";

                HttpResponseMessage response = await _client.PostAsync(uri, null).ConfigureAwait(false);
                string respContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                log.LogInformation(respContent);
            }

            foreach (var message in messages)
                message.Dispose();
        }
    }
}
