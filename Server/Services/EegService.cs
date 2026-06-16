using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Common.Contracts;
using Common.Models;
using System.Globalization;

namespace Server.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class EegService : IEegService, IDisposable
    {
        private int _lastRowIndex = -1;
        private bool _disposed = false;
        private FileManager _fileManager;
        private EegSample _lastSample = null;
        private string _currentParticipantId;

        // Pragovi iz konfiguracije
        private readonly int _batteryLowThreshold;
        private readonly int _contactQualityMin;
        private readonly double _stressSpikeThreshold;
        private readonly long _timestampSkewMaxMs;
        private readonly double _channelOutOfBandPct;

        // Eventi
        public event Action<string> OnTransferStarted;
        public event Action<EegSample> OnSampleReceived;
        public event Action<string> OnTransferCompleted;
        public event Action<string> OnWarningRaised;

        public EegService()
        {
            _batteryLowThreshold = int.Parse(ConfigurationManager.AppSettings["BatteryLowThreshold"]);
            _contactQualityMin = int.Parse(ConfigurationManager.AppSettings["ContactQualityMin"]);
            _stressSpikeThreshold = double.Parse(ConfigurationManager.AppSettings["StressSpikeThreshold"], System.Globalization.CultureInfo.InvariantCulture);
            _timestampSkewMaxMs = long.Parse(ConfigurationManager.AppSettings["TimestampSkewMaxMs"]);
            _channelOutOfBandPct = double.Parse(ConfigurationManager.AppSettings["ChannelOutOfBandPct"], CultureInfo.InvariantCulture);
        }

        public string StartSession(EegMeta meta)
        {
            if (meta == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Meta ne sme biti null." });
            if (string.IsNullOrWhiteSpace(meta.ParticipantId))
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "ParticipantId ne sme biti prazan." });
            if (string.IsNullOrWhiteSpace(meta.FileName))
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "FileName ne sme biti prazan." });
            if (meta.TotalRows <= 0)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "TotalRows mora biti veći od 0." });
            if (string.IsNullOrWhiteSpace(meta.SchemaVersion))
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "SchemaVersion ne sme biti prazan." });

            _lastRowIndex = -1;
            _lastSample = null;
            _currentParticipantId = meta.ParticipantId;

            if (_fileManager != null)
            {
                _fileManager.Dispose();
                _fileManager = null;
            }

            _fileManager = new FileManager();
            _fileManager.OpenSession(meta);

            OnTransferStarted?.Invoke(meta.ParticipantId);
            return "ACK";
        }

        public string PushSample(EegSample sample)
        {
            if (sample == null)
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample ne sme biti null." });

            if (_fileManager == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Sesija nije pokrenuta." });

            if (HasInvalidNumericValue(sample))
            {
                _fileManager.WriteReject(sample, "Sample sadrži neispravne numeričke vrednosti.");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Sample sadrži neispravne numeričke vrednosti." });
            }

            if (sample.Timestamp == default(DateTime))
            {
                _fileManager.WriteReject(sample, "Timestamp nije ispravan.");
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault { Message = "Timestamp nije ispravan." });
            }

            if (sample.RowIndex <= _lastRowIndex)
            {
                _fileManager.WriteReject(sample, $"RowIndex mora biti veci od {_lastRowIndex}.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = $"RowIndex mora biti veci od {_lastRowIndex}." });
            }

            if (sample.Battery < 0 || sample.Battery > 100)
            {
                _fileManager.WriteReject(sample, "Battery mora biti između 0 i 100.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Battery mora biti između 0 i 100." });
            }

            if (sample.ContactQuality < 0 || sample.ContactQuality > 100)
            {
                _fileManager.WriteReject(sample, "ContactQuality mora biti između 0 i 100.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "ContactQuality mora biti između 0 i 100." });
            }

            if (sample.AF3 < 0 || sample.T7 < 0 || sample.Pz < 0 || sample.T8 < 0 || sample.AF4 < 0)
            {
                _fileManager.WriteReject(sample, "EEG kanali ne smeju biti negativni.");
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "EEG kanali ne smeju biti negativni." });
            }

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

            // Analitika 10 — ContactQuality, Battery, TimeSkew
            if (sample.ContactQuality < _contactQualityMin)
            {
                string msg = $"[PoorContactWarning] ParticipantId={_currentParticipantId} | ContactQuality={sample.ContactQuality} < {_contactQualityMin} | RowIndex={sample.RowIndex} | Timestamp={FormatTimestamp(sample.Timestamp)}";
                OnWarningRaised?.Invoke(msg);
                _fileManager.WriteReject(sample, $"PoorContactWarning: ContactQuality={sample.ContactQuality}");
            }

            if (sample.Battery < _batteryLowThreshold)
            {
                string msg = $"[LowBatteryWarning] ParticipantId={_currentParticipantId} | Battery={sample.Battery} < {_batteryLowThreshold} | RowIndex={sample.RowIndex} | Timestamp={FormatTimestamp(sample.Timestamp)}";
                OnWarningRaised?.Invoke(msg);
                _fileManager.WriteReject(sample, $"LowBatteryWarning: Battery={sample.Battery}");
            }

            if (_lastSample != null)
            {
                long skewMs = (long)Math.Abs((sample.Timestamp - _lastSample.Timestamp).TotalMilliseconds);
                if (skewMs > _timestampSkewMaxMs)
                {
                    string msg = $"[TimeSkewWarning] ParticipantId={_currentParticipantId} | Razmak={skewMs}ms > {_timestampSkewMaxMs}ms | RowIndex={sample.RowIndex} | Timestamp={FormatTimestamp(sample.Timestamp)}";
                    OnWarningRaised?.Invoke(msg);
                    _fileManager.WriteReject(sample, $"TimeSkewWarning: skew={skewMs}ms");
                }

                WarnIfChannelOutOfBand("AF3", _lastSample.AF3, sample.AF3, sample);
                WarnIfChannelOutOfBand("T7", _lastSample.T7, sample.T7, sample);
                WarnIfChannelOutOfBand("Pz", _lastSample.Pz, sample.Pz, sample);
                WarnIfChannelOutOfBand("T8", _lastSample.T8, sample.T8, sample);
                WarnIfChannelOutOfBand("AF4", _lastSample.AF4, sample.AF4, sample);

                // Analitika 9 — DeltaStress i DeltaRelaxation
                double deltaStress = sample.Stress - _lastSample.Stress;
                if (Math.Abs(deltaStress) > _stressSpikeThreshold)
                {
                    string smer = deltaStress > 0 ? "porast" : "pad";
                    string msg = $"[StressSpike] ParticipantId={_currentParticipantId} | {smer} za {Math.Abs(deltaStress):F4} | RowIndex={sample.RowIndex} | Stress: {_lastSample.Stress:F4} -> {sample.Stress:F4} | Timestamp={FormatTimestamp(sample.Timestamp)}";
                    OnWarningRaised?.Invoke(msg);
                }

                double deltaRelaxation = sample.Relaxation - _lastSample.Relaxation;
                if (Math.Abs(deltaRelaxation) > _stressSpikeThreshold)
                {
                    string smer = deltaRelaxation > 0 ? "porast" : "pad";
                    string msg = $"[RelaxationSpike] ParticipantId={_currentParticipantId} | {smer} za {Math.Abs(deltaRelaxation):F4} | RowIndex={sample.RowIndex} | Relaxation: {_lastSample.Relaxation:F4} -> {sample.Relaxation:F4} | Timestamp={FormatTimestamp(sample.Timestamp)}";
                    OnWarningRaised?.Invoke(msg);
                }
            }

            _lastRowIndex = sample.RowIndex;
            _lastSample = sample;
            _fileManager.WriteSample(sample);

            OnSampleReceived?.Invoke(sample);
            return "IN_PROGRESS";
        }

        public string EndSession()
        {
            if (_fileManager == null)
                throw new FaultException<ValidationFault>(
                    new ValidationFault { Message = "Nema aktivne sesije." });

            _lastRowIndex = -1;
            _lastSample = null;
            _currentParticipantId = null;
            _fileManager.Dispose();
            _fileManager = null;

            OnTransferCompleted?.Invoke("Prenos završen.");
            return "COMPLETED";
        }

        private bool HasInvalidNumericValue(EegSample sample)
        {
            return HasInvalidDouble(sample.AF3) ||
                   HasInvalidDouble(sample.T7) ||
                   HasInvalidDouble(sample.Pz) ||
                   HasInvalidDouble(sample.T8) ||
                   HasInvalidDouble(sample.AF4) ||
                   HasInvalidDouble(sample.Attention) ||
                   HasInvalidDouble(sample.Engagement) ||
                   HasInvalidDouble(sample.Excitement) ||
                   HasInvalidDouble(sample.Interest) ||
                   HasInvalidDouble(sample.Relaxation) ||
                   HasInvalidDouble(sample.Stress);
        }

        private void WarnIfChannelOutOfBand(string channelName, double previousValue, double currentValue, EegSample sample)
        {
            if (previousValue <= 0)
                return;

            double changePct = Math.Abs((currentValue - previousValue) / previousValue) * 100.0;
            if (changePct > _channelOutOfBandPct)
            {
                string msg = $"[ChannelOutOfBandWarning] ParticipantId={_currentParticipantId} | Channel={channelName} | Promena={changePct:F2}% > {_channelOutOfBandPct:F2}% | RowIndex={sample.RowIndex} | Value: {previousValue:F4} -> {currentValue:F4} | Timestamp={FormatTimestamp(sample.Timestamp)}";
                OnWarningRaised?.Invoke(msg);
                _fileManager.WriteReject(sample, $"ChannelOutOfBandWarning: {channelName}={changePct:F2}%");
            }
        }

        private static bool HasInvalidDouble(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        private static string FormatTimestamp(DateTime value)
        {
            return value.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
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

        ~EegService()
        {
            Dispose(false);
        }
    }
}
