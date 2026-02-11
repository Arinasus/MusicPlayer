using MusicStore.Models;
using Bogus;
using SkiaSharp;
using System.Text.Json;

namespace MusicStore.Services
{
    public static class SongGenerator
    {
        private static readonly string[] SupportedLocales = { "en_US", "de_DE", "uk_UA" };
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
            var rngData = new Random(dataSeed);

            var locale = SupportedLocales.Contains(lang) ? lang : "en_US";
            var faker = new Faker(locale);

            var songs = new List<Song>();

            for (int i = 1; i <= count; i++)
            {
                int index = (page - 1) * count + i;

                // Song title — случайное название
                var title = faker.Commerce.ProductName();

                // Artist — имя или группа
                string artist = rngData.NextDouble() > 0.5
                    ? faker.Name.FullName()
                    : faker.Company.CompanyName();

                // Album — случайное название или "Single"
                string album = rngData.NextDouble() > 0.5
                    ? faker.Commerce.ProductName()
                    : "Single";

                // Genre
                var genre = faker.Music.Genre();

                // Likes
                var rngLikes = new Random((int)(seed ^ (page * 1000 + i)));
                int likes = GenerateLikes(rngLikes, avgLikes);

                // Notes
                var rngNotes = new Random((int)(seed ^ (page * 2000 + i)));
                var notes = new List<string>();
                for (int n = 0; n < 8; n++)
                    notes.Add(NoteSet[rngNotes.Next(NoteSet.Length)]);

                int duration = (int)(notes.Count * 0.5);

                // Cover image
                var coverImageBase64 = await GenerateCoverImage(title, artist, genre, seed);

                // Review из Data/reviews.json
                var review = GetRandomReview(locale, rngData);

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
                    CoverImageBase64 = coverImageBase64,
                    Review = review
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
            var reviewSet = Reviews.ContainsKey(locale) ? Reviews[locale] : Reviews["en_US"];
            return reviewSet[rng.Next(reviewSet.Length)];
        }

        private static async Task<string?> GenerateCoverImage(string title, string artist, string genre, long seed)
        {
            var token = Environment.GetEnvironmentVariable("REPLICATE_API_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("REPLICATE_API_TOKEN is not set!");
                return GenerateFallbackCover(title, artist);
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
            var response = await client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Replicate API failed: {response.StatusCode}, body: {errorJson}");
                return GenerateFallbackCover(title, artist);
            }

            var resultJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(resultJson);

            // Replicate возвращает URL картинки в массиве "output"
            if (doc.RootElement.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Array)
            {
                var imageUrl = outputElement[0].GetString();
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    try
                    {
                        var imageBytes = await client.GetByteArrayAsync(imageUrl);
                        using var bitmap = SKBitmap.Decode(imageBytes);
                        if (bitmap == null)
                        {
                            Console.WriteLine("SKBitmap.Decode returned null — данные не картинка");
                            return GenerateFallbackCover(title, artist);
                        }

                        using var canvas = new SKCanvas(bitmap);
                        using var paint = new SKPaint
                        {
                            Color = SKColors.White,
                            TextSize = 48,
                            IsAntialias = true,
                            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                        };

                        canvas.DrawText(title, 50, 100, paint);
                        canvas.DrawText(artist, 50, 160, paint);

                        using var image = SKImage.FromBitmap(bitmap);
                        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                        return Convert.ToBase64String(data.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Cover generation exception: {ex.Message}");
                        return GenerateFallbackCover(title, artist);
                    }
                }
            }

            Console.WriteLine("Replicate API did not return an image URL.");
            return GenerateFallbackCover(title, artist);
        }

        private static string GenerateFallbackCover(string title, string artist)
        {
            using var bitmap = new SKBitmap(400, 400);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.DarkSlateBlue);

            using var paint = new SKPaint { Color = SKColors.White, TextSize = 32, IsAntialias = true };
            canvas.DrawText(title, 20, 200, paint);
            canvas.DrawText(artist, 20, 250, paint);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return Convert.ToBase64String(data.ToArray());
        }
    }
}
