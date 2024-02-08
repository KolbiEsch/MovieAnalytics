namespace MovieAnalyticsWeb.Models
{
    public class AggregateMovieData
    {
        public string Title { get; set; }

        public string Genres { get; set; }

        public string SpokenLanguages { get; set; }

        public int Runtime { get; set; }

        public DateTime WatchedDate { get; set; }

        public string Rewatch { get; set; }

        public int ReleaseYear { get; set; }

    }
}
