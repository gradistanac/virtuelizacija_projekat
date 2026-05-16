using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Server.Services;
using Common.Contracts;
using System.ServiceModel.Channels;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            ServiceHost host = new ServiceHost(typeof(EegService));

            try
            {
                host.Open();

                Console.WriteLine("Server je pokrenut.");
                Console.WriteLine("Pritisnite ENTER za gasenje.");

                Console.ReadLine();

                Console.WriteLine("\n[Simulacija Dispose pattern-a pri prekidu sesije]");
                var testService = new EegService();
                testService.StartSession(new Common.Models.EegMeta { ParticipantId = "test", FileName = "test.csv", TotalRows = 0, SchemaVersion = "1.0" });
                testService.SimulateDisconnect();
                Console.WriteLine("[Simulacija zavrsena]\n");

                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());

                Console.ReadLine();

                host.Abort();
            }
        }
    }
}
