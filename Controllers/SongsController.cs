using Microsoft.AspNetCore.Mvc;
using MusicStore.Models;
using MusicStore.Services;
using NAudio.Lame;
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
        // IMPORTANT: Songs must be reproducible based on seed + page + index.
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
                await _repo.UpdateAsync(song); // cache metadata only

            return Ok(songs);
        }

        // IMPORTANT: Returns MP3 audio for a specific song.
        // IMPORTANT: Audio must be deterministic and generated on demand.
        [HttpGet("{id}/audio")]
        public async Task<IActionResult> GetAudio(int id)
        {
            var song = await _repo.GetByIdAsync(id);
            if (song == null)
                return NotFound();

            var ms = new MemoryStream();
            GenerateSongAudioMp3(song, ms);
            ms.Position = 0;

            return File(ms, "audio/mpeg");
        }

        // IMPORTANT: Export ZIP containing MP3 files.
        // IMPORTANT: File names must include title + album + artist.
        [HttpPost("exportZip")]
        public async Task<IActionResult> ExportZip([FromBody] ExportRequest req)
        {
            var songs = await SongGenerator.GenerateSong(
                req.Page,
                req.Lang,
                req.Seed,
                req.Likes,
                req.Count);

            var ms = new MemoryStream();

            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var song in songs)
                {
                    var safeName = $"{song.Title}_{song.Album}_{song.Artist}";
                    foreach (var c in Path.GetInvalidFileNameChars())
                        safeName = safeName.Replace(c, '_');

                    var entry = archive.CreateEntry($"{safeName}.mp3");

                    using var entryStream = entry.Open();
                    using var audioStream = new MemoryStream();

                    GenerateSongAudioMp3(song, audioStream);
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

        // IMPORTANT: Generates MP3 audio using NAudio + LAME encoder.
        private static void GenerateSongAudioMp3(Song song, Stream output)
        {
            int sampleRate = 44100;
            double noteDuration = 0.5;

            using var pcmStream = new MemoryStream();
            using (var writer = new WaveFileWriter(pcmStream, new WaveFormat(sampleRate, 1)))
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

            pcmStream.Position = 0;

            using var reader = new WaveFileReader(pcmStream);
            using var mp3Writer = new LameMP3FileWriter(output, reader.WaveFormat, 128);
            reader.CopyTo(mp3Writer);
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
