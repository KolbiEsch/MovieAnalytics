using System.Text.Json.Serialization;

namespace MovieAnalyticsWeb.Models
{
    public class TMDBMovieData
    {
        public Genre[] Genres { get; set; }

        [JsonPropertyName("spoken_languages")]
        public SpokenLanguage[] SpokenLanguages { get; set; }

        public string Title { get; set; }

        public int Runtime { get; set; }
    }
}
