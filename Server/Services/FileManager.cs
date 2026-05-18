using Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Services
{
    public class FileManager : IDisposable
    {
        private readonly string _dataPath;
        private StreamWriter _sessionWriter;
        private bool _disposed = false;
        private string _currentParticipantId;

        public FileManager()
        {
            _dataPath = ConfigurationManager.AppSettings["DataPath"];
        }

        public void OpenSession(EegMeta meta)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string sessionPath = Path.Combine(_dataPath, meta.ParticipantId, date);
            _currentParticipantId = meta.ParticipantId;

            if (!Directory.Exists(sessionPath))
                Directory.CreateDirectory(sessionPath);

            string sessionFilePath = Path.Combine(sessionPath, "session.csv");
            bool isNewFile = !File.Exists(sessionFilePath);
            _sessionWriter = new StreamWriter(sessionFilePath, append: true);

            if (isNewFile)
                _sessionWriter.WriteLine("Timestamp,AF3,T7,Pz,T8,AF4,Attention,Engagement," +
                    "Excitement,Interest,Relaxation,Stress,Battery,ContactQuality,SlideIndex,SetIndex,RowIndex");
        }

        public void WriteSample(EegSample sample)
        {
            _sessionWriter.WriteLine($"{sample.Timestamp:dd/MM/yyyy HH:mm:ss}," +
                $"{sample.AF3},{sample.T7},{sample.Pz},{sample.T8},{sample.AF4}," +
                $"{sample.Attention},{sample.Engagement},{sample.Excitement}," +
                $"{sample.Interest},{sample.Relaxation},{sample.Stress}," +
                $"{sample.Battery},{sample.ContactQuality}," +
                $"{sample.SlideIndex},{sample.SetIndex},{sample.RowIndex}");
        }

        public void WriteReject(EegSample sample, string reason)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string rejectsPath = Path.Combine(_dataPath, _currentParticipantId, date, "rejects.csv");

            string rawLine = $"{sample.Timestamp:dd/MM/yyyy HH:mm:ss},{sample.AF3},{sample.T7},{sample.Pz},{sample.T8},{sample.AF4}," +
                             $"{sample.Attention},{sample.Engagement},{sample.Excitement},{sample.Interest},{sample.Relaxation},{sample.Stress}," +
                             $"{sample.Battery},{sample.ContactQuality},{sample.SlideIndex},{sample.SetIndex},{sample.RowIndex}";

            using (StreamWriter rejectWriter = new StreamWriter(rejectsPath, append: true))
            {
                rejectWriter.WriteLine($"{DateTime.Now:dd/MM/yyyy HH:mm:ss},{reason},{rawLine}");
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
                    CloseSession();
                    if (_sessionWriter != null)
                    {
                        _sessionWriter.Dispose();
                        _sessionWriter = null;
                    }
                }
                _disposed = true;
            }
        }

        ~FileManager()
        {
            Dispose(false);
        }
    }
}
