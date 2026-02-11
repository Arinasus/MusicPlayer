using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace MusicStore.Services
{
    public interface IImageService
    {
        Task<string> GenerateCoverAsync(string title, string artist, string genre);
    }

    public class ImageService : IImageService
    {
        private readonly HttpClient _client;
        private readonly string _token;

        public ImageService(IConfiguration config)
        {
            _client = new HttpClient();
            _token = config["REPLICATE_API_TOKEN"];
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Token", _token);
        }

        public async Task<string> GenerateCoverAsync(string title, string artist, string genre)
        {
            try
            {
                var url = "https://api.replicate.com/v1/predictions";
                var payload = new
                {
                    version = "black-forest-labs/flux-1.1-pro",
                    input = new
                    {
                        prompt = $"{genre} abstract album cover background, {title} by {artist}",
                        aspect_ratio = "1:1",
                        output_format = "png",
                        output_quality = 80
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var response = await _client.PostAsync(url, new StringContent(json, System.Text.Encoding.UTF8, "application/json"));
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Replicate API failed: {response.StatusCode}, body: {body}");
                    return GenerateFallbackCover(title, artist);
                }

                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Array)
                {
                    var imageUrl = outputElement[0].GetString();
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var imageBytes = await _client.GetByteArrayAsync(imageUrl);
                        return $"data:image/png;base64,{Convert.ToBase64String(imageBytes)}";
                    }
                }

                Console.WriteLine("Replicate API did not return an image URL.");
                return GenerateFallbackCover(title, artist);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cover generation exception: {ex.Message}");
                return GenerateFallbackCover(title, artist);
            }
        }

        // 👉 Fallback: градиент + текст через ImageSharp
        private string GenerateFallbackCover(string title, string artist)
        {
            using var image = new Image<Rgba32>(512, 512);

            int seed = (title + artist).GetHashCode(); 
            var rng = new Random(seed); 
            var startColor = Color.FromRgb( 
                (byte)rng.Next(256), 
                (byte)rng.Next(256), 
                (byte)rng.Next(256)); 
            var endColor = Color.FromRgb( 
                (byte)rng.Next(256), 
                (byte)rng.Next(256), 
                (byte)rng.Next(256));
            image.Mutate(ctx => ctx.Fill(new LinearGradientBrush(
                new PointF(0, 0), 
                new PointF(512, 512), 
                GradientRepetitionMode.None, new[] { 
                new ColorStop(0, startColor), 
                    new ColorStop(1, endColor) 
                })));
            // шрифты
            var fontCollection = new FontCollection();
            var family = fontCollection.Add("Resources/dejavu-fonts-ttf-2.37/ttf/DejaVuSans.ttf");
            var fontTitle = family.CreateFont(32, FontStyle.Bold);
            var fontArtist = family.CreateFont(24, FontStyle.Regular);

            // текст
            image.Mutate(ctx =>
            {
                ctx.DrawText(title, fontTitle, Color.White, new PointF(20, 220));
                ctx.DrawText(artist, fontArtist, Color.White, new PointF(20, 270));
            });

            using var ms = new MemoryStream();
            image.SaveAsPng(ms);
            return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
        }
    }
}
