using MusicStore.Models;
using Bogus;
using SkiaSharp;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace MusicStore.Services
{
    public static class SongGenerator
    {
        private static readonly string[] SupportedLocales = { "en", "de", "uk" };
        private static readonly string[] NoteSet = { "C4", "D4", "E4", "F4", "G4", "A4", "B4" };

        // Загружаем отзывы один раз при старте
        private static readonly Dictionary<string, string[]> Reviews = LoadReviews();

        private static Dictionary<string, string[]> LoadReviews()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "reviews.json");
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(json)
                   ?? new Dictionary<string, string[]>();
        }

        public static async Task<List<Song>> GenerateSong(int page, string lang, long seed, double avgLikes, int count = 10)
        {
            var dataSeed = (int)(seed ^ page);
            Randomizer.Seed = new Random(dataSeed);
            var rng = new Random(dataSeed);

            var locale = SupportedLocales.Contains(lang) ? lang : "en";

            // Bogus для английского
            var faker = new Faker("en");

            // Загружаем словари для DE/UK
            LocaleData? dict = null;
            if (locale != "en")
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Resources", $"{locale}.json");
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                dict = JsonSerializer.Deserialize<LocaleData>(json, options)
                       ?? throw new Exception($"Locale file {locale}.json is invalid");

            }

            var songs = new List<Song>();

            for (int i = 1; i <= count; i++)
            {
                int index = (page - 1) * count + i;

                string title;
                string artist;
                string album;
                string genre;

                if (locale == "en")
                {
                    // Полностью Bogus
                    title = faker.Commerce.ProductName();
                    artist = rng.NextDouble() > 0.5 ? faker.Name.FullName() : faker.Company.CompanyName();
                    album = rng.NextDouble() > 0.5 ? faker.Commerce.ProductName() : "Single";
                    genre = faker.Music.Genre();
                }
                else
                {
                    // Локализованные словари
                    title = GenerateTitle(dict!, rng);
                    artist = GenerateArtist(dict!, rng);
                    album = rng.NextDouble() > 0.5 ? GenerateAlbum(dict!, rng) : "Single";

                    // жанры можно оставить от Bogus — они универсальны
                    genre = faker.Music.Genre();
                }

                // Likes — детерминированно
                var rngLikes = new Random((int)(seed ^ (page * 1000 + i)));
                int likes = GenerateLikes(rngLikes, avgLikes);

                // Notes — детерминированно
                var rngNotes = new Random((int)(seed ^ (page * 2000 + i)));
                var notes = new List<string>();
                for (int n = 0; n < 8; n++)
                    notes.Add(NoteSet[rngNotes.Next(NoteSet.Length)]);

                int duration = (int)(notes.Count * 0.5);

                // Reviews — локализованные
                var review = GetRandomReview(locale, rng);

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
                    CoverImageBase64 = null,
                    Review = review,
                    CoverImageUrl = null
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

        private static string GetRandomReview(string locale, Random rng)
        {
            var reviewSet = Reviews.ContainsKey(locale) ? Reviews[locale] : Reviews["en"];
            return reviewSet[rng.Next(reviewSet.Length)];
        }

        private static async Task<string?> GenerateCoverImage(string title, string artist, string genre, long seed)
{
    var token = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");
    if (string.IsNullOrEmpty(token))
    {
        Console.WriteLine("REPLICATE_API_TOKEN is not set!");
        return GenerateEmptyPng();
    }

    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Token", token);

    var url = "https://api.replicate.com/v1/predictions";

    var payload = new
    {
        version = "black-forest-labs/flux-1.1-pro",
        input = new
        {
            prompt = $"{genre} abstract album cover background, {title} by {artist}",
            aspect_ratio = "1:1",
            output_format = "png",
            output_quality = 80
        }
    };

    var json = JsonSerializer.Serialize(payload);
    var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));

    if (!response.IsSuccessStatusCode)
    {
        var errorJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Replicate API failed: {response.StatusCode}, body: {errorJson}");
        return GenerateEmptyPng();
    }

    var resultJson = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(resultJson);

    if (doc.RootElement.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Array)
    {
        var imageUrl = outputElement[0].GetString();
        if (!string.IsNullOrEmpty(imageUrl))
        {
            try
            {
                var imageBytes = await client.GetByteArrayAsync(imageUrl);
                return Convert.ToBase64String(imageBytes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cover generation exception: {ex.Message}");
                return GenerateEmptyPng();
            }
        }
    }

    Console.WriteLine("Replicate API did not return an image URL.");
    return GenerateEmptyPng();
}

private static string GenerateEmptyPng()
{
    // Минимальный PNG в base64 (1x1 прозрачный пиксель)
    // Это стандартный заглушечный PNG
    const string emptyPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+X2XcAAAAASUVORK5CYII=";
    return emptyPngBase64;
}
        private static string GenerateTitle(LocaleData dict, Random rng)
        {
            var w1 = dict.TitleWords[rng.Next(dict.TitleWords.Length)];
            var w2 = dict.AlbumWords[rng.Next(dict.AlbumWords.Length)];
            return $"{w1} {w2}";
        }

        private static string GenerateAlbum(LocaleData dict, Random rng)
        {
            var w1 = dict.AlbumWords[rng.Next(dict.AlbumWords.Length)];
            var w2 = dict.TitleWords[rng.Next(dict.TitleWords.Length)];
            return $"{w1} {w2}";
        }

        private static string GenerateArtist(LocaleData dict, Random rng)
        {
            if (rng.NextDouble() > 0.5)
            {
                var first = dict.ArtistFirstNames[rng.Next(dict.ArtistFirstNames.Length)];
                var last = dict.ArtistLastNames[rng.Next(dict.ArtistLastNames.Length)];
                return $"{first} {last}";
            }
            else
            {
                var w1 = dict.BandWords[rng.Next(dict.BandWords.Length)];
                var w2 = dict.TitleWords[rng.Next(dict.TitleWords.Length)];
                return $"{w1} {w2}";
            }
        }


    }
}
