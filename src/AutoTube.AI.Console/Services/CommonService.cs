using System.Drawing;
using System.Text.RegularExpressions;
using AutoTube.AI.Console.Models;
using Newtonsoft.Json;

namespace AutoTube.AI.Console.Services
{
    public static class CommonService
    {
        public static readonly string BasePath = "C:\\Dev\\Public.AutoTube.AI\\";
        public static readonly string OutputPath = $"{BasePath}output\\";
        public static readonly string TempPath = $"{BasePath}temp\\";
        public static readonly string PromptsPath = $"{BasePath}src\\AutoTube.AI.Console\\prompts\\";
        public static readonly string FontPath = $"{BasePath}fonts\\governor.ttf";
        public static readonly string MusicPath = $"{BasePath}music\\Chill Vibes Only.mp3";

        public async static Task<ContentResponseModel?> GetTextContent(ContentRequestModel input)
        {
            ContentResponseModel? model = null;

            try
            {
                var response = await OpenAIService.GetResponse(input.UsrPrompt, input.SysPrompt, input.Temperature, input.Seed);

                if (!string.IsNullOrWhiteSpace(response))
                {
                    var responseAux = response.Replace("```json", string.Empty).Replace("```", string.Empty);
                    model = JsonConvert.DeserializeObject<ContentResponseModel>(responseAux);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{input.ObjName}] {msg}");
            }

            return model;
        }

        public async static Task<byte[]?> GetAudioContent(string content, int voice, string objName)
        {
            byte[]? audio = null;

            try
            {
                audio = await OpenAIService.TextToSpeech(content, OpenAIService.Voices[voice]);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }

            return audio;
        }

        public async static Task<byte[]?> GetImageContent(string prompt, string objName)
        {
            byte[]? image = null;

            try
            {
                image = await OpenAIService.GenerateImage(prompt);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }

            return image;
        }

        public static string GetSafeFileName(this string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeName = Regex.Replace(fileName, "[" + Regex.Escape(new string(invalidChars)) + "]", "_");
            safeName = safeName.Replace(" ", "-");
            safeName = safeName.Replace("'", "");
            safeName = safeName.Replace(",", "");
            return safeName;
        }
    }
}
