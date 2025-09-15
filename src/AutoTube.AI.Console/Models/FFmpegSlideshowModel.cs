using AutoTube.AI.Console.Enums;

namespace AutoTube.AI.Console.Models
{
    public class FFmpegSlideshowModel
    {
        public IEnumerable<string> Images { get; set; } = [];

        public double SecondsPerSlide { get; set; }

        public int FrameRate { get; set; } = 30;

        public double ConversionFactor { get; set; } = 1;

        public IEnumerable<FFmpegAudioModel> Audios { get; set; } = [];

        public string OutputPath { get; set; } = string.Empty;

        public SlideModeEnum SlideMode { get; set; } = SlideModeEnum.None;

        public int Width { get; set; } = 1920;

        public int Height { get; set; } = 1080;
    }
}
