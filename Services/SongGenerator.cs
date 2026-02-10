using MusicStore.Models;
using System.Globalization;
using Bogus;
using MusicStore.Controllers;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using static Bogus.DataSets.Name;
namespace MusicStore.Services
{
    public class SongGenerator
    {
        public static List<Song> GenerateSong(int page, string lang, long seed, double avgLikes, int count = 10 )
        {
            var dataSeed = (int)(seed ^ page); 
            var rngData = new Random(dataSeed);

            var locale = lang switch
            {
                "en" => "en",
                "de" => "de",
                "uk" => "uk",
                _ => "en"
            };
            var faker = new Faker(locale);
            faker.Random = new Bogus.Randomizer(dataSeed);
            var songs = new List<Song>();
            for(int i = 1; i <= count; i++)
            {
                int index = (page - 1) * count + i;

                var title = faker.Commerce.ProductName();
                var artist = faker.Name.FullName();
                var album = rngData.NextDouble() > 0.5 ? faker.Commerce.ProductName() : "Single";
                var genre = faker.Music.Genre();


                var rngLikes = new Random((int)(seed ^ (page * 1000 + i))); 
                int likes = GenerateLikes(rngLikes, avgLikes);
                songs.Add(new Song
                {
                    Index = index,
                    Title = title,
                    Artist = artist,
                    Album = album,
                    Genre = genre,
                    Likes = likes
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
    }
}
