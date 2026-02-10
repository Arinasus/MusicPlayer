using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using MusicStore.Models;
using MusicStore.Services;
using NAudio.Wave;
using System.IO.Compression;
using NAudio.Lame;

namespace MusicStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetSongs
        (
            int page = 1,
            string lang = "en",
            long seed = 12345,
            double likes = 3.7,
            int count = 10
        )
        {
            var songs = SongGenerator.GenerateSong(page, lang, seed, likes, count);
            return Ok(songs);
        }

        [HttpPost("exportZip")] 
        public IActionResult ExportZip([FromBody] ExportRequest req) 
        { 
            var songs = SongGenerator.GenerateSong(req.Page, req.Lang, req.Seed, req.Likes, req.Count); 
            using var ms = new MemoryStream(); using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true)) { 
                foreach (var song in songs) { var entryName = $"{song.Title}_{song.Album}_{song.Artist}.mp3"; 
                var entry = archive.CreateEntry(entryName); 
                using var entryStream = entry.Open(); 
                using var waveOut = new MemoryStream(); 
                GenerateSongAudio(song, waveOut); 
                waveOut.Position = 0; 
                waveOut.CopyTo(entryStream); 
                } 
                } ms.Position = 0; 
                return File(ms.ToArray(), "application/zip", "songs.zip"); 
        }
        
        public class ExportRequest { 
            public int Page { get; set; } 
            public string Lang { get; set; } = "en"; 
            public long Seed { get; set; } = 12345; 
            public double Likes { get; set; } = 3.7; 
            public int Count { get; set; } = 10; 
        }
        private static void GenerateSongAudio(Song song, Stream output)
        {
            int sampleRate = 44100; 
            using var writer = new WaveFileWriter(output, new WaveFormat(sampleRate, 1));
            double noteDuration = 0.5; // каждая нота 0.5 сек
                                       foreach (var noteName in song.Notes) { var freq = NoteFrequencies[noteName]; int samplesPerNote = (int)(sampleRate * noteDuration); 
                for (int i = 0; i < samplesPerNote; i++) { double t = (double)i / sampleRate; 
                    short sample = (short)(Math.Sin(2 * Math.PI * freq * t) * short.MaxValue); byte[] buffer = BitConverter.GetBytes(sample); 
                    writer.Write(buffer, 0, buffer.Length); } } }
        private static readonly Dictionary<string, double> NoteFrequencies = new()
{
    { "C4", 261.63 },
    { "D4", 293.66 },
    { "E4", 329.63 },
    { "F4", 349.23 },
    { "G4", 392.00 },
    { "A4", 440.00 },
    { "B4", 493.88 }
};
    }
}
