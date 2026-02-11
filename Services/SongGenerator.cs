using MusicStore.Models;
using Bogus;
using System.Text.Json;

namespace MusicStore.Services
{
    // IMPORTANT: Generates deterministic song metadata based on seed, page, index, and locale.
    // NOTE: Audio is generated separately; this class only produces metadata.
    public static class SongGenerator
    {
        private static readonly string[] SupportedLocales = { "en", "de", "uk" };

        // NOTE: Simple note set used for deterministic audio generation.
        private static readonly string[] NoteSet = { "C4", "D4", "E4", "F4", "G4", "A4", "B4" };

        // IMPORTANT: Reviews are loaded once at startup.
        private static readonly Dictionary<string, string[]> Reviews = LoadReviews();

        private static Dictionary<string, string[]> LoadReviews()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Data", "reviews.json");
            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<Dictionary<string, string[]>>(json)
                   ?? new Dictionary<string, string[]>();
        }

        // IMPORTANT: Generates a deterministic batch of songs.
        // NOTE: Titles, artists, albums, genres, notes depend ONLY on seed + page + index + lang.
        // NOTE: Likes depend ONLY on avgLikes + seed + index.
        public static async Task<List<Song>> GenerateSong(
            int page,
            string lang,
            long seed,
            double avgLikes,
            int count = 10)
        {
            var locale = SupportedLocales.Contains(lang) ? lang : "en";

            // Bogus is used only for English
            var faker = new Faker("en");

            // Load locale dictionary for DE/UK
            LocaleData? dict = null;
            if (locale != "en")
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Resources", $"{locale}.json");
                var json = File.ReadAllText(path);

                dict = JsonSerializer.Deserialize<LocaleData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new Exception($"Locale file {locale}.json is invalid");
            }

            var songs = new List<Song>();

            for (int i = 1; i <= count; i++)
            {
                int index = (page - 1) * count + i;

                // IMPORTANT: Independent RNG streams for deterministic behavior
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
                    // English uses Bogus entirely
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
                    // Localized generation for DE/UK
                    title = GenerateTitle(dict!, rngTitle);
                    artist = GenerateArtist(dict!, rngArtist);
                    album = rngAlbum.NextDouble() > 0.5
                        ? GenerateAlbum(dict!, rngAlbum)
                        : "Single";
                    genre = dict!.Genres[rngGenre.Next(dict.Genres.Length)];
                }

                // Likes depend only on avgLikes + seed + index
                int likes = GenerateLikes(avgLikes, seed, index);

                // Deterministic note sequence
                var notes = new List<string>();
                for (int n = 0; n < 8; n++)
                    notes.Add(NoteSet[rngNotes.Next(NoteSet.Length)]);

                // Simple duration: 0.5 seconds per note
                int duration = (int)(notes.Count * 0.5);

                // Localized review
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
                    Review = review,
                    CoverImageUrl = null
                });
            }

            return await Task.FromResult(songs);
        }

        // IMPORTANT: Fractional likes implemented probabilistically.
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
