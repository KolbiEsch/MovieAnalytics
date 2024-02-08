using CsvHelper;
using CsvHelper.Configuration;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using MovieAnalyticsWeb.Models;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MovieAnalyticsWeb.Data
{
    public sealed class Service : IService
    {
        private readonly ApplicationDbContext _context;

        private readonly ITMDBApiClient _apiClient;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IHttpContextAccessor _contextAccessor;

        private readonly IWebHostEnvironment _environment;

        private readonly string _beginningFilePath;

        public Service(ApplicationDbContext context,
            ITMDBApiClient apiClient,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IWebHostEnvironment environment)
        {
            _context = context;
            _apiClient = apiClient;
            _userManager = userManager;
            _contextAccessor = contextAccessor;
            _environment = environment;

            if (_environment.IsDevelopment())
            {
                _beginningFilePath = "";
            } else
            {
                _beginningFilePath = "C:/home/site/wwwroot/";
            }
        }

        public async Task<List<int>> GetViewData()
        {
            var diaryFile = await GetDiaryFileOfUser();

            if (diaryFile == null) return new List<int>();
            
            using var streamReader = new StreamReader(_beginningFilePath + diaryFile.Path);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var diaryMovies = csvReader.GetRecords<DiaryMovieData>();
            return diaryMovies.Select(x => x.WatchedDate.Year).Distinct().ToList();
        }

        
        public async Task<MovieStatistics> GetStatistics(string year)
        {
            var statistics = new MovieStatistics();

            //Check if new stats need to be added to file.
            await PopulateStatisticsFile();

            var aggregateFile = await GetStatsFileOfUser();

            if (aggregateFile == null)
            {
                return statistics;
            }

            using var streamReader = new StreamReader(_beginningFilePath + aggregateFile.Path);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var aggregateMovieDataList = csvReader.GetRecords<AggregateMovieData>().ToList();

            // Try to change from bool to make this more readable
            bool isYearStats = int.TryParse(year, out int yearNumber);
            if (isYearStats)
            {
                aggregateMovieDataList = aggregateMovieDataList.Where(x => x.WatchedDate.Year == yearNumber).ToList();
                statistics.YearOfStats = yearNumber;
                InitializeWeekFilmsSeenStats(statistics.WeekFilmsSeen, yearNumber);
                InitializeWeekdayFilmsSeenStats(statistics.WeekdayFilmsSeen);
            }

            //Get min and max year to initialize YearFilmsSeen. Try and separate.
            //Make into separate classes

            var yearList = aggregateMovieDataList.Select(x => x.ReleaseYear);
            var minYear = yearList.Min();
            var maxYear = yearList.Max();
            InitializeYearFilmsSeenStats(minYear, maxYear, statistics.YearFilmsSeen);

            var uniqueMovieList = aggregateMovieDataList.DistinctBy(x => x.Title);

            var movieCount = 0;
            foreach(var movie in aggregateMovieDataList)
            {
                PopulateGenreStatistics(movie.Genres.Split(","), statistics.GenreCount);
                PopulateLanguageStatistics(movie.SpokenLanguages.Split(","), statistics.LanguageCount);
                statistics.RuntimeList.Add(movie.Runtime);
                if (isYearStats)
                {
                    PopulateWeekFilmsSeenStats(yearNumber, movie.WatchedDate.DayOfYear, statistics.WeekFilmsSeen);
                    PopulateWeekdayFilmsSeenStats(movie.WatchedDate.DayOfWeek.ToString(), statistics.WeekdayFilmsSeen);
                }
                if (movie.Rewatch == "Yes")
                {
                    statistics.RewatchCount++;
                }
                
                movieCount++;
            }
            statistics.NumberOfMovies = movieCount;

            foreach(var movie in uniqueMovieList)
            {
                PopulateYearFilmsSeenStats(movie.ReleaseYear, statistics.YearFilmsSeen);
            }

            return statistics;
        }

        private async Task PopulateStatisticsFile()
        {
            List<DiaryMovieData>? newDiaryMovies = await GetNewlyWatchedMovies();
            if (newDiaryMovies == null) { return; }
            List<int> TMDBIds = await GetTMDBMovieIds(newDiaryMovies);
            List<TMDBMovieData?> TMDBMovies = await GetTMDBMovies(TMDBIds);

            List<AggregateMovieData> newAggregateMovies = GenerateAggregateNewlyWatchedMoviesList(TMDBMovies, newDiaryMovies);
            
            await WriteAggregateMovieDataToFile(newAggregateMovies);

            //If everything was written correctly set diary file to no new entrys.
            var diaryFile = await GetDiaryFileOfUser();

            diaryFile.NumOfNewEntrys = 0;
            await _context.SaveChangesAsync();
        }

        private static List<AggregateMovieData> GenerateAggregateNewlyWatchedMoviesList(List<TMDBMovieData?> newTMDBData,
            List<DiaryMovieData> newDiaryData) {

            List <AggregateMovieData> newAggregateMovies = new();
            for (int i = 0; i < newTMDBData.Count; i++)
            {
                var TMDBMovie = newTMDBData[i];
                if (TMDBMovie == null)
                {
                    continue;
                }

                AggregateMovieData movie = new();
                movie.Title = TMDBMovie.Title;
                movie.Runtime = TMDBMovie.Runtime;
                movie.Genres = string.Join(",", TMDBMovie.Genres.Select(x => x.Name));
                movie.SpokenLanguages = string.Join(",", TMDBMovie.SpokenLanguages.Select(x => x.EnglishName));
                movie.Rewatch = newDiaryData[i].Rewatch;
                movie.WatchedDate = newDiaryData[i].WatchedDate;
                movie.ReleaseYear = newDiaryData[i].ReleaseYear;
                newAggregateMovies.Add(movie);

            }

            return newAggregateMovies;
        }

        private async Task WriteAggregateMovieDataToFile(List<AggregateMovieData> aggregateMovieDataList)
        {
            if (aggregateMovieDataList == null) { return; }

            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);

            var currentAggregateFile = await GetStatsFileOfUser();

            if (currentAggregateFile != null)
            {
                await AppendAggregateMovieDataToFile(currentAggregateFile, aggregateMovieDataList);
                return;
            }

            string fileName = String.Concat("statistics", Guid.NewGuid().ToString("N"), ".csv");
            var newCsvPath = Path.Combine("wwwroot/files/", fileName);

            FilePath aggregateFilePath = new FilePath
            {
                Path = newCsvPath,
                ApplicationUserId = user.Id
            };

            using var streamWriter = new StreamWriter(newCsvPath);
            using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecords(aggregateMovieDataList);
            }
            
            await _context.FilePaths.AddAsync(aggregateFilePath);
            await _context.SaveChangesAsync();
        }

        private async Task AppendAggregateMovieDataToFile(FilePath currentAggregateFile, List<AggregateMovieData> newAggregateMovieDataList)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var streamWriter = new StreamWriter(_beginningFilePath + currentAggregateFile.Path, true);
            using (var csvWriter = new CsvWriter(streamWriter, config))
            {
                csvWriter.WriteRecords(newAggregateMovieDataList);
            }

            await _context.SaveChangesAsync();
        }

        public async Task WriteDiaryMovieDataToFile(IFormFile diaryFile)
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);

            var currentDiaryFile = await GetDiaryFileOfUser();
            
            StreamReader reader = new(diaryFile.OpenReadStream());
            using CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
            var diaryMovies = csvReader.GetRecords<DiaryMovieData>().ToList();

            // Append to existing file.
            if (currentDiaryFile != null)
            {
                await AppendDiaryMovieDataToFile(currentDiaryFile, diaryMovies);
                return;
            }

            string fileName = String.Concat("diary", Guid.NewGuid().ToString("N"), ".csv");
            var newDiaryPath = Path.Combine("wwwroot/files/", fileName);

            FilePath diaryFilePath = new FilePath
            {
                Path = newDiaryPath,
                ApplicationUserId = user.Id,
            };

            int count = 0;
            using var writer = new StreamWriter(newDiaryPath);
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteHeader<DiaryMovieData>();
                csv.NextRecord();
                foreach(var movie in diaryMovies)
                {
                    csv.WriteRecord(movie);
                    csv.NextRecord();
                    count++;
                }
            }

            diaryFilePath.NumberOfRows = count;
            diaryFilePath.NumOfNewEntrys = count;

            _context.FilePaths.Add(diaryFilePath);
            await _context.SaveChangesAsync();
        }

        private async Task AppendDiaryMovieDataToFile(FilePath currentDiaryFile, List<DiaryMovieData> diaryMovies)
        {
            var newDiaryMovies = diaryMovies.Skip(currentDiaryFile.NumberOfRows).ToList();
            
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var streamWriter = new StreamWriter(_beginningFilePath + currentDiaryFile.Path, true);
            using (var csvWriter = new CsvWriter(streamWriter, config))
            {
                csvWriter.WriteRecords(newDiaryMovies);
            }
               
            currentDiaryFile.NumberOfRows += newDiaryMovies.Count;
            currentDiaryFile.NumOfNewEntrys += newDiaryMovies.Count;

            await _context.SaveChangesAsync();
        }

        private async Task<List<DiaryMovieData>?> GetNewlyWatchedMovies()
        {
            var diaryFile = await GetDiaryFileOfUser();
            if (diaryFile == null) { return null; }
            if (diaryFile.NumOfNewEntrys == 0) { return null; }
            StreamReader reader = new(_beginningFilePath + diaryFile.Path);
            CsvReader csvReader = new(reader, CultureInfo.InvariantCulture);
            var newDiaryMovies = csvReader.GetRecords<DiaryMovieData>()
                .Skip(diaryFile.NumberOfRows - diaryFile.NumOfNewEntrys).ToList();
            return newDiaryMovies;
        }

        private async Task<FilePath?> GetDiaryFileOfUser()
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);

            var diaryFile = _context.FilePaths.Where(x => x.Path.StartsWith("wwwroot/files/diary")
                && x.ApplicationUserId == user.Id).FirstOrDefault();

            return diaryFile;
        }

        private async Task<FilePath?> GetStatsFileOfUser()
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);

            var aggregateFile = _context.FilePaths.Where(x => x.Path.StartsWith("wwwroot/files/statistics")
                && x.ApplicationUserId == user.Id).FirstOrDefault();

            return aggregateFile;
        }

        private static void PopulateGenreStatistics(string[] genres, Dictionary<string, int> genreCount)
        {
            foreach(var genre in genres)
            {
                if (genreCount.ContainsKey(genre))
                {
                    genreCount[genre]++;
                }
                else { genreCount.Add(genre, 1); }
            }
        }

        private static void PopulateLanguageStatistics(string[] spokenLanguages, Dictionary<string, int> languageCount)
        {
            foreach(var language in spokenLanguages)
            {
                if (languageCount.ContainsKey(language))
                {
                    languageCount[language]++;
                }
                else { languageCount.Add(language, 1); }
            }
        }

        private static void InitializeYearFilmsSeenStats(int startYear, int endYear, Dictionary<int, int> yearFilmsSeen)
        {
            for (int i = startYear; i <= endYear; i++)
            {
                yearFilmsSeen.Add(i, 0);
            }
        }

        private static void PopulateYearFilmsSeenStats(int releaseYear, Dictionary<int, int> yearFilmsSeen)
        {
            if (yearFilmsSeen.ContainsKey(releaseYear))
            {
                yearFilmsSeen[releaseYear]++;
            }
            else { yearFilmsSeen.Add(releaseYear, 1); }
        }

        private static void InitializeWeekdayFilmsSeenStats(Dictionary<string, int> weekdayFilmsSeen)
        {
            string[] days = new string[7] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            foreach(var day in days)
            {
                weekdayFilmsSeen.Add(day, 0);
            }
        }

        private static void PopulateWeekdayFilmsSeenStats(string weekday, Dictionary<string, int> weekdayFilmsSeen)
        {
            if (weekdayFilmsSeen.ContainsKey(weekday))
            {
                weekdayFilmsSeen[weekday]++;
            }
            else { weekdayFilmsSeen.Add(weekday, 1); }
        }

        private static void PopulateWeekFilmsSeenStats(int year, int dayOfYear, Dictionary<string, int> weekFilmsSeen)
        {
            int weekNum = (int) Math.Ceiling(dayOfYear / 7.0);
            weekFilmsSeen[GetMonthRangeKey(year, weekNum)]++;
        }

        private static void InitializeWeekFilmsSeenStats(Dictionary<string, int> weekFilmsSeen, int year)
        {
            for (int weekNum = 1; weekNum <= 53; weekNum++)
            {
                weekFilmsSeen.Add(GetMonthRangeKey(year, weekNum), 0);
            }
        }

        private static string GetMonthRangeKey(int year, int weekOfYear)
        {
            int dayOfYearStart = weekOfYear * 7 - 6;
            int dayOfYearEnd = weekOfYear * 7;
            if (dayOfYearEnd > 365)
            {
                dayOfYearEnd = DateTime.IsLeapYear(year) ? 366 : 365;
            }

            DateTime weekStart = new DateTime(year, 1, 1).AddDays(dayOfYearStart - 1);
            DateTime weekEnd = new DateTime(year, 1, 1).AddDays(dayOfYearEnd - 1);

            string monthStartName = weekStart.ToString("MMM");
            string monthEndName = weekEnd.ToString("MMM");

            string fullDateString;
            if (weekStart.Day == weekEnd.Day)
            {
                fullDateString = $"Week {weekOfYear}\n" +
                    $"{monthStartName} {weekStart.Day}";
                return fullDateString;
            }
            if (monthStartName == monthEndName)
            {
                fullDateString = $"Week {weekOfYear}\n" +
                    $"{monthStartName} {weekStart.Day} - {weekEnd.Day}";
                return fullDateString;
            }
            
            fullDateString = $"Week {weekOfYear}\n" +
            $"{monthStartName} {weekStart.Day} - {monthEndName} {weekEnd.Day}";
            
            return fullDateString;
        }

        private async Task<List<int>> GetTMDBMovieIds(List<DiaryMovieData> movies)
        {
            List<Task<int>> tasks = new();
            for(int i = 0; i < movies.Count; i++)
            {
                tasks.Add(_apiClient.GetTMDBMovieId(movies[i].Title, movies[i].ReleaseYear));
                
            }

            var ids = await Task.WhenAll(tasks);
            return ids.ToList();
        }

        private async Task<List<TMDBMovieData?>> GetTMDBMovies(List<int> TMDBIds)
        {
            List<Task<TMDBMovieData?>> tasks = new();
            foreach(var id in TMDBIds)
            {
                tasks.Add(_apiClient.GetTMDBMovieData(id));
            }

            var TMDBMovies = await Task.WhenAll(tasks);
            return TMDBMovies.ToList();
        }
    }

    /*
    Get credits
    Path: api.themoviedb.org/3/movie/{id}/credits?api_key={key}&language=en-US
    Two arrays of objects cast and crew
    department, job, name
    Add suggestions to watch a particular
    actor if noticed that they have recently
    watched that actor multiple time.
    */

    public interface IService
    {
        Task<List<int>> GetViewData();
        Task<MovieStatistics> GetStatistics(string year);
        Task WriteDiaryMovieDataToFile(IFormFile file);
    }
}