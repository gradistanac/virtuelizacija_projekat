using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Models;
using System.IO;
using System.Globalization;

namespace Client.Services
{
    public class CsvReader
    {
        private readonly string _logPath = "client_log.txt";

        public List<(string participantId, string fileName, List<EegSample> samples)> ReadAllFiles(string folderPath)
        {
            var result = new List<(string participantId, string fileName, List<EegSample> samples)>();

            string[] files = Directory.GetFiles(folderPath, "*.csv", SearchOption.AllDirectories);
            files = files.OrderBy(f => int.Parse(Path.GetFileName(f).Split('_')[1])).ToArray();

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                string[] parts = fileName.Split('_');
                string participantId = parts[1];

                List<EegSample> samples = ReadFile(filePath);
                result.Add((participantId, fileName, samples));
            }

            return result;
        }

        public void LogError(string message)
        {
            using (StreamWriter logWriter = new StreamWriter(_logPath, append: true))
            {
                logWriter.WriteLine($"{DateTime.Now:dd/MM/yyyy HH:mm:ss} | {message}");
            }
        }

        public List<EegSample> ReadFile(string filePath)
        {
            var samples = new List<EegSample>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                reader.ReadLine(); // preskoci zaglavlje

                string line;
                int rowIndex = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        string[] columns = line.Split(',');
                        if (columns.Length < 16)
                            throw new FormatException("CSV red nema očekivanih 16 kolona.");

                        EegSample sample = new EegSample
                        {
                            RowIndex = rowIndex,
                            Timestamp = DateTime.ParseExact(columns[0], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                            AF3 = double.Parse(columns[1], CultureInfo.InvariantCulture),
                            T7 = double.Parse(columns[2], CultureInfo.InvariantCulture),
                            Pz = double.Parse(columns[3], CultureInfo.InvariantCulture),
                            T8 = double.Parse(columns[4], CultureInfo.InvariantCulture),
                            AF4 = double.Parse(columns[5], CultureInfo.InvariantCulture),
                            Attention = double.Parse(columns[6], CultureInfo.InvariantCulture),
                            Engagement = double.Parse(columns[7], CultureInfo.InvariantCulture),
                            Excitement = double.Parse(columns[8], CultureInfo.InvariantCulture),
                            Interest = double.Parse(columns[9], CultureInfo.InvariantCulture),
                            Relaxation = double.Parse(columns[10], CultureInfo.InvariantCulture),
                            Stress = double.Parse(columns[11], CultureInfo.InvariantCulture),
                            Battery = int.Parse(columns[12], CultureInfo.InvariantCulture),
                            ContactQuality = int.Parse(columns[13], CultureInfo.InvariantCulture),
                            SlideIndex = int.Parse(columns[14], CultureInfo.InvariantCulture),
                            SetIndex = int.Parse(columns[15], CultureInfo.InvariantCulture)
                        };

                        samples.Add(sample);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Fajl: {filePath} | Red: {rowIndex} | Greška: {ex.Message} | Sirov red: {line}");
                    }
                    finally
                    {
                        rowIndex++;
                    }
                }
            }

            return samples;
        }
    }
}
