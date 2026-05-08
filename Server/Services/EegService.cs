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
    public class EegService : IEegService
    {
        private int _lastRowIndex = -1;
        private bool _disposed = false;

        public string EndSession()
        {
            _lastRowIndex = -1;

            Console.WriteLine("Sesija je zavrsena.");

            return "COMPLETED"; 
        }

        public string PushSample(EegSample sample)
        {
            //throw new NotImplementedException();
            // 1. Provera da li je sample null
            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample ne sme biti null." });

            // 2. Provera Timestamp
            if (sample.Timestamp == default(DateTime))
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Timestamp nije ispravan." });

            // 3. Monotoni rast RowIndex
            if (sample.RowIndex <= _lastRowIndex)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = $"RowIndex mora biti veci od {_lastRowIndex}." });

            // 4. Battery opseg
            if (sample.Battery < 0 || sample.Battery > 100)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Battery mora biti između 0 i 100." });

            if (sample.ContactQuality < 0 || sample.ContactQuality > 100)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "ContactQuality mora biti između 0 i 100." });

            // 5. EEG kanali ne smeju biti negativni
            if (sample.AF3 < 0 || sample.T7 < 0 || sample.Pz < 0 || sample.T8 < 0 || sample.AF4 < 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "EEG kanali ne smeju biti negativni." });

            // 6. Metrike između 0 i 100
            if (sample.Attention < 0 || sample.Attention > 1 ||
                sample.Engagement < 0 || sample.Engagement > 1 ||
                sample.Excitement < 0 || sample.Excitement > 1 ||
                sample.Interest < 0 || sample.Interest > 1 ||
                sample.Relaxation < 0 || sample.Relaxation > 1 ||
                sample.Stress < 0 || sample.Stress > 1)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Metrike moraju biti između 0 i 100." });

            // Sve proslo validaciju
            _lastRowIndex = sample.RowIndex;
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
            return "ACK";
        }

        public string Ping(string message)
        {
            Console.WriteLine("Klijent poslao: " + message);

            return "Server primio poruku: " + message;
        }
    }
}
