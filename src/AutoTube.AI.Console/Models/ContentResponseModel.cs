using Newtonsoft.Json;

namespace AutoTube.AI.Console.Models
{
    public class ContentResponseModel
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("timelap")]
        public IEnumerable<TimelapModel> Timelap { get; set; } = [];
    }
}
