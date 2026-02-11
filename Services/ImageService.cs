using SkiaSharp;
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

        // 👉 Fallback: градиент + текст
        private string GenerateFallbackCover(string title, string artist)
        {
            using var bitmap = new SKBitmap(512, 512);
            using var canvas = new SKCanvas(bitmap);

            // градиентный фон
            var paint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(512, 512),
                    new[] { SKColors.DeepSkyBlue, SKColors.MediumVioletRed },
                    null,
                    SKShaderTileMode.Clamp)
            };
            canvas.DrawRect(new SKRect(0, 0, 512, 512), paint);

            // текст альбома
            var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 32,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            canvas.DrawText(title, 20, 220, textPaint);

            // текст исполнителя
            textPaint.TextSize = 24;
            canvas.DrawText(artist, 20, 270, textPaint);

            canvas.Flush();

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            return $"data:image/png;base64,{Convert.ToBase64String(data.ToArray())}";
        }
    }
}
