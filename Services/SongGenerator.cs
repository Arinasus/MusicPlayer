using MusicStore.Models;
using Bogus;

namespace MusicStore.Services
{
    public static class SongGenerator
    {
        private static readonly string[] SupportedLocales = { "en_US", "de_DE", "uk_UA" };
        private static readonly string[] NoteSet = { "C4", "D4", "E4", "F4", "G4", "A4", "B4" };

        public static async Task<List<Song>> GenerateSong(int page, string lang, long seed, double avgLikes, int count = 10)
        {
            var dataSeed = (int)(seed ^ page);
            Randomizer.Seed = new Random(dataSeed);
            var rngData = new Random(dataSeed);

            var locale = SupportedLocales.Contains(lang) ? lang : "en_US";
            var faker = new Faker(locale);

            var songs = new List<Song>();

            for (int i = 1; i <= count; i++)
            {
                int index = (page - 1) * count + i;

                var title = faker.Commerce.ProductName();
                var artist = faker.Name.FullName();
                var album = rngData.NextDouble() > 0.5 ? faker.Commerce.ProductName() : "Single";
                var genre = faker.Music.Genre();

                var rngLikes = new Random((int)(seed ^ (page * 1000 + i)));
                int likes = GenerateLikes(rngLikes, avgLikes);

                var rngNotes = new Random((int)(seed ^ (page * 2000 + i)));
                var notes = new List<string>();
                for (int n = 0; n < 8; n++)
                    notes.Add(NoteSet[rngNotes.Next(NoteSet.Length)]);

                int duration = (int)(notes.Count * 0.5);

                var review = await GetReviewFromApi(locale);
                var coverPrompt = $"{genre} album cover, {artist}"; 
                var coverImageBase64 = await GenerateCoverFromApi(coverPrompt);
                songs.Add(new Song
                {
                    Index = index,
                    Title = title,
                    Artist = artist,
                    Album = album,
                    Genre = genre,
                    Likes = likes,
                    Notes = notes,
                    Duration = duration,
                    Review = review, 
                    CoverImageBase64 = coverImageBase64
                });
            }

            return songs;
        }

        private static int GenerateLikes(Random rng, double avg)
        {
            int baseLikes = (int)Math.Floor(avg);
            double prob = avg - baseLikes;
            return baseLikes + (rng.NextDouble() < prob ? 1 : 0);
        }

        private static async Task<string> GetReviewFromApi(string lang)
        {
            var token = Environment.GetEnvironmentVariable("HF_API_TOKEN");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var url = $"https://api-inference.huggingface.co/datasets/amazon_reviews_multi/{lang}";
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return "No review available";

            var json = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("review_body", out var review))
                return review.GetString() ?? "No review";

            return "No review";
        }
        private static async Task<string> GenerateCoverFromApi(string prompt)
{
    var token = Environment.GetEnvironmentVariable("HF_API_TOKEN");
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    var url = "https://api-inference.huggingface.co/models/runwayml/stable-diffusion-v1-5";
    var payload = new { inputs = prompt };
    var json = System.Text.Json.JsonSerializer.Serialize(payload);

    var response = await client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
    if (!response.IsSuccessStatusCode) return null;

    var bytes = await response.Content.ReadAsByteArrayAsync();
    return Convert.ToBase64String(bytes); // вернём base64 для фронта
}

    }
}
