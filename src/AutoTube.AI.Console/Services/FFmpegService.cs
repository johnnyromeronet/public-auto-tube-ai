using System.Diagnostics;
using System.Globalization;
using AutoTube.AI.Console.Models;
using SixLabors.Fonts;
using Xabe.FFmpeg.Downloader;

namespace AutoTube.AI.Console.Services
{
    public static class FFmpegService
    {
        public static async Task<bool> AddTextToImage(TextImageModel model, string objName)
        {
            try
            {
                model.InputPath = await ResizeAndCrop(model, objName);
                var processedText = WrapTextToFit(model);

                var fontPath = model.FontPath.Replace("\\", "/");
                var lines = processedText.Split(Environment.NewLine);
                var maxSize = model.SpecFontSize != null ? Math.Max(model.FontSize, model.SpecFontSize.Item2) : model.FontSize;
                var yOffset = maxSize * (lines.Length - 1) + 100;

                var frmtBoxAlpha = model.BoxAlpha.ToString(CultureInfo.InvariantCulture);

                List<Tuple<Tuple<int, int>, string>> drawTextFilters = [];
                for (int i = 0; i < lines.Length; i++)
                {
                    var position = 0;
                    var hasBox = false;
                    var boxColor = model.SpecBoxColor != null && model.SpecBoxColor.Item1 == lines[i] ?
                        model.SpecBoxColor.Item2 : model.BoxColor;

                    var fontPosition = model.Landscape ? $":x=20:y=h-th-{yOffset}" : $":x=(w-tw)/2:y=(h-th)/2-{yOffset}";
                    var fontSize = model.SpecFontSize != null && model.SpecFontSize.Item1 == lines[i] ?
                        model.SpecFontSize.Item2 : model.FontSize;

                    // SHADOW
                    var shadowColor = model.SpecShadowColor != null && model.SpecShadowColor.Item1 == lines[i] ?
                        model.SpecShadowColor.Item2 : model.ShadowColor;

                    if (!string.IsNullOrEmpty(shadowColor))
                    {
                        var shadowX = 8;
                        var shadowY = 8;
                        var shadowPosition = model.Landscape ? $":x=20+{shadowX}:y=h-th-{yOffset}+{shadowY}" : $":x=(w-tw)/2+{shadowX}:y=(h-th)/2-{yOffset}+{shadowY}";
                        var shadowLayer = $"drawtext=text='{lines[i]}'{fontPosition}:fontsize={fontSize}:fontcolor={shadowColor}:fontfile='{fontPath}'{shadowPosition}";

                        if (!hasBox && !string.IsNullOrEmpty(boxColor))
                        {
                            shadowLayer += $":box=1:boxcolor={boxColor}@{frmtBoxAlpha}:boxborderw={model.BoxPadding}";
                            hasBox = true;
                        }

                        drawTextFilters.Add(Tuple.Create(Tuple.Create(i, position), shadowLayer));
                        position++;
                    }

                    // BORDER
                    var borderColor = model.SpecBorderColor != null && model.SpecBorderColor.Item1 == lines[i] ?
                       model.SpecBorderColor.Item2 : model.BorderColor;

                    if (!string.IsNullOrEmpty(borderColor))
                    {
                        var borderThickness = 4;
                        int[] offsets = [-borderThickness, 0, borderThickness];

                        foreach (var xOffset in offsets)
                        {
                            foreach (var yOffsetValue in offsets)
                            {
                                if (xOffset == 0 && yOffsetValue == 0) continue;
                                var borderPosition = model.Landscape ? $":x=20+{xOffset}:y=h-th-{yOffset}+{yOffsetValue}" : $":x=(w-tw)/2+{xOffset}:y=(h-th)/2-{yOffset}+{yOffsetValue}";
                                var borderTexts = $"drawtext=text='{lines[i]}'{fontPosition}{borderPosition}:fontsize={fontSize}:fontcolor={borderColor}:fontfile='{fontPath}'";
                                drawTextFilters.Add(Tuple.Create(Tuple.Create(i, position), borderTexts));
                                position++;
                            }
                        }
                    }

                    // TEXT
                    var fontColor = model.SpecFontColor != null && model.SpecFontColor.Item1 == lines[i] ?
                        model.SpecFontColor.Item2 : model.FontColor;
                    var textLayer = $"drawtext=text='{lines[i]}'{fontPosition}:fontsize={fontSize}:fontcolor={fontColor}:fontfile='{fontPath}'";

                    if (!hasBox && !string.IsNullOrEmpty(boxColor))
                    {
                        textLayer += $":box=1:boxcolor={boxColor}@{frmtBoxAlpha}:boxborderw={model.BoxPadding}";
                        hasBox = true;
                    }

                    drawTextFilters.Add(Tuple.Create(Tuple.Create(i, position), textLayer));
                    position++;

                    yOffset -= !string.IsNullOrEmpty(boxColor) ? (maxSize + model.BoxPadding) : maxSize;
                }

                var ffmpegArgs = string.Empty;
                drawTextFilters = [.. drawTextFilters.OrderByDescending(x => x.Item1.Item1).ThenBy(x => x.Item1.Item2)];

                string filterComplex = $"-vf \"{string.Join(",", drawTextFilters.Select(x => x.Item2))}\"";
                ffmpegArgs = $"-i \"{model.InputPath}\" {filterComplex} -y \"{model.OutputPath}\"";

                var result = await RunFFmpeg<bool>(ffmpegArgs, objName);
                if (result)
                {
                    File.Delete(model.InputPath);
                    return await AdjustSize(model.OutputPath, objName, landscape: model.Landscape);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }
            return false;
        }

        private async static Task<bool> AdjustSize(string inputPath, string objName, int destinationSize = 2, int compressionLevel = 30, int scale = -1, bool landscape = true)
        {
            List<string> scales = landscape ? ["1920:1080", "1600:900", "1366:768", "1280:720"] : ["1080:1920", "900:1600", "768:1366", "720:1280"];
            try
            {
                FileInfo outputFile = new(inputPath);
                string tempPath = Path.Combine(Path.GetDirectoryName(inputPath)!, $"temp_{Path.GetFileName(inputPath)}");

                while (outputFile.Length > destinationSize * 1024 * 1024 && compressionLevel < 100)
                {
                    string ffmpegArgs = scale == -1 ? $"-i \"{inputPath}\" -pix_fmt pal8 -compression_level {compressionLevel} -y \"{tempPath}\"" :
                        $"-i \"{inputPath}\" -vf \"scale={scales[scale]}:force_original_aspect_ratio=decrease,pad={scales[scale]}:(ow-iw)/2:(oh-ih)/2\" -pix_fmt pal8 -compression_level {compressionLevel} -y \"{tempPath}\"";

                    bool result = await RunFFmpeg<bool>(ffmpegArgs, objName);
                    if (!result) return false;

                    FileInfo tempFile = new(tempPath);
                    if (tempFile.Length < 2 * 1024 * 1024)
                    {
                        File.Delete(inputPath);
                        File.Move(tempPath, inputPath);
                        return true;
                    }
                    else
                    {
                        compressionLevel += 10;
                        if (compressionLevel > 100) compressionLevel = 100;
                    }
                }

                var isValid = outputFile.Length <= 2 * 1024 * 1024;
                if (!isValid && scale < scales.Count)
                {
                    scale++;
                    return await AdjustSize(inputPath, objName, scale: scale, landscape: landscape);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }
            return false;
        }

        private static string WrapTextToFit(TextImageModel model)
        {
            var fontCollection = new FontCollection();
            var font = fontCollection.Add(model.FontPath);
            var fontInstance = new Font(font, model.FontSize);

            Font? specFontIntance = null;
            string beforeSpecText;
            string afterSpecText = string.Empty;

            if (model.SpecFontSize != null)
            {
                specFontIntance = new Font(font, model.SpecFontSize.Item2);

                beforeSpecText = model.Text[..model.Text.IndexOf(model.SpecFontSize.Item1)].Trim();
                afterSpecText = model.Text[(model.Text.IndexOf(model.SpecFontSize.Item1) + model.SpecFontSize.Item1.Length)..].Trim();
            }
            else
            {
                beforeSpecText = model.Text;
            }

            string result = string.Empty;

            if (!string.IsNullOrEmpty(beforeSpecText))
            {
                result += WrapSimpleText(beforeSpecText, model.Width, fontInstance);
            }

            if (model.SpecFontSize != null && specFontIntance != null)
            {
                if (!string.IsNullOrEmpty(afterSpecText))
                {
                    result += Environment.NewLine + WrapSimpleText(afterSpecText, model.Width, fontInstance);
                }

                if (!string.IsNullOrEmpty(model.SpecFontSize.Item1))
                {
                    result += Environment.NewLine + WrapSimpleText(model.SpecFontSize.Item1, model.Width, specFontIntance);
                }
            }

            return result;
        }

        private static string WrapSimpleText(string text, float maxWidth, Font font)
        {
            string[] words = text.Split(' ');
            string line = string.Empty;
            string result = string.Empty;

            foreach (string word in words)
            {
                string testLine = (line.Length > 0) ? line + " " + word : word;
                float textWidth = TextMeasurer.MeasureSize(testLine, new TextOptions(font)).Width;

                if (textWidth > maxWidth - 40)
                {
                    result += line + Environment.NewLine;
                    line = word;
                }
                else
                {
                    line = testLine;
                }
            }

            result += line;
            return result;
        }

        private async static Task<string> ResizeAndCrop(TextImageModel model, string objName)
        {
            try
            {
                var originalWidth = await GetImageSize(model.InputPath, "width", objName);
                var originalHeight = await GetImageSize(model.InputPath, "height", objName);

                string resizeFilter = string.Empty;
                if (originalWidth != model.Width || originalHeight != model.Height)
                {
                    resizeFilter = $"scale='if(lt(iw,{model.Width}),{model.Width},iw)*if(lt(ih,{model.Height}),{model.Height},ih)/ih':'if(lt(ih,{model.Height}),{model.Height},ih)*if(lt(iw,{model.Width}),{model.Width},iw)/iw'";
                    resizeFilter += $",crop={model.Width}:{model.Height}:(iw-{model.Width})/2:(ih-{model.Height})/2";
                }

                string directory = Path.GetDirectoryName(model.OutputPath)!;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(model.OutputPath);
                string extension = Path.GetExtension(model.OutputPath);
                string newFileName = $"{fileNameWithoutExtension}_resize{extension}";
                string newOutputPath = Path.Combine(directory, newFileName);

                string ffmpegArgs = $"-i \"{model.InputPath}\" {(!string.IsNullOrEmpty(resizeFilter) ? $"-vf \"{resizeFilter}\"" : string.Empty)} -y \"{newOutputPath}\"";
                var result = await RunFFmpeg<bool>(ffmpegArgs, objName);

                if (result)
                {
                    return newOutputPath;
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }
            return string.Empty;
        }

        private async static Task<float> GetImageSize(string imagePath, string dimension, string objName)
        {
            string ffmpegArgs = $"-v error -select_streams v:0 -show_entries stream={dimension} -of csv=p=0 \"{imagePath}\"";
            return await RunFFmpeg<float>(ffmpegArgs, objName, true);
        }

        private async static Task<T?> RunFFmpeg<T>(string arguments, string objName, bool ffprobe = false)
        {
            try
            {
                var ffmpegPath = $"{AppContext.BaseDirectory}ffmpeg.exe";
                if (!File.Exists(ffmpegPath))
                {
                    await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
                }

                if (ffprobe)
                {
                    ffmpegPath = $"{AppContext.BaseDirectory}ffprobe.exe";
                }

                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                string output = string.Empty;
                process.OutputDataReceived += (sender, data) =>
                {
                    if (!string.IsNullOrEmpty(data.Data))
                    {
                        System.Console.WriteLine($"[INFO][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {data.Data}");
                        output += data.Data + "\n";
                    }
                };

                process.ErrorDataReceived += (sender, data) =>
                {
                    if (!string.IsNullOrEmpty(data.Data))
                    {
                        System.Console.WriteLine($"[INFO][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {data.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                if (ffprobe)
                {
                    output = output.Trim();
                    var result = float.TryParse(output.Trim(), out float width) ? width : -1;
                    return (T?)Convert.ChangeType(result, typeof(T?));
                }
                else
                {
                    var result = process.ExitCode == 0;
                    return (T?)Convert.ChangeType(result, typeof(T?));
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }
            return default;
        }
    }
}
