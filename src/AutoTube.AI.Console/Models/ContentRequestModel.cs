namespace AutoTube.AI.Console.Models
{
    public class ContentRequestModel
    {
        public string SysPrompt { get; set; } = string.Empty;

        public string UsrPrompt { get; set; } = string.Empty;

        public float Temperature { get; set; }

        public int? Seed { get; set; }

        public string ObjName { get; set; } = string.Empty;
    }
}
