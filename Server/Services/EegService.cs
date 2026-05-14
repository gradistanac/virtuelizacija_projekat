using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Shared.Contracts;
using Shared.Models;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class EegService : IEegService, IDisposable
    {
        private int _lastRowIndex = -1;
        private bool _disposed = false;
        private FileManager _fileManager;

        public string EndSession()
        {
            _lastRowIndex = -1;
            _fileManager.CloseSession();
            Console.WriteLine("Sesija je zavrsena.");
            return "COMPLETED";
        }

        public string PushSample(EegSample sample)
        {
            
            //throw new NotImplementedException();
            // 1. Provera da li je sample null
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample ne sme biti null." });
            }
                

            // 2. Provera Timestamp
            if (sample.Timestamp == default(DateTime))
            {
                _fileManager.WriteReject(sample, "Timestamp nije ispravan");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Timestamp nije ispravan." });
            }
                

            // 3. Monotoni rast RowIndex
            if (sample.RowIndex <= _lastRowIndex)
            {
                _fileManager.WriteReject(sample, $"RowIndex mora biti veci od {_lastRowIndex}.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = $"RowIndex mora biti veci od {_lastRowIndex}."});
            }
                

            // 4. Battery opseg
            if (sample.Battery < 0 || sample.Battery > 100)
            {
                _fileManager.WriteReject(sample, "Battery mora biti izmedju 0 i 100.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Battery mora biti između 0 i 100." });
            }
                

            if (sample.ContactQuality < 0 || sample.ContactQuality > 100)
            {
                _fileManager.WriteReject(sample, "ContactQuality mora biti između 0 i 100.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "ContactQuality mora biti između 0 i 100."});
            }

            // 5. EEG kanali ne smeju biti negativni
            if (sample.AF3 < 0 || sample.T7 < 0 || sample.Pz < 0 || sample.T8 < 0 || sample.AF4 < 0)
            {
                _fileManager.WriteReject(sample, "EEG kanali ne smeju biti negativni.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "EEG kanali ne smeju biti negativni."});
            }
                

            // 6. Metrike između 0 i 1
            if (sample.Attention < 0 || sample.Attention > 1 ||
                sample.Engagement < 0 || sample.Engagement > 1 ||
                sample.Excitement < 0 || sample.Excitement > 1 ||
                sample.Interest < 0 || sample.Interest > 1 ||
                sample.Relaxation < 0 || sample.Relaxation > 1 ||
                sample.Stress < 0 || sample.Stress > 1)
            {
                _fileManager.WriteReject(sample, "Metrike moraju biti između 0 i 1.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Metrike moraju biti između 0 i 1." });
            }
                

            // Sve proslo validaciju
            _lastRowIndex = sample.RowIndex;
            _fileManager.WriteSample(sample);
            return "IN_PROGRESS";
        }

        public string StartSession(EegMeta meta)
        {
            //throw new NotImplementedException();
            //return "ACK";
            if (meta == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Meta ne sme biti null." });
            if (string.IsNullOrWhiteSpace(meta.ParticipantId))
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "ParticipantId ne sme biti null." });

            _lastRowIndex = -1;
            _fileManager = new FileManager();
            _fileManager.OpenSession(meta);
            return "ACK";
        }

        public string Ping(string message)
        {
            Console.WriteLine("Klijent poslao: " + message);

            return "Server primio poruku: " + message;
        }

        public void SimulateDisconnect()
        {
            Console.WriteLine("[Test] Simulacija prekida veze — pozivam Dispose...");
            Dispose();
            Console.WriteLine("[Test] Dispose pozvan, resursi zatvoreni.");
        }


        ~EegService()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_fileManager != null)
                    {
                        _fileManager.Dispose();
                        _fileManager = null;
                    }
                }
                _disposed = true;
            }
        }
    }
}
