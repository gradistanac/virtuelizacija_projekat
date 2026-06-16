using Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Server.Services
{
    public class FileManager : IDisposable
    {
        private readonly string _dataPath;
        private StreamWriter _sessionWriter;
        private bool _disposed = false;
        private string _currentParticipantId;
        private DateTime _currentSessionDate;
        private string _rejectsPath;

        public FileManager()
        {
            _dataPath = ConfigurationManager.AppSettings["DataPath"];
        }

        public void OpenSession(EegMeta meta)
        {
            CloseSession();

            _currentSessionDate = meta.SessionDate == default(DateTime)
                ? DateTime.Today
                : meta.SessionDate.Date;

            string date = _currentSessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string sessionPath = Path.Combine(_dataPath, meta.ParticipantId, date);
            _currentParticipantId = meta.ParticipantId;
            _rejectsPath = Path.Combine(sessionPath, "rejects.csv");

            if (!Directory.Exists(sessionPath))
                Directory.CreateDirectory(sessionPath);

            string sessionFilePath = Path.Combine(sessionPath, "session.csv");
            _sessionWriter = new StreamWriter(sessionFilePath, append: false, Encoding.UTF8);

            _sessionWriter.WriteLine("Timestamp,AF3,T7,Pz,T8,AF4,Attention,Engagement," +
                "Excitement,Interest,Relaxation,Stress,Battery,ContactQuality,SlideIndex,SetIndex,RowIndex");
        }

        public void WriteSample(EegSample sample)
        {
            _sessionWriter.WriteLine(CreateRawLine(sample));
        }

        public void WriteReject(EegSample sample, string reason)
        {
            bool isNew = !File.Exists(_rejectsPath);

            using (StreamWriter rejectWriter = new StreamWriter(_rejectsPath, append: true, Encoding.UTF8))
            {
                if (isNew)
                    rejectWriter.WriteLine("Time,Reason,RawLine");

                rejectWriter.WriteLine(
                    $"{FormatTimestamp(DateTime.Now)},{EscapeCsv(reason)},{EscapeCsv(CreateRawLine(sample))}");
            }
        }

        public void CloseSession()
        {
            if (_sessionWriter != null)
            {
                _sessionWriter.Flush();
                _sessionWriter.Close();
                _sessionWriter = null;
            }
        }

        private static string CreateRawLine(EegSample sample)
        {
            return string.Join(",",
                FormatTimestamp(sample.Timestamp),
                sample.AF3.ToString(CultureInfo.InvariantCulture),
                sample.T7.ToString(CultureInfo.InvariantCulture),
                sample.Pz.ToString(CultureInfo.InvariantCulture),
                sample.T8.ToString(CultureInfo.InvariantCulture),
                sample.AF4.ToString(CultureInfo.InvariantCulture),
                sample.Attention.ToString(CultureInfo.InvariantCulture),
                sample.Engagement.ToString(CultureInfo.InvariantCulture),
                sample.Excitement.ToString(CultureInfo.InvariantCulture),
                sample.Interest.ToString(CultureInfo.InvariantCulture),
                sample.Relaxation.ToString(CultureInfo.InvariantCulture),
                sample.Stress.ToString(CultureInfo.InvariantCulture),
                sample.Battery.ToString(CultureInfo.InvariantCulture),
                sample.ContactQuality.ToString(CultureInfo.InvariantCulture),
                sample.SlideIndex.ToString(CultureInfo.InvariantCulture),
                sample.SetIndex.ToString(CultureInfo.InvariantCulture),
                sample.RowIndex.ToString(CultureInfo.InvariantCulture));
        }

        private static string EscapeCsv(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
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
                    CloseSession();
                _disposed = true;
            }
        }

        ~FileManager()
        {
            Dispose(false);
        }
    }
}
