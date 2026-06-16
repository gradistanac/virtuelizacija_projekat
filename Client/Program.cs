using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common.Contracts;
using Common.Models;
using Client.Services;

namespace Client
{
    public class Program
    {
        private const int MaxTransferAttempts = 2;

        static void Main(string[] args)
        {
            CsvReader csvReader = new CsvReader();
            var allFiles = csvReader.ReadAllFiles("Data");

            foreach (var (participantId, fileName, samples) in allFiles)
            {
                if (samples.Count == 0)
                {
                    string message = $"Ispitanik {participantId} - fajl {fileName} nema validnih redova za slanje.";
                    csvReader.LogError(message);
                    Console.WriteLine(message);
                    continue;
                }

                if (!SendParticipant(csvReader, participantId, fileName, samples))
                {
                    break;
                }
            }
        }

        private static bool SendParticipant(CsvReader csvReader, string participantId, string fileName, List<EegSample> samples)
        {
            for (int attempt = 1; attempt <= MaxTransferAttempts; attempt++)
            {
                ChannelFactory<IEegService> factory = null;
                IClientChannel channel = null;
                bool transferCompleted = false;

                try
                {
                    Console.WriteLine($"Ispitanik {participantId} - pocinjem slanje (pokusaj {attempt}/{MaxTransferAttempts})...");

                    factory = new ChannelFactory<IEegService>("EegServiceEndpoint");
                    IEegService proxy = factory.CreateChannel();
                    channel = (IClientChannel)proxy;

                    string startStatus = proxy.StartSession(new EegMeta
                    {
                        ParticipantId = participantId,
                        FileName = fileName,
                        TotalRows = samples.Count,
                        SchemaVersion = "1.0",
                        SessionDate = samples[0].Timestamp.Date
                    });
                    EnsureStatus(startStatus, "ACK", "StartSession", participantId, -1);

                    foreach (var sample in samples)
                    {
                        string pushStatus = proxy.PushSample(sample);
                        EnsureStatus(pushStatus, "IN_PROGRESS", "PushSample", participantId, sample.RowIndex);
                    }

                    string endStatus = proxy.EndSession();
                    EnsureStatus(endStatus, "COMPLETED", "EndSession", participantId, -1);

                    transferCompleted = true;
                    Console.WriteLine($"Ispitanik {participantId} - gotovo.");
                    return true;
                }
                catch (FaultException<ValidationFault> ex)
                {
                    string message = $"Ispitanik {participantId} | ValidationFault | {ex.Detail.Message}";
                    csvReader.LogError(message);
                    Console.WriteLine(message);
                    return true;
                }
                catch (FaultException<DataFormatFault> ex)
                {
                    string message = $"Ispitanik {participantId} | DataFormatFault | {ex.Detail.Message}";
                    csvReader.LogError(message);
                    Console.WriteLine(message);
                    return true;
                }
                catch (TimeoutException ex)
                {
                    string message = $"Ispitanik {participantId} | Timeout | Pokušaj {attempt}/{MaxTransferAttempts} | {ex.Message}";
                    csvReader.LogError(message);
                    Console.WriteLine(message);

                    if (attempt == MaxTransferAttempts)
                        return false;
                }
                catch (CommunicationException ex)
                {
                    string message = $"Ispitanik {participantId} | CommunicationException | Pokušaj {attempt}/{MaxTransferAttempts} | {ex.Message}";
                    csvReader.LogError(message);
                    Console.WriteLine(message);

                    if (attempt == MaxTransferAttempts)
                        return false;
                }
                catch (Exception ex)
                {
                    string message = $"Ispitanik {participantId} | Neočekivana greška | {ex.Message}";
                    csvReader.LogError(message);
                    Console.WriteLine(message);
                    return false;
                }
                finally
                {
                    CloseCommunicationObject(channel, transferCompleted);
                    CloseCommunicationObject(factory, transferCompleted);
                }
            }

            return false;
        }

        private static void EnsureStatus(string actualStatus, string expectedStatus, string operationName, string participantId, int rowIndex)
        {
            if (!string.Equals(actualStatus, expectedStatus, StringComparison.OrdinalIgnoreCase))
            {
                string rowLabel = rowIndex >= 0 ? $" | RowIndex={rowIndex}" : string.Empty;
                throw new CommunicationException(
                    $"Neočekivan status za {operationName}. ParticipantId={participantId}{rowLabel} | Očekivano={expectedStatus} | Dobijeno={actualStatus}");
            }
        }

        private static void CloseCommunicationObject(ICommunicationObject communicationObject, bool closeGracefully)
        {
            if (communicationObject == null)
                return;

            try
            {
                if (closeGracefully && communicationObject.State != CommunicationState.Faulted)
                {
                    communicationObject.Close();
                }
                else
                {
                    communicationObject.Abort();
                }
            }
            catch (CommunicationException)
            {
                communicationObject.Abort();
            }
            catch (TimeoutException)
            {
                communicationObject.Abort();
            }
        }
    }
}
