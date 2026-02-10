using MusicStore.Models;
using Bogus;
using System.Globalization;

namespace MusicStore.Services
{
    public static class SongGenerator
    {
        private static readonly Dictionary<string, string> LocaleMapping = new()
        {
            ["en"] = "en_US",
            ["de"] = "de_DE",
            ["uk"] = "uk_UA",
            ["fr"] = "fr_FR",
            ["it"] = "it_IT",
            ["es"] = "es_ES",
            ["pt"] = "pt_BR",
            ["ar"] = "ar_SA"
        };

        private static readonly string[] NoteSet = { "C4", "D4", "E4", "F4", "G4", "A4", "B4" };

        public static List<Song> GenerateSong(int page, string lang, long seed, double avgLikes, int count = 10)
        {
            var dataSeed = (int)(seed ^ page);
            Randomizer.Seed = new Random(dataSeed);
            var rngData = new Random(dataSeed);

            // Получаем правильную локаль для Bogus
            var locale = LocaleMapping.ContainsKey(lang) ? LocaleMapping[lang] : "en_US";
            
            // Создаем Faker с указанием локали
            var faker = new Faker(locale);

            var songs = new List<Song>();

            for (int i = 1; i <= count; i++)
            {
                int index = (page - 1) * count + i;

                // Генерация данных с использованием локализованного Faker
                var title = GenerateSongTitle(faker, locale);
                var artist = GenerateArtist(faker, locale);
                var album = rngData.NextDouble() > 0.5 ? GenerateAlbumTitle(faker, locale) : "Single";
                var genre = faker.Music.Genre();

                var rngLikes = new Random((int)(seed ^ (page * 1000 + i)));
                int likes = GenerateLikes(rngLikes, avgLikes);

                var rngNotes = new Random((int)(seed ^ (page * 2000 + i)));
                var notes = new List<string>();
                for (int n = 0; n < 8; n++)
                    notes.Add(NoteSet[rngNotes.Next(NoteSet.Length)]);

                int duration = (int)(notes.Count * 0.5);
                
                var review = faker.Lorem.Sentence();
                
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
                    Review = review
                });
            }

            return songs;
        }

        private static string GenerateSongTitle(Faker faker, string locale)
        {
            // Разные паттерны для разных языков
            switch (locale.Split('_')[0])
            {
                case "de":
                    return $"{faker.Hacker.Adjective()} {faker.Hacker.Noun()}";
                case "uk":
                    return $"{faker.Commerce.ProductAdjective()} {faker.Commerce.ProductName()}";
                case "fr":
                    return $"{faker.Lorem.Word()} {faker.Lorem.Word()}";
                case "it":
                case "es":
                case "pt":
                    return $"{faker.Commerce.Product()} {faker.Commerce.Color()}";
                case "ar":
                    // Для арабского используем отдельные наборы слов
                    var arabicWords = new[] { "حب", "ليل", "روح", "قلب", "حلم", "بحر", "نجم", "شمس" };
                    var arabicAdjectives = new[] { "جميل", "كبير", "صغير", "قديم", "جديد", "سريع", "بطيء" };
                    var random = new Random();
                    return $"{arabicAdjectives[random.Next(arabicAdjectives.Length)]} {arabicWords[random.Next(arabicWords.Length)]}";
                default: // en и другие
                    return $"{faker.Commerce.ProductAdjective()} {faker.Commerce.ProductName()}";
            }
        }

        private static string GenerateArtist(Faker faker, string locale)
        {
            var random = new Random();
            
            // 50% chance для имени человека, 50% для названия группы
            if (random.NextDouble() > 0.5)
            {
                return faker.Name.FullName();
            }
            else
            {
                switch (locale.Split('_')[0])
                {
                    case "de":
                        return $"{faker.Company.CompanyName()} {faker.Hacker.Abbreviation()}";
                    case "uk":
                        return $"{faker.Commerce.ProductAdjective()} {faker.Commerce.Product()}";
                    case "fr":
                        return $"Les {faker.Lorem.Word()}s";
                    case "it":
                        return $"I {faker.Lorem.Word()}";
                    case "es":
                        return $"Los {faker.Lorem.Word()}";
                    case "pt":
                        return $"{faker.Company.CompanyName()}";
                    case "ar":
                        var arabicBandNames = new[] { "الفرقة", "الأصدقاء", "الليل", "النهار", "الروح", "القلب" };
                        var arabicPrefixes = new[] { "فرقة", "مجموعة", "أصدقاء" };
                        return $"{arabicPrefixes[random.Next(arabicPrefixes.Length)]} {arabicBandNames[random.Next(arabicBandNames.Length)]}";
                    default:
                        return $"{faker.Company.CompanyName()} {faker.Company.Bs()}";
                }
            }
        }

        private static string GenerateAlbumTitle(Faker faker, string locale)
        {
            switch (locale.Split('_')[0])
            {
                case "de":
                    return $"{faker.Hacker.Verb()} {faker.Hacker.Noun()}";
                case "uk":
                    return $"{faker.Commerce.ProductMaterial()} {faker.Commerce.Product()}";
                case "fr":
                    return $"{faker.Lorem.Word(1)} {faker.Lorem.Word(1)}";
                case "it":
                    return $"Il {faker.Lorem.Word()} {faker.Commerce.Color()}";
                case "es":
                    return $"El {faker.Lorem.Word()} {faker.Commerce.Color()}";
                case "pt":
                    return $"{faker.Commerce.ProductName()} {faker.Commerce.ProductAdjective()}";
                case "ar":
                    var arabicAlbumWords = new[] { "رحلة", "حكاية", "أغنية", "ذكرى", "لوحة", "قصة" };
                    var arabicAdjectives = new[] { "طويلة", "قصيرة", "جميلة", "حزينة", "سعيدة", "غريبة" };
                    var random = new Random();
                    return $"{arabicAlbumWords[random.Next(arabicAlbumWords.Length)]} {arabicAdjectives[random.Next(arabicAdjectives.Length)]}";
                default:
                    return $"{faker.Commerce.ProductName()} {faker.Commerce.ProductAdjective()}";
            }
        }

        private static int GenerateLikes(Random rng, double avg)
        {
            int baseLikes = (int)Math.Floor(avg);
            double prob = avg - baseLikes;
            return baseLikes + (rng.NextDouble() < prob ? 1 : 0);
        }
    }
}