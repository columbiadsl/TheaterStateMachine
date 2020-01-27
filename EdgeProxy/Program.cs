#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeProxy
{
    /// <summary>
    /// EdgeProxy runs on the local network, relaying telemetry to the cloud service, and 
    ///  commands from the cloud service. Communicates with local show server.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            Welcome();

            var cts = new CancellationTokenSource();

            //recover if edge device disconnects
            do
            {
                NamedPipeServerStream pipeTelemetry = new NamedPipeServerStream("telemetry", PipeDirection.InOut, 1);
                NamedPipeServerStream pipeCommand = new NamedPipeServerStream("command", PipeDirection.InOut, 1);
                var pipeServer = new PipeServer(pipeTelemetry, pipeCommand);

                Console.WriteLine("Waiting for Connection pipes");

                pipeTelemetry.WaitForConnection();
                Console.WriteLine("Telemetry Pipe Ready");
                pipeCommand.WaitForConnection();
                Console.WriteLine("Command Pipe Ready");

                //background task to forward Pipe messages
                _ = pipeServer.Worker(cts.Token);

                //handle console menu
                do
                {
                    try
                    {
                        if (!pipeCommand.IsConnected)
                            break;
                        if (!pipeTelemetry.IsConnected)
                            break;

                        Task.Delay(500);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } while (!cts.Token.IsCancellationRequested);

                Console.WriteLine("Edge device disconnected");
                Task.Delay(500);

                //if (pipeTelemetry.IsConnected)
                {
                    pipeTelemetry.Disconnect();
                    pipeTelemetry.Close();
                    Console.WriteLine("Telemetry pipe closed");
                }
                //if (pipeCommand.IsConnected)
                {
                    pipeCommand.Disconnect();
                    pipeCommand.Close();
                    Console.WriteLine("Command pipe closed");
                }
            } while (!cts.Token.IsCancellationRequested);
        }

        private static void Welcome()
        {
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly());
            Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@\r\n\n");
        }
    }
}
