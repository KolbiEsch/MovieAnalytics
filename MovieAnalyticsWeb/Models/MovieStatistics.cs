using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MovieAnalyticsWeb.Data;

namespace MovieAnalyticsWeb.Models
{
    public class MovieStatistics
    {

        private static readonly Dictionary<string, IOrderBy> OrderFunctions = new()
            {
                { "valueStringInt", new OrderBy<KeyValuePair<string, int>, int>(x => x.Value) },
                { "keyIntInt", new OrderBy<KeyValuePair<int, int>, int>(x => x.Key) },
                { "keyFirstNumStringInt", 
                new OrderBy<KeyValuePair<string, int>, int>(x => ExtractFirstNumber(x.Key)) },
                { "keyWeekdayStringInt", 
                new OrderBy<KeyValuePair<string, int>, int>(x => WeekdayToNum(x.Key)) }
            };

        public Dictionary<string, int> GenreCount { get; } = new Dictionary<string, int>();

        private IOrderedQueryable<KeyValuePair<string, int>> GenreCountSorted
        {
            get => GenreCount.AsQueryable().OrderBy(OrderFunctions["valueStringInt"], true);
        }

        public List<string> GenreNameList => GenreCountSorted.Select(x => x.Key).Take(10).ToList();

        public List<int> GenreNumList => GenreCountSorted.Select(x => x.Value).Take(10).ToList();

        public Dictionary<string, int> LanguageCount { get; } = new Dictionary<string, int>();

        private IEnumerable<KeyValuePair<string, int>> LanguageCountSorted
        {
            get => LanguageCount.AsQueryable().OrderBy(OrderFunctions["valueStringInt"], true);
        }

        public List<string> LanguageNameList
        {
            get => LanguageCountSorted.Select(x => x.Key).Take(10).ToList();
        }

        public List<int> LanguageCountList
        {
            get => LanguageCountSorted.Select(x => x.Value).Take(10).ToList();
        }

        public Dictionary<int, int> YearFilmsSeen { get; } = new Dictionary<int, int>();

        private IOrderedQueryable<KeyValuePair<int, int>> YearFilmsSeenSorted
        {
            get => YearFilmsSeen.AsQueryable().OrderBy(OrderFunctions["keyIntInt"], false);
        }

        public List<int> YearList => YearFilmsSeenSorted.Select(x => x.Key).ToList();

        public int MinMovieYear => YearList[0];

        public int MaxMovieYear => YearList[YearList.Count - 1];

        public List<int> FilmsSeenInYearList => YearFilmsSeenSorted.Select(x => x.Value).ToList();

        public Dictionary<string, int> WeekFilmsSeen { get; } = new Dictionary<string, int>();

        private IOrderedQueryable<KeyValuePair<string, int>> WeekFilmsSeenSorted
        {
            get => WeekFilmsSeen.AsQueryable().OrderBy(OrderFunctions["keyFirstNumStringInt"], false);
        }

        public List<string> WeekList => WeekFilmsSeenSorted.Select(x => x.Key).ToList();

        public List<int> FilmsSeenInWeekList => WeekFilmsSeenSorted.Select(x => x.Value).ToList();

        public Dictionary<string, int> WeekdayFilmsSeen { get; } = new Dictionary<string, int>();

        private IEnumerable<KeyValuePair<string, int>> WeekdayFilmsSeenSorted
        {
            get => WeekdayFilmsSeen.AsQueryable().OrderBy(OrderFunctions["keyWeekdayStringInt"], false);
        }

        public List<string> WeekdayList => WeekdayFilmsSeenSorted.Select(x => x.Key).ToList();

        public List<int> FilmsSeenInWeekdayList => WeekdayFilmsSeenSorted.Select(x => x.Value).ToList();

        public static int WeekdayToNum(string weekday)
        {
            if (weekday == "Monday") return 0;
            if (weekday == "Tuesday") return 1;
            if (weekday == "Wednesday") return 2;
            if (weekday == "Thursday") return 3;
            if (weekday == "Friday") return 4;
            if (weekday == "Saturday") return 5;
            if (weekday == "Sunday") return 6;
            
            return -1;
        }

        public List<int> RuntimeList { get; set; } = new List<int>();

        public int YearOfStats { get; set; } = -1;

        public int NumberOfMovies { get; set; }

        public int RewatchCount { get; set; }

        public int MoviesWatchedStatYear
        {
            get
            {
                return YearFilmsSeen.ContainsKey(YearOfStats) ?
                    YearFilmsSeen[YearOfStats] : 0;
            } 
        }

        public int HoursWatched
        {
            get { return RuntimeList.Sum() / 60; }
        }

        public int LanguagesHeard
        {
            get { return LanguageCount.Count; }
        }

        public double AverageMoviesPerMonth
        {
            get
            {
                if (YearOfStats == -1)
                {
                    return 0;
                }
                DateTime currentDate = DateTime.Now;
                if (currentDate.Year == YearOfStats)
                {
                    int daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                    double percentageOfMonthCompleted = ((double) currentDate.Day / daysInMonth);
                    return Math.Round(NumberOfMovies / (currentDate.Month - 1 + percentageOfMonthCompleted), 1);
                }
                return Math.Round(NumberOfMovies / 12.0, 1);
            }
        }

        public double AverageMoviesPerWeek
        {
            get
            {
                if (YearOfStats == -1)
                {
                    return 0;
                }
                DateTime currentDate = DateTime.Now;
                double weeks;
                if (currentDate.Year == YearOfStats)
                {
                    weeks = currentDate.DayOfYear / 7.0;
                    return Math.Round(NumberOfMovies / weeks, 1);
                }
                int daysOfYear = DateTime.IsLeapYear((int)YearOfStats) ? 366 : 365;
                weeks = daysOfYear / 7.0;
                return Math.Round(NumberOfMovies / weeks, 1);
            }
        }

        private static int ExtractFirstNumber(string withNumber)
        {
            var numberString = withNumber.SkipWhile(c => !char.IsDigit(c))
                                   .TakeWhile(c => char.IsDigit(c)).ToArray();

            return int.Parse(numberString);
        }
    }
}