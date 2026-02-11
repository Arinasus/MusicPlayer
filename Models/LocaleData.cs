namespace MusicStore.Models
{
    // IMPORTANT: Locale-specific word lists used for generating titles, artists, albums, and genres.
    public class LocaleData
    {
        public string[] TitleWords { get; set; } = Array.Empty<string>(); 
        public string[] AlbumWords { get; set; } = Array.Empty<string>(); 
        public string[] ArtistFirstNames { get; set; } = Array.Empty<string>(); 
        public string[] ArtistLastNames { get; set; } = Array.Empty<string>(); 
        public string[] BandWords { get; set; } = Array.Empty<string>(); 
        public string[] Genres { get; set; } = Array.Empty<string>(); 
        public string[] Reviews { get; set; } = Array.Empty<string>();
    }
}
