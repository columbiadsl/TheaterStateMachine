#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeProxy
{
    public class PipeServer
    {
        private const string IOT_CONNECTIONSTRING = "HostName=DvRvn3nvdIth.azure-devices.net;DeviceId=proxy-multiplexor;SharedAccessKey=FionzIkVTyUePWLOL4Jr9eUndfCxLCvBmXkCLUErDUI=";
        private readonly NamedPipeServerStream _pipeCommand;
        private readonly NamedPipeServerStream _pipeTelemetry;

        public PipeServer(NamedPipeServerStream pipeTelemetry, NamedPipeServerStream pipeCommand)
        {
            _pipeTelemetry = pipeTelemetry;
            _pipeCommand = pipeCommand;
        }

        public async Task Worker(CancellationToken ct)
        {
            Action<string> callback = SendCommand;
            var pmp = new MessagePump(IOT_CONNECTIONSTRING, callback);
            await pmp.OpenAsync();

            using (StreamReader sr = new StreamReader(_pipeTelemetry))
            {
                while (!ct.IsCancellationRequested)
                {
                    if (!_pipeTelemetry.IsConnected)
                    {
                        break;
                    }

                    try
                    {
                        // Read the request from the client. Once the client has
                        // written to the pipe its security token will be available.

                        var command = await sr.ReadLineAsync();
                        Console.Write("L2C\t");
                        Console.WriteLine(command);

                        //Send to IoT Hub Here
                        await pmp.SendAsync(command);
                    }
                    // Catch the IOException that is raised if the pipe is broken
                    // or disconnected.
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                }

                await pmp.CloseAsync();
            }
        }

        private void SendCommand(string cmd)
        {
            try
            {
                if (_pipeCommand.IsConnected)
                {
                    cmd += "\r\n";
                    _pipeCommand.Write(Encoding.UTF8.GetBytes(cmd));
                    _pipeCommand.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
