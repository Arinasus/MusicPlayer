using Microsoft.AspNetCore.Mvc;
using MusicStore.Models;
using MusicStore.Services;
using NAudio.Wave;
using System.IO.Compression;

namespace MusicStore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SongsController : ControllerBase
    {
        private readonly ISongRepository _repo;
        private readonly IImageService _imageService;

        public SongsController(ISongRepository repo, IImageService imageService)
        {
            _repo = repo;
            _imageService = imageService;
        }

        [HttpGet]
        public async Task<IActionResult> GetSongs(
            int page = 1,
            string lang = "en",
            long seed = 12345,
            double likes = 3.7,
            int count = 10
        )
        {
            var songs = await SongGenerator.GenerateSong(page, lang, seed, likes, count);

            // Храним только метаданные в памяти, без аудио
            foreach (var song in songs)
            {
                await _repo.UpdateAsync(song);
            }

            return Ok(songs);
        }

        [HttpPost("exportZip")]
        public async Task<IActionResult> ExportZip([FromBody] ExportRequest req)
        {
            var songs = await SongGenerator.GenerateSong(
                req.Page,
                req.Lang,
                req.Seed,
                req.Likes,
                req.Count
            );

            var ms = new MemoryStream(); // НЕ using — ASP.NET сам закроет

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var song in songs)
                {
                    var safeName = string.Join("_", new[] { song.Title, song.Album, song.Artist });
                    foreach (var c in Path.GetInvalidFileNameChars())
                        safeName = safeName.Replace(c, '_');

                    var entry = archive.CreateEntry($"{safeName}.wav");

                    using var entryStream = entry.Open();
                    using var audioStream = new MemoryStream();

                    GenerateSongAudio(song, audioStream);
                    audioStream.Position = 0;

                    audioStream.CopyTo(entryStream);
                }
            }

            ms.Position = 0;
            return File(ms.ToArray(), "application/zip", "songs.zip");
        }

        public class ExportRequest
        {
            public int Page { get; set; }
            public string Lang { get; set; } = "en";
            public long Seed { get; set; } = 12345;
            public double Likes { get; set; } = 3.7;
            public int Count { get; set; } = 10;
        }

        [HttpGet("{id}/cover")]
        public async Task<IActionResult> GetCover(int id)
        {
            var song = await _repo.GetByIdAsync(id);
            if (song == null) return NotFound();

            if (!string.IsNullOrEmpty(song.CoverImageUrl))
                return Ok(new { cover = song.CoverImageUrl });

            var coverUrl = await _imageService.GenerateCoverAsync(song.Title, song.Artist, song.Genre);
            song.CoverImageUrl = coverUrl;
            await _repo.UpdateAsync(song);

            return Ok(new { cover = coverUrl });
        }

        private static void GenerateSongAudio(Song song, Stream output)
        {
            int sampleRate = 44100;
            // ВАЖНО: не оборачиваем writer в using, чтобы не закрыть output
            var writer = new WaveFileWriter(output, new WaveFormat(sampleRate, 1));
            double noteDuration = 0.5;

            if (song.Notes == null || song.Notes.Count == 0)
                song.Notes = new List<string> { "C4", "E4", "G4" };

            foreach (var noteName in song.Notes)
            {
                var freq = NoteFrequencies[noteName];
                int samplesPerNote = (int)(sampleRate * noteDuration);

                for (int i = 0; i < samplesPerNote; i++)
                {
                    double t = (double)i / sampleRate;
                    short sample = (short)(Math.Sin(2 * Math.PI * freq * t) * short.MaxValue);
                    byte[] buffer = BitConverter.GetBytes(sample);
                    writer.Write(buffer, 0, buffer.Length);
                }
            }

            writer.Flush();
        }

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
