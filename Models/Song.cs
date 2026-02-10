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
        public string Review { get; set; } = "";
        public string CoverImageBase64 { get; set; } = ""; // обложка
        public byte[] AudioPreview { get; set; } = Array.Empty<byte>(); // mp3/миди превью
        public List<string> Lyrics { get; set; } = new(); // для синхронного скролла
    }
}
