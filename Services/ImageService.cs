using System.Drawing;
using System.Drawing.Imaging;
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

        // 👉 Fallback: градиент + текст через System.Drawing.Common
        private string GenerateFallbackCover(string title, string artist)
        {
            using var bmp = new Bitmap(512, 512);
            using var g = Graphics.FromImage(bmp);

            // градиентный фон
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, 512, 512),
                Color.DeepSkyBlue,
                Color.MediumVioletRed,
                45f);
            g.FillRectangle(brush, 0, 0, 512, 512);

            // текст альбома
            using var fontTitle = new Font("Arial", 32, FontStyle.Bold);
            using var fontArtist = new Font("Arial", 24, FontStyle.Regular);
            g.DrawString(title, fontTitle, Brushes.White, new PointF(20, 220));
            g.DrawString(artist, fontArtist, Brushes.White, new PointF(20, 270));

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
        }
    }
}
