using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RoofAI
{
    public class FeedbackEntry
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("user_input")]
        public string UserInput { get; set; }

        [JsonProperty("ai_response")]
        public string AiResponse { get; set; }

        [JsonProperty("rating")]
        public int Rating { get; set; }

        [JsonProperty("roof_parameters")]
        public string RoofParameters { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("notes")]
        public string Notes { get; set; }
    }

    public static class FeedbackCollector
    {
        private static readonly string DefaultFeedbackDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoofAI");

        private static readonly string FeedbackFile = Path.Combine(DefaultFeedbackDir, "feedback.jsonl");

        private static int _entryCount = -1;

        public static string SaveFeedback(string userInput, string aiResponse,
            int rating, string roofParameters = "", string notes = "")
        {
            rating = Math.Max(1, Math.Min(5, rating));

            if (_entryCount < 0)
                _entryCount = CountExistingEntries();

            var entry = new FeedbackEntry
            {
                Id = $"fb_{_entryCount:D6}",
                UserInput = userInput,
                AiResponse = aiResponse,
                Rating = rating,
                RoofParameters = roofParameters,
                Timestamp = DateTime.UtcNow.ToString("o"),
                Notes = notes
            };

            _entryCount++;

            Directory.CreateDirectory(DefaultFeedbackDir);

            string json = JsonConvert.SerializeObject(entry, Formatting.None);
            File.AppendAllText(FeedbackFile, json + Environment.NewLine);

            return entry.Id;
        }

        public static List<FeedbackEntry> LoadFeedback(int minRating = 0)
        {
            var entries = new List<FeedbackEntry>();

            if (!File.Exists(FeedbackFile))
                return entries;

            foreach (string line in File.ReadAllLines(FeedbackFile))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var entry = JsonConvert.DeserializeObject<FeedbackEntry>(line);
                    if (entry != null && entry.Rating >= minRating)
                        entries.Add(entry);
                }
                catch { }
            }

            return entries;
        }

        public static FeedbackStats GetStats()
        {
            var all = LoadFeedback();
            if (all.Count == 0)
                return new FeedbackStats();

            double totalRating = 0;
            int[] ratingDist = new int[6];

            foreach (var e in all)
            {
                totalRating += e.Rating;
                if (e.Rating >= 1 && e.Rating <= 5)
                    ratingDist[e.Rating]++;
            }

            return new FeedbackStats
            {
                TotalEntries = all.Count,
                AverageRating = totalRating / all.Count,
                RatingDistribution = ratingDist,
                HighQualityCount = all.FindAll(e => e.Rating >= 4).Count
            };
        }

        public static string ExportForTraining(string outputPath, int minRating = 4)
        {
            var entries = LoadFeedback(minRating);
            if (entries.Count == 0)
                return null;

            var conversations = new List<object>();
            foreach (var entry in entries)
            {
                conversations.Add(new
                {
                    id = entry.Id,
                    category = "user_feedback",
                    messages = new[]
                    {
                        new { role = "user", content = entry.UserInput },
                        new { role = "assistant", content = entry.AiResponse }
                    }
                });
            }

            var output = new { conversations };
            string json = JsonConvert.SerializeObject(output, Formatting.Indented);
            File.WriteAllText(outputPath, json);

            return outputPath;
        }

        private static int CountExistingEntries()
        {
            if (!File.Exists(FeedbackFile))
                return 0;

            int count = 0;
            foreach (string line in File.ReadAllLines(FeedbackFile))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    count++;
            }
            return count;
        }
    }

    public class FeedbackStats
    {
        public int TotalEntries { get; set; }
        public double AverageRating { get; set; }
        public int[] RatingDistribution { get; set; } = new int[6];
        public int HighQualityCount { get; set; }

        public override string ToString()
        {
            return $"Toplam: {TotalEntries}, Ortalama: {AverageRating:F1}/5, " +
                   $"Yuksek Kalite (4+): {HighQualityCount}";
        }
    }
}
