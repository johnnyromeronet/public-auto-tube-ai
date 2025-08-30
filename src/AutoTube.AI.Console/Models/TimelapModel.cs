using Newtonsoft.Json;

namespace AutoTube.AI.Console.Models
{
    public class TimelapModel
    {
        [JsonProperty("beg")]
        public string Beg { get; set; } = string.Empty;

        [JsonProperty("end")]
        public string End { get; set; } = string.Empty;

        [JsonProperty("image_prompt")]
        public string ImagePrompt { get; set; } = string.Empty;
    }
}
