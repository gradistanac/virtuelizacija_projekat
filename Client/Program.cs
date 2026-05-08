using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Shared.Contracts;
using Shared.Models;
using Client.Services;

namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            CsvReader csvReader = new CsvReader();
            var allFiles = csvReader.ReadAllFiles("Data");

            using (ChannelFactory<IEegService> factory = new ChannelFactory<IEegService>("EegServiceEndpoint"))
            {
                IEegService proxy = factory.CreateChannel();
                try
                {
                    foreach (var (participantId, samples) in allFiles)
                    {
                        Console.WriteLine($"Slanje podataka za ispitanika: {participantId}");

                        proxy.StartSession(new EegMeta
                        {
                            ParticipantId = participantId,
                            FileName = $"subject_{participantId}_results.csv",
                            TotalRows = samples.Count,
                            SchemaVersion = "1.0"
                        });

                        Console.WriteLine($"Pocetak sesije za ispitanika: {participantId}, ukupno redova: {samples.Count}");

                        foreach (var sample in samples)
                        {
                            proxy.PushSample(sample);
                        }

                        string endStatus = proxy.EndSession();
                        Console.WriteLine($"Sesija zavrsena za ispitanika {participantId}: {endStatus}");
                    }
                }
                catch (FaultException ex)
                {
                    Console.WriteLine($"Greška: {ex.Message}");
                }
                finally
                {
                    ((IClientChannel)proxy).Close();
                }
            }
        }
    }
}
