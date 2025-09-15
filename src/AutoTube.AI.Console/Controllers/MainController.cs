using AutoTube.AI.Console.Enums;
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

            var model = await CommonService.GetTextContent(new()
            {
                ObjName = _objName,
                Temperature = _temperature,
                SysPrompt = mainPrompt,
                UsrPrompt = usrPrompt,
                Seed = _seed
            });

            if (model != null)
            {
                var baseName = $"{DateTime.UtcNow:yyyyMMdd}_{model.Title.GetSafeFileName()}";
                var json = JsonConvert.SerializeObject(model, Formatting.Indented);
                await File.WriteAllTextAsync($"{CommonService.OutputPath}{prefix}\\content.json", json);

                var audio = await CommonService.GetAudioContent(model.Content, 1, _objName);
                if (audio != null)
                {
                    var audioPath = $"{CommonService.OutputPath}{prefix}\\audio.mp3";
                    await File.WriteAllBytesAsync(audioPath, audio);

                    List<string> imgList = [];
                    var counter = 1;

                    foreach (var item in model.Timelap)
                    {
                        item.ImagePrompt = imgPrompt.Replace($"**[image_prompt]**", item.ImagePrompt);
                        var image = await CommonService.GetImageContent(item.ImagePrompt, _objName);

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
                            success = await FFmpegService.AddTextToImage(new()
                            {
                                Text = $"LA HISTORIA DE {model.Name.ToUpper()}",
                                InputPath = auxPath,
                                OutputPath = $"{CommonService.OutputPath}{prefix}\\{baseName}.png",
                                FontPath = CommonService.FontPath,
                                FontSize = 220,
                                SpecFontSize = Tuple.Create(model.Name.ToUpper(), 300F),
                                FontColor = "#FFFFFF",
                                SpecFontColor = Tuple.Create(model.Name.ToUpper(), "#000000"),
                                ShadowColor = "#000000",
                                SpecShadowColor = Tuple.Create(model.Name.ToUpper(), string.Empty),
                                BorderColor = "#000000",
                                SpecBorderColor = Tuple.Create(model.Name.ToUpper(), string.Empty),
                                BoxColor = "#000000",
                                SpecBoxColor = Tuple.Create(model.Name.ToUpper(), "#FAA700")
                            }, _objName);

                            if (!success)
                            {
                                break;
                            }
                        }

                        success = true;
                        counter++;
                    }

                    if (success)
                    {
                        var duration = await FFmpegService.GetMediaDuration(audioPath, _objName);
                        var secondsPerSlide = duration / imgList.Count;

                        success = await FFmpegService.CreateSlideshow(new()
                        {
                            Images = imgList,
                            SecondsPerSlide = secondsPerSlide,
                            SlideMode = SlideModeEnum.ZoomIn,
                            OutputPath = $"{CommonService.OutputPath}{prefix}\\{baseName}.mp4",
                            Audios = [
                                new()
                                {
                                    Path = audioPath,
                                    Volume = 1.8
                                },
                                new()
                                {
                                    Path = CommonService.MusicPath,
                                    Volume = 0.15,
                                    FadeOutDuration = 1.5
                                }
                            ]
                        }, _objName);

                        if (success)
                        {
                            await File.AppendAllTextAsync($"{CommonService.PromptsPath}{_historicalPrompt}", $"{Environment.NewLine}- {model.Title}");
                        }
                    }
                }
            }
        }
    }
}
