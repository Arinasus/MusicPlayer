namespace MusicStore.Models
{
    public class Song
    {
        public int Index { get; set; }
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Album { get; set; } = "";
        public string Genre { get; set; } = "";
        public int Likes { get; set; }
        public List<string> Notes { get; set; } = new();
        public int Duration { get; set; }
        public string Review { get; set; } ="";
        public string Language { get; set; } = "en";
        public string CoverImage { get; set; } = string.Empty;
    }
}
