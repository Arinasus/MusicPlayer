using MusicStore.Models;
using System.Globalization;
using Bogus;
using MusicStore.Controllers;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using static Bogus.DataSets.Name;
namespace MusicStore.Services
{
    public static class SongGenerator
{
    private static readonly string[] SupportedLocales = { "en", "de", "fr", "it", "es", "pt", "ar" };
    private static readonly string[] NoteSet = { "C4", "D4", "E4", "F4", "G4", "A4", "B4" };

    public static List<Song> GenerateSong(int page, string lang, long seed, double avgLikes, int count = 10)
{
    var dataSeed = (int)(seed ^ page);
    Randomizer.Seed = new Random(dataSeed); // фиксируем сид для Bogus
    var rngData = new Random(dataSeed);

    var locale = SupportedLocales.Contains(lang) ? lang : "en";
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

        songs.Add(new Song
        {
            Index = index,
            Title = title,
            Artist = artist,
            Album = album,
            Genre = genre,
            Likes = likes,
            Notes = notes,
            Duration = duration
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
