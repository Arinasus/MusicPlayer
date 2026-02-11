using MusicStore.Models;
using Bogus;
using System.Text.Json;

namespace MusicStore.Services
{
    public static class SongGenerator
    {
        private static readonly string[] SupportedLocales = { "en", "de", "uk" };

        // Набор нот для простого генератора мелодий
        private static readonly string[] NoteSet = { "C4", "D4", "E4", "F4", "G4", "A4", "B4" };

        // Отзывы загружаем один раз
        private static readonly Dictionary<string, string[]> Reviews = LoadReviews();

        private static Dictionary<string, string[]> LoadReviews()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "reviews.json");
            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(json)
                   ?? new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Генерация списка песен.
        /// ВАЖНО: titles / artists / albums / genres / notes зависят только от seed + page + index + lang.
        /// Likes зависят только от avgLikes + seed + index.
        /// </summary>
        public static async Task<List<Song>> GenerateSong(int page, string lang, long seed, double avgLikes, int count = 10)
        {
            var dataSeed = (int)(seed ^ page);
            Randomizer.Seed = new Random(dataSeed);

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

                // Отдельные RNG для разных аспектов, чтобы поведение было стабильным и предсказуемым
                var rngTitle = new Random((int)(seed ^ page ^ index ^ 101));
                var rngArtist = new Random((int)(seed ^ page ^ index ^ 202));
                var rngAlbum = new Random((int)(seed ^ page ^ index ^ 303));
                var rngGenre = new Random((int)(seed ^ page ^ index ^ 404));
                var rngNotes = new Random((int)(seed ^ page ^ index ^ 505));
                var rngReview = new Random((int)(seed ^ page ^ index ^ 606));

                string title;
                string artist;
                string album;
                string genre;

                if (locale == "en")
                {
                    // Полностью Bogus для английского
                    title = faker.Commerce.ProductName();
                    artist = rngArtist.NextDouble() > 0.5
                        ? faker.Name.FullName()
                        : faker.Company.CompanyName();
                    album = rngAlbum.NextDouble() > 0.5
                        ? faker.Commerce.ProductName()
                        : "Single";
                    genre = faker.Music.Genre();
                }
                else
                {
                    // Локализованные словари для DE/UK
                    title = GenerateTitle(dict!, rngTitle);
                    artist = GenerateArtist(dict!, rngArtist);
                    album = rngAlbum.NextDouble() > 0.5
                        ? GenerateAlbum(dict!, rngAlbum)
                        : "Single";

                    // Жанр тоже локализованный
                    genre = dict!.Genres[rngGenre.Next(dict.Genres.Length)];
                }

                // Likes — зависят только от avgLikes + seed + index
                int likes = GenerateLikes(avgLikes, seed, index);

                // Notes — детерминированно от seed + page + index
                var notes = new List<string>();
                for (int n = 0; n < 8; n++)
                {
                    notes.Add(NoteSet[rngNotes.Next(NoteSet.Length)]);
                }

                // Простейшая длительность: по 0.5 секунды на ноту
                int duration = (int)(notes.Count * 0.5);

                // Reviews — локализованные
                var review = GetRandomReview(locale, rngReview);

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

            // async-совместимость, если вызывается через await
            return await Task.FromResult(songs);
        }

        /// <summary>
        /// Генерация лайков: дробное значение реализовано вероятностно.
        /// Не зависит от lang, page, только от avgLikes + seed + index.
        /// </summary>
        private static int GenerateLikes(double avgLikes, long seed, int index)
        {
            var rng = new Random((int)(seed ^ index ^ 9999));

            int baseLikes = (int)Math.Floor(avgLikes);
            double prob = avgLikes - baseLikes;

            return baseLikes + (rng.NextDouble() < prob ? 1 : 0);
        }

        private static string GetRandomReview(string locale, Random rng)
        {
            var reviewSet = Reviews.ContainsKey(locale) ? Reviews[locale] : Reviews["en"];
            return reviewSet[rng.Next(reviewSet.Length)];
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
