using MusicStore.Models;

namespace MusicStore.Services
{
    public class InMemorySongRepository : ISongRepository
    {
        private readonly List<Song> _songs = new();

        public InMemorySongRepository()
        {
            // Можно добавить тестовые песни
            _songs.Add(new Song
            {
                Index = 1,
                Title = "Test Song",
                Artist = "Demo Artist",
                Album = "Demo Album",
                Genre = "Pop",
                Likes = 5,
                Notes = new List<string> { "C4", "E4", "G4" },
                Duration = 3,
                Review = "Nice demo track"
            });
        }

        public Task<Song?> GetByIdAsync(int id)
        {
            var song = _songs.FirstOrDefault(s => s.Index == id);
            return Task.FromResult(song);
        }

        public Task UpdateAsync(Song song)
        {
            var existing = _songs.FirstOrDefault(s => s.Index == song.Index);
            if (existing != null)
            {
                // Обновляем поля
                existing.Title = song.Title;
                existing.Artist = song.Artist;
                existing.Album = song.Album;
                existing.Genre = song.Genre;
                existing.Likes = song.Likes;
                existing.Notes = song.Notes;
                existing.Duration = song.Duration;
                existing.Review = song.Review;
                existing.CoverImageUrl = song.CoverImageUrl;
                existing.CoverImageBase64 = song.CoverImageBase64;
                existing.AudioPreview = song.AudioPreview;
            }
            else
            {
                _songs.Add(song);
            }

            return Task.CompletedTask;
        }
    }
}
