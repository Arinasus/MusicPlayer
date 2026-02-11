using MusicStore.Models;
namespace MusicStore.Models
{
    public interface ISongRepository { 
        Task<Song?> GetByIdAsync(int id); 
        Task UpdateAsync(Song song); 
    }
}
