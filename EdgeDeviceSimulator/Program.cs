#region copyright
// This work is licensed under the Creative Commons Attribution-ShareAlike 4.0 International License. 
// To view a copy of this license, visit http://creativecommons.org/licenses/by-sa/4.0/ or send a letter 
// to Creative Commons, PO Box 1866, Mountain View, CA 94042, USA.
#endregion copyright

using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeDeviceSimulator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //PrintJson();
            Welcome();

            NamedPipeClientStream pipeTelemetry = new NamedPipeClientStream(".", "telemetry",
                    PipeDirection.InOut, PipeOptions.None,
                    TokenImpersonationLevel.Impersonation);

            NamedPipeClientStream pipeCommand = new NamedPipeClientStream(".", "command",
                PipeDirection.InOut, PipeOptions.None,
                TokenImpersonationLevel.Impersonation);

            //TELEMETRY HAS TO BE FIRST
            Console.Write("Connecting Telemetry to server...");
            pipeTelemetry.Connect();
            Console.WriteLine("Connected");

            //COMMAND HAS TO BE SECOND
            Console.Write("Connecting Command to server...");
            pipeCommand.Connect();
            Console.WriteLine("Connected");

            var cts = new CancellationTokenSource();

            //listen for commands
            _ = Worker(pipeCommand, cts.Token);

            using (var sw = new StreamWriter(pipeTelemetry))
            {
                sw.AutoFlush = true;
                do
                {
                    Console.WriteLine("[W] Write Telemetry");
                    Console.WriteLine("[x] Exit");
                    string resp = ReadMenu();
                    if (resp == "x")
                        break;

                    //ensure it serializes
                    Telemetry telemetry = Telemetry.FromString(resp);
                    if (telemetry != null)
                    {
                        sw.WriteLine(telemetry.ToJson());
                        pipeTelemetry.WaitForPipeDrain();
                    }
                    if (!pipeTelemetry.IsConnected)
                        break;
                } while (true);
            }

            cts.Cancel();
            pipeTelemetry.Close();
            pipeCommand.Close();
        }

        private static void PrintJson()
        {
            Dictionary<string, CharacterTrigger> testDict = new Dictionary<string, CharacterTrigger>()
        {
            {"Step1", new CharacterTrigger(){
                BeaconId = "BEA001",
                Triggers = new Dictionary<string, List<Command>>()
                {
                    { "LAN001", new List<Command>()
                    {
                        { new Command()
                        {
                            Light = new Effect()
                            {
                                Type = "play",
                                Value = 1000
                            },
                            Sound = null
                        }
                        }
                    }
                    }
                }
            } }
        };

            Console.WriteLine(JsonConvert.SerializeObject(testDict, Formatting.Indented));
        }

        private static string ReadMenu()
        {
            do
            {
                var line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    continue;

                switch (line[0])
                {
                    case 'X':
                    case 'x':
                        return "x";

                    case 'W':
                    case 'w':
                        {
                            Console.WriteLine("Enter Telemetry or empty for default");
                            var resp = Console.ReadLine();
                            if (string.IsNullOrEmpty(resp))
                                return Telemetry.Random();
                            return resp;
                        }
                    default:
                        Console.WriteLine("Invalid selection"); break;
                }
            } while (true);
        }

        private static void Welcome()
        {
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly());
            Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@\r\n\n");
        }

        private static async Task Worker(NamedPipeClientStream pipeClient, CancellationToken ct)
        {
            using (var sr = new StreamReader(pipeClient))
            {
                while (!ct.IsCancellationRequested)
                {
                    var command = await sr.ReadLineAsync();
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss}\t{command}");
                    //Do somthing with the data here
                }
            }
        }
    }
}
