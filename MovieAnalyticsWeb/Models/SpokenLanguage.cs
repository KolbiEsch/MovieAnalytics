using System.Text.Json.Serialization;

namespace MovieAnalyticsWeb.Models
{
    public class SpokenLanguage
    {
        [JsonPropertyName("english_name")]
        public string EnglishName { get; set; }

        public string Name { get; set; }

    }
}
