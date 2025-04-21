using System.Text.Json.Serialization;

namespace NoisyPipes.Models
{
    public class AzureDevOpsProject
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
