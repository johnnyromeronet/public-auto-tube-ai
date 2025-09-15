using System.Diagnostics;
using System.Globalization;
using AutoTube.AI.Console.Enums;
using AutoTube.AI.Console.Models;
using SixLabors.Fonts;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace AutoTube.AI.Console.Services
{
    public static class FFmpegService
    {
        public static async Task<bool> CreateSlideshow(FFmpegSlideshowModel model, string objName)
        {
            try
            {
                var secondsAux = model.SecondsPerSlide * model.ConversionFactor;
                var totalFrames = secondsAux * model.FrameRate;
                var frmtSeconds = secondsAux.ToString(CultureInfo.InvariantCulture);
                var frmtFrameRate = model.FrameRate.ToString(CultureInfo.InvariantCulture);
                var frmtTotalFrames = totalFrames.ToString(CultureInfo.InvariantCulture);
                var images = model.Images.ToList();
                var audios = model.Audios.ToList();

                string inputArgs = string.Empty;
                for (int i = 0; i < images.Count; i++)
                {
                    inputArgs += new List<SlideModeEnum>() { SlideModeEnum.ZoomOut, SlideModeEnum.ZoomIn }.Contains(model.SlideMode) ?
                        $"-framerate {frmtFrameRate} -t {frmtSeconds} -i \"{images[i]}\" " : $"-loop 1 -t {frmtSeconds} -i \"{images[i]}\" ";
                }

                for (int i = 0; i < audios.Count; i++)
                {
                    inputArgs += $"-i \"{audios[i].Path}\" ";
                }

                var outScale = $"{model.Width}:{model.Height}";
                var slideScale = outScale;
                var outWidth = model.Width;
                var outHeight = model.Height;
                var auxScale = string.Empty;
                var filter = string.Empty;

                for (int i = 0; i < images.Count; i++)
                {
                    var originalWidth = await GetImageSize(images[i], "width", objName);
                    var originalHeight = await GetImageSize(images[i], "height", objName);
                    var aspectRatio = GetAspectRatio((int)originalWidth, (int)originalHeight);

                    if (aspectRatio != "1:1" && (
                       (originalWidth > originalHeight && model.Width > model.Height) ||
                       (originalWidth < originalHeight && model.Width < model.Height)
                    ))
                    {
                        slideScale = $"{model.Width * 1.5}:{model.Height * 1.5}";
                        auxScale = outScale;
                    }
                    else if (model.Width > model.Height)
                    {
                        var temp = model.Height / originalHeight;
                        outWidth = (int)Math.Ceiling(originalWidth * temp);
                        auxScale = $"{outWidth}:{model.Height}";
                    }
                    else
                    {
                        var temp = model.Width / originalWidth;
                        outHeight = (int)Math.Ceiling(originalHeight * temp);
                        auxScale = $"{model.Width}:{outHeight}";
                    }

                    filter += $"[{i}:v]split=2[v_bg{i}][v_scaled{i}]; ";
                    filter += $"[v_bg{i}]scale={outScale}:force_original_aspect_ratio=increase,crop={outScale},gblur=sigma=50,setsar=1[bg{i}]; ";
                    filter += $"[v_scaled{i}]scale={auxScale},setsar=1[scaled{i}]; ";

                    switch (model.SlideMode)
                    {
                        case SlideModeEnum.ZoomOut:
                            filter += $"[scaled{i}]nullsink; ";
                            filter += $"[{i}:v]scale={slideScale}:force_original_aspect_ratio=increase,setsar=1[prezoom{i}]; ";
                            filter += $"[prezoom{i}]zoompan=z='(ih/{outHeight})-((ih/{outHeight}-1)*(on/{frmtTotalFrames}))':x='{outWidth / 2}':y='{outHeight / 2}':d={frmtTotalFrames}:s={outWidth}x{outHeight},fps={frmtFrameRate},setsar=1[zoom{i}]; ";
                            filter += $"[bg{i}][zoom{i}]overlay=(W-w)/2:(H-h)/2[out{i}]; ";
                            break;
                        case SlideModeEnum.ZoomIn:
                            filter += $"[scaled{i}]nullsink; ";
                            filter += $"[{i}:v]scale={slideScale}:force_original_aspect_ratio=increase,setsar=1[prezoom{i}]; ";
                            filter += $"[prezoom{i}]zoompan=z='1+((ih/{outHeight}-1)*(on/{frmtTotalFrames}))':x='{outWidth / 2}':y='{outHeight / 2}':d={frmtTotalFrames}:s={outWidth}x{outHeight},fps={frmtFrameRate},setsar=1[zoom{i}]; ";
                            filter += $"[bg{i}][zoom{i}]overlay=(W-w)/2:(H-h)/2[out{i}]; ";
                            break;
                        default:
                            filter += $"[bg{i}][scaled{i}]overlay=(W-w)/2:(H-h)/2[out{i}]; ";
                            break;
                    }
                }

                string concatFilter = string.Join("", Enumerable.Range(0, images.Count).Select(i => $"[out{i}]"));
                filter += $"{concatFilter}concat=n={images.Count}:v=1[outv]; ";

                string args = string.Empty;
                if (audios.Count > 0)
                {
                    double videoDuration = images.Count * model.SecondsPerSlide;
                    var frmtDuration = videoDuration.ToString(CultureInfo.InvariantCulture);

                    string audioFilter = string.Empty;
                    string finalAudioInputs = string.Empty;

                    for (int i = 0; i < audios.Count; i++)
                    {
                        var frmtVolume = audios[i].Volume.ToString(CultureInfo.InvariantCulture);
                        audioFilter += $"[{images.Count + i}:a]atrim=0:{frmtDuration}[trimmed{i}]; ";

                        if (audios[i].FadeOutDuration > 0)
                        {
                            var frmtFadeOutStart = (videoDuration - audios[i].FadeOutDuration).ToString(CultureInfo.InvariantCulture);
                            var frmtFadeOutDuration = audios[i].FadeOutDuration.ToString(CultureInfo.InvariantCulture);
                            audioFilter += $"[trimmed{i}]volume={frmtVolume},afade=t=out:st={frmtFadeOutStart}:d={frmtFadeOutDuration}[audio{i}]; ";
                        }
                        else
                        {
                            audioFilter += $"[trimmed{i}]volume={frmtVolume}[audio{i}]; ";
                        }

                        finalAudioInputs += $"[audio{i}]";
                    }

                    filter += $"{audioFilter}{finalAudioInputs}amix=inputs={audios.Count}:normalize=1[finalAudio]; ";

                    args = inputArgs +
                       "-filter_complex \"" + filter + "\" " +
                       "-map \"[outv]\" -map \"[finalAudio]\" " +
                       $"-r {frmtFrameRate} -c:v libx264 -preset slow -crf 22 -b:v 4000k -pix_fmt yuv420p " +
                       "-c:a aac -b:a 192k " +
                       "-y \"" + model.OutputPath + "\"";
                }
                else
                {
                    args = inputArgs +
                        "-filter_complex \"" + filter + "\" " +
                        "-map \"[outv]\" " +
                        $"-r {frmtFrameRate} -c:v libx264 -preset slow -crf 22 -b:v 4000k -pix_fmt yuv420p " +
                        "-y \"" + model.OutputPath + "\"";
                }

                var result = await RunFFmpeg<bool>(args, objName);

                var duration = await GetMediaDuration(model.OutputPath, objName);
                var durationPerSlide = duration / images.Count;
                if (Math.Round(durationPerSlide) != Math.Round(model.SecondsPerSlide))
                {
                    model.ConversionFactor = model.SecondsPerSlide / durationPerSlide;
                    result = await CreateSlideshow(model, objName);
                }

                return result;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
            }

            return false;
        }

        public async static Task<double> GetMediaDuration(string fileName, string objName)
        {
            try
            {
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official);
                var mediaInfo = await FFmpeg.GetMediaInfo(fileName);
                double duration = mediaInfo.Duration.TotalSeconds;
                return duration;
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} - {ex.InnerException.Message}" : ex.Message;
                System.Console.WriteLine($"[ERROR][{DateTime.UtcNow:yyyyMMddHHmmss}][{objName}] {msg}");
                return 0;
            }
        }

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

        private static string GetAspectRatio(int width, int height)
        {
            int a = width;
            int b = height;

            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }

            int gcd = a; // este es el MCD correcto

            int w = width / gcd;
            int h = height / gcd;

            return $"{w}:{h}";
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
