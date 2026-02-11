using MusicStore.Models;
namespace MusicStore.Models
{
    // IMPORTANT: Repository interface for caching generated song metadata. 
    // NOTE: Only metadata is stored. audio is always generated on demand.
    public interface ISongRepository { 
        Task<Song?> GetByIdAsync(int id); 
        Task UpdateAsync(Song song); 
    }
}
