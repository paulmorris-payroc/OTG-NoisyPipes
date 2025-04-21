using System.Text.Json.Serialization;

namespace NoisyPipes.Models
{
    public class AzureDevOpsPipeline
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("folder")]
        public string Folder { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }
}
