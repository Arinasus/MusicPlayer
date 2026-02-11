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

        // IMPORTANT: Returns a generated batch of songs.
        // IMPORTANT: Only metadata is cached; audio is generated on demand.
        [HttpGet]
        public async Task<IActionResult> GetSongs(
            int page = 1,
            string lang = "en",
            long seed = 12345,
            double likes = 3.7,
            int count = 10)
        {
            var songs = await SongGenerator.GenerateSong(page, lang, seed, likes, count);

            foreach (var song in songs)
                await _repo.UpdateAsync(song);

            return Ok(songs);
        }

        // IMPORTANT: Returns WAV audio for a specific song.
        [HttpGet("{id}/audio")]
        public async Task<IActionResult> GetAudio(int id)
        {
            var song = await _repo.GetByIdAsync(id);
            if (song == null)
                return NotFound();

            var bytes = GenerateSongAudioWav(song);
            return File(bytes, "audio/wav");
        }

        // IMPORTANT: Export ZIP containing WAV files.
        [HttpPost("exportZip")]
        public async Task<IActionResult> ExportZip([FromBody] ExportRequest req)
        {
            var songs = await SongGenerator.GenerateSong(
                req.Page,
                req.Lang,
                req.Seed,
                req.Likes,
                req.Count);

            using var ms = new MemoryStream();

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var song in songs)
                {
                    var safeName = $"{song.Title}_{song.Album}_{song.Artist}";
                    foreach (var c in Path.GetInvalidFileNameChars())
                        safeName = safeName.Replace(c, '_');

                    var entry = archive.CreateEntry($"{safeName}.wav");

                    using var entryStream = entry.Open();
                    var audioBytes = GenerateSongAudioWav(song);
                    entryStream.Write(audioBytes, 0, audioBytes.Length);
                }
            }

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

        // IMPORTANT: Cover images are generated lazily and cached.
        [HttpGet("{id}/cover")]
        public async Task<IActionResult> GetCover(int id)
        {
            var song = await _repo.GetByIdAsync(id);
            if (song == null)
                return NotFound();

            if (!string.IsNullOrEmpty(song.CoverImageUrl))
                return Ok(new { cover = song.CoverImageUrl });

            var coverUrl = await _imageService.GenerateCoverAsync(song.Title, song.Artist, song.Genre);
            song.CoverImageUrl = coverUrl;
            await _repo.UpdateAsync(song);

            return Ok(new { cover = coverUrl });
        }

        // IMPORTANT: Generates deterministic WAV audio for a song.
        private static byte[] GenerateSongAudioWav(Song song)
        {
            int sampleRate = 44100;
            double noteDuration = 0.5;

            using var ms = new MemoryStream();
            using (var writer = new WaveFileWriter(ms, new WaveFormat(sampleRate, 1)))
            {
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
                        writer.WriteSample(sample / 32768f);
                    }
                }
            }

            return ms.ToArray();
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
