using System.Text.RegularExpressions;

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
