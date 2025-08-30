namespace AutoTube.AI.Console.Models
{
    public class TextImageModel
    {
        public string Text { get; set; } = string.Empty;

        public string InputPath { get; set; } = string.Empty;

        public string OutputPath { get; set; } = string.Empty;

        public string FontPath { get; set; } = string.Empty;

        public int BoxPadding { get; set; } = 30;

        public float BoxAlpha { get; set; } = 1F;

        public int Width { get; set; } = 2560;

        public int Height { get; set; } = 1440;

        public bool Landscape { get; set; } = true;

        // TEXT

        public float FontSize { get; set; }

        public string FontColor { get; set; } = string.Empty;

        public string ShadowColor { get; set; } = string.Empty;

        public string BorderColor { get; set; } = string.Empty;

        public string BoxColor { get; set; } = string.Empty;

        // SPEC

        public Tuple<string, float>? SpecFontSize { get; set; }

        public Tuple<string, string>? SpecFontColor { get; set; }

        public Tuple<string, string>? SpecShadowColor { get; set; }

        public Tuple<string, string>? SpecBorderColor { get; set; }

        public Tuple<string, string>? SpecBoxColor { get; set; }
    }
}
