using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Common.Contracts;
using Server.Services;

namespace Server
{
    public class Program
    {
        static void Main(string[] args)
        {
            EegService service = new EegService();

            // Pretplate na evente
            service.OnTransferStarted += (participantId) =>
            {
                Console.WriteLine($"[EVENT] Prenos pokrenut za ispitanika: {participantId}");
            };

            service.OnSampleReceived += (sample) =>
            {
                Console.WriteLine($"Primljen uzorak: RowIndex={sample.RowIndex} | Timestamp={sample.Timestamp:dd/MM/yyyy HH:mm:ss}");
            };

            service.OnTransferCompleted += (msg) =>
            {
                Console.WriteLine($"[EVENT] {msg}");
            };

            service.OnWarningRaised += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] {msg}");
                Console.ResetColor();
            };

            ServiceHost host = new ServiceHost(service);

            try
            {
                host.Open();
                Console.WriteLine("Server pokrenut.");
                Console.WriteLine("Pritisni ENTER za kraj.");
                Console.ReadLine();
                host.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
                host.Abort();
            }
            finally
            {
                service.Dispose();
            }
        }
    }
}
