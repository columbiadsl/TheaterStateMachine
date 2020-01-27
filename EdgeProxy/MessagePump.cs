#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdgeProxy
{
    public class MessagePump
    {
        private readonly Action<string> _callback;

        private readonly DeviceClient _client;

        public MessagePump(string connectionString, Action<string> callback)
        {
            _client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Amqp);
            _callback = callback;
        }

        private enum ErrorId
        {
            SendBatchAsync = 1000,
        }

        public async Task OpenAsync()
        {
            _client.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);

            // Method Call processing will be enabled when the first method handler is added.
            // Setup a callback for the 'C2L' method.
            await _client.SetMethodHandlerAsync(nameof(C2L), C2L, null).ConfigureAwait(false);
            await _client.OpenAsync().ConfigureAwait(false);
        }

        public async Task SendAsync(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;
            try
            {
                using (var msg = new Message(Encoding.UTF8.GetBytes(message)))
                {
                    await _client.SendEventAsync(msg).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<int> SendBatchAsync(List<string> messages)
        {
            int resp = -1;
            try
            {
                var events = new List<Message>();
                events.AddRange(messages.Select(msg => new Message(Encoding.UTF8.GetBytes(msg))));

                if (events.Count > 0)
                    await _client.SendEventBatchAsync(events).ConfigureAwait(false);

                resp = events.Count;

                events.ForEach(e => e.Dispose());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return resp;
        }

        internal async Task CloseAsync()
        {
            await _client.CloseAsync();
        }

        private Task<MethodResponse> C2L(MethodRequest methodRequest, object userContext)
        {
            try
            {
                var json = methodRequest.DataAsJson;
                Console.WriteLine("C2L\t{0}", json);

                //send it to the protocol translator
                _callback(json);

                return Task.FromResult(new MethodResponse(new byte[0], 200));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(ex.Message), 500));
            }
        }

        private void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Console.WriteLine("IoT Hub Connection Changed {0} {1}", status, reason);
        }
    }
}
