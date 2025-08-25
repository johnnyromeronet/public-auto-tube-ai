using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AutoTube.AI.Console.Services
{
    public static class OpenAIService
    {
        private static readonly string _objName = "OPENAI";
        private static readonly string _apiKey = "sssssqqff";
        private static readonly string _apiUrl = "https://api.openai.com/v1/";
        private static readonly string _chatModel = "gpt-4o";
        private static readonly string _imgModel = "dall-e-3";
        private static readonly string _ttsModel = "tts-1";
        private static readonly int _timeout = 5;

        public static readonly string[] Voices = [
            "nova",
            "ash",
            "onyx",
            "fable",
            "echo",
            "ballad",
            "coral",
            "sage",
            "shimmer",
            "alloy"
        ];

        public static async Task<string?> GetResponse(string usrPrompt, string sysPrompt, double temperature, int? seed = null)
        {
            System.Console.WriteLine($"[INFO][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] Generando contenido...");

            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromMinutes(_timeout);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var requestBody = new
                {
                    model = _chatModel,
                    messages = new[]
                    {
                        new { role = "system", content = sysPrompt },
                        new { role = "user", content = usrPrompt }
                    },
                    temperature,
                    seed
                };

                string jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{_apiUrl}chat/completions", content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);

                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] {msg}");
                return string.Empty;
            }
        }

        public async static Task<byte[]?> GenerateImage(string prompt)
        {
            System.Console.WriteLine($"[INFO][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] Generando imagen...");

            try
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromMinutes(_timeout);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var requestBody = new
                {
                    model = _imgModel,
                    prompt,
                    n = 1,
                    size = "1024x1024"
                };

                string jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{_apiUrl}images/generations", content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(responseBody);

                var imageUrl = doc.RootElement.GetProperty("data")[0].GetProperty("url").GetString();

                using HttpClientHandler handler = new() { AllowAutoRedirect = true };
                using HttpClient downloadClient = new(handler);

                return await downloadClient.GetByteArrayAsync(imageUrl);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] {msg}");
                return null;
            }
        }

        public async static Task<byte[]?> TextToSpeech(string inputText, string voice)
        {
            System.Console.WriteLine($"[INFO][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] Generando audio...");

            try 
            {
                using HttpClient client = new();
                client.Timeout = TimeSpan.FromMinutes(_timeout);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var requestBody = new
                {
                    model = _ttsModel,
                    input = inputText,
                    voice
                };

                string jsonRequest = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{_apiUrl}audio/speech", content);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] {msg}");
                return null;
            }
        }
    }
}
