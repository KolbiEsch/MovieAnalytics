using CsvHelper.Configuration.Attributes;

namespace MovieAnalyticsWeb.Models
{
    public class DiaryMovieData
    {
        [Name("Name")]
        public string Title { get; set; }

        [Name("Watched Date")]
        public DateTime WatchedDate { get; set; }

        [Name("Year")]
        public int ReleaseYear { get; set; }

        public string Rewatch { get; set; }

    }
}
