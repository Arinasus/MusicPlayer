using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace MusicStore.Services
{
    public interface IImageService { 
        Task<string> GenerateCoverAsync(string title, string artist, string genre); }
    public class ImageService : IImageService 
    { 
        private readonly HttpClient _client; 
        private readonly string _token; 
        public ImageService(IConfiguration config) 
        { 
            _client = new HttpClient(); 
            _token = config["REPLICATE_API_TOKEN"]; 
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _token); 
        } 
        public async Task<string> GenerateCoverAsync(string title, string artist, string genre) { 
            var url = "https://api.replicate.com/v1/predictions"; 
            var payload = new { version = "black-forest-labs/flux-1.1-pro", input = new { prompt = $"{genre} abstract album cover background, {title} by {artist}", aspect_ratio = "1:1", output_format = "png", output_quality = 80 } }; var json = JsonSerializer.Serialize(payload); 
            var response = await _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json")); var body = await response.Content.ReadAsStringAsync(); 
            if (!response.IsSuccessStatusCode) { 
                Console.WriteLine($"Replicate API failed: {response.StatusCode}, body: {body}"); return "/images/fallback.png"; } using var doc = JsonDocument.Parse(body); 
            if (doc.RootElement.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Array) { return outputElement[0].GetString() ?? "/images/fallback.png"; } 
            return "/images/fallback.png"; 
        } 
    }
}
