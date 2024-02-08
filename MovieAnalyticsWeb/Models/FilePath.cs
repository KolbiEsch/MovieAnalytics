namespace MovieAnalyticsWeb.Models
{
    public class FilePath
    {
        public Guid Id { get; set; }

        public string Path { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }

        public string ApplicationUserId { get; set; }

        public int NumberOfRows { get; set; }

        public int NumOfNewEntrys { get; set; }
    }
}
