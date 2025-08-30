using AutoTube.AI.Console.Models;
using AutoTube.AI.Console.Services;
using Newtonsoft.Json;

namespace AutoTube.AI.Console.Controllers
{
    public static class MainController
    {
        private static readonly string _objName = "MAIN";
        private static readonly string _mainPrompt = "main.md";
        private static readonly string _historicalPrompt = "historical.md";
        private static readonly string _imgPrompt = "image.md";
        private static readonly string _usrPromptSimple = "Genera una narración sobre un personaje siguiendo las indicaciones del prompt.";
        private static readonly string _usrPromptSurname = "Genera una narración sobre {0}, siguiendo las indicaciones del prompt.";
        private static readonly float _temperature = 0.6F;
        private static readonly int _seed = 1;

        public async static Task StartProcess(string? content = null)
        {
            var usrPrompt = string.IsNullOrEmpty(content) ? _usrPromptSimple : _usrPromptSurname.Replace("{0}", content);
            System.Console.WriteLine($"[INFO][{DateTime.UtcNow:yyyyMMddHHmmss}][{_objName}] {usrPrompt}");

            var success = false;
            var prefix = $"{_objName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
            Directory.CreateDirectory($"{CommonService.OutputPath}{prefix}");
            Directory.CreateDirectory($"{CommonService.TempPath}{prefix}");

            var mainPrompt = await File.ReadAllTextAsync($"{CommonService.PromptsPath}{_mainPrompt}");
            var imgPrompt = await File.ReadAllTextAsync($"{CommonService.PromptsPath}{_imgPrompt}");

            if (string.IsNullOrEmpty(content))
            {
                var historicalPrompt = await File.ReadAllTextAsync($"{CommonService.PromptsPath}{_historicalPrompt}");
                var historicalAux = historicalPrompt.Replace("- ", string.Empty).Replace("\r\n", ", ");
                mainPrompt = mainPrompt.Replace($"**[{_historicalPrompt}]**", historicalAux);
            }

            // TO-DO: Obtener guión
            var model = new ContentResponseModel();

            if (model != null)
            {
                var baseName = $"{DateTime.UtcNow:yyyyMMdd}_{model.Title.GetSafeFileName()}";
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                await File.WriteAllTextAsync($"{CommonService.OutputPath}{prefix}\\content.json", json);

                // TO-DO: Obtener audio
                var audio = new byte[0];
                if (audio != null)
                {
                    var audioPath = $"{CommonService.OutputPath}{prefix}\\audio.mp3";
                    await File.WriteAllBytesAsync(audioPath, audio);

                    List<string> imgList = [];
                    var counter = 1;

                    // TO-DO: Obtener imagenes
                    foreach (var item in model.Timelap)
                    {
                        item.ImagePrompt = imgPrompt.Replace($"**[image_prompt]**", item.ImagePrompt);
                        var image = new byte[0];

                        if (image == null)
                        {
                            success = false;
                            break;
                        }

                        var auxPath = $"{CommonService.OutputPath}{prefix}\\image_{counter}.png";
                        await File.WriteAllBytesAsync(auxPath, image);
                        imgList.Add(auxPath);

                        if (counter == 1)
                        {
                            // TO-DO: Generar miniatura
                        }

                        success = true;
                        counter++;
                    }

                    if (success)
                    {
                        // TO-DO: Generar vídeo
                    }
                }
            }
        }
    }
}
