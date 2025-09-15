namespace AutoTube.AI.Console.Models
{
    public class FFmpegAudioModel
    {
        public string Path { get; set; } = string.Empty;

        public double Volume { get; set; } = 1;

        public double FadeOutDuration { get; set; } = 0;
    }
}
