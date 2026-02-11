using MusicStore.Models;

namespace MusicStore.Services
{
    // IMPORTANT: Simple in-memory repository used only for caching metadata.
    // NOTE: This is NOT a persistent database. Data resets on application restart.
    public class InMemorySongRepository : ISongRepository
    {
        private readonly List<Song> _songs = new();

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
                // IMPORTANT: Update metadata only
                existing.Title = song.Title;
                existing.Artist = song.Artist;
                existing.Album = song.Album;
                existing.Genre = song.Genre;
                existing.Likes = song.Likes;
                existing.Notes = song.Notes;
                existing.Duration = song.Duration;
                existing.Review = song.Review;
                existing.CoverImageUrl = song.CoverImageUrl;
            }
            else
            {
                _songs.Add(song);
            }

            return Task.CompletedTask;
        }
    }
}
