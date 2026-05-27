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

        private readonly IServiceScopeFactory _scopeFactory;

        public Service(ApplicationDbContext context,
            ITMDBApiClient apiClient,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor contextAccessor,
            IWebHostEnvironment environment,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _apiClient = apiClient;
            _userManager = userManager;
            _contextAccessor = contextAccessor;
            _environment = environment;
            _scopeFactory = scopeFactory;
        }

        private string GetFilesPath(IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                return Path.Combine(environment.WebRootPath, "files");
            }
            else
            {
                var path = @"D:\home\data\files";
                Directory.CreateDirectory(path);
                return path;
            }
        }

        private string GetStoredPath(string fileName, IWebHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                return "files/" + fileName;
            }
            return Path.Combine(@"D:\home\data\files", fileName);
        }

        public async Task<List<int>> GetViewData()
        {
            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);
            if (user == null) return new List<int>();

            var diaryFile = await GetDiaryFileOfUser(user.Id);
            if (diaryFile == null) return new List<int>();
            
            using var streamReader = new StreamReader(Path.IsPathRooted(diaryFile.Path) ? diaryFile.Path : Path.Combine(_environment.WebRootPath, diaryFile.Path));
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var diaryMovies = csvReader.GetRecords<DiaryMovieData>();
            return diaryMovies.Select(x => x.WatchedDate.Year).Distinct().ToList();
        }

        
        public async Task<MovieStatistics> GetStatistics(string year)
        {
            var statistics = new MovieStatistics();

            var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);
            if (user == null)
            {
                return statistics;
            }

            // Log the diary file state before populating
            var diaryFileCheck = await GetDiaryFileOfUser(user.Id);

            var aggregateFile = await GetStatsFileOfUser(user.Id);

            if (aggregateFile == null)
            {
                return statistics;
            }

            using var streamReader = new StreamReader(Path.IsPathRooted(aggregateFile.Path) ? aggregateFile.Path : Path.Combine(_environment.WebRootPath, aggregateFile.Path));
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
                if (!string.IsNullOrEmpty(movie.Rewatch) && movie.Rewatch.Equals("Yes", StringComparison.OrdinalIgnoreCase))
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

        private async Task PopulateStatisticsFile(String userId)
        {
            List<DiaryMovieData>? newDiaryMovies = await GetNewlyWatchedMovies(userId);
            if (newDiaryMovies == null) { return; }

            List<int> TMDBIds = await GetTMDBMovieIds(newDiaryMovies);
            List<TMDBMovieData?> TMDBMovies = await GetTMDBMovies(TMDBIds);

            List<AggregateMovieData> newAggregateMovies = GenerateAggregateNewlyWatchedMoviesList(TMDBMovies, newDiaryMovies);

            await WriteAggregateMovieDataToFile(newAggregateMovies, userId);

            //If everything was written correctly set diary file to no new entrys.
            var diaryFile = await GetDiaryFileOfUser(userId);

            if (diaryFile == null) { return; }

            if (diaryFile == null) { return; }

            diaryFile.NumOfNewEntrys = 0;
            await _context.SaveChangesAsync();
        }

        private async Task PopulateStatisticsFileScoped(
            string userId,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            // Get diary file using scoped context
            var diaryFile = context.FilePaths
                .Where(x => (x.Path.StartsWith("files/diary") || x.Path.Contains("diary")) && x.ApplicationUserId == userId)
                .FirstOrDefault();

            if (diaryFile == null) return;
            if (diaryFile.NumOfNewEntrys == 0) return;

            List<DiaryMovieData> newDiaryMovies;
            using (var reader = new StreamReader(Path.IsPathRooted(diaryFile.Path) ? diaryFile.Path : Path.Combine(_environment.WebRootPath, diaryFile.Path), true))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var allMovies = csvReader.GetRecords<DiaryMovieData>().ToList();
                newDiaryMovies = allMovies
                    .Skip(diaryFile.NumberOfRows - diaryFile.NumOfNewEntrys)
                    .ToList();
            }

            // Fetch TMDB data
            List<int> tmdbIds = await GetTMDBMovieIds(newDiaryMovies);
            List<TMDBMovieData?> tmdbMovies = await GetTMDBMovies(tmdbIds);

            List<AggregateMovieData> newAggregateMovies =
                GenerateAggregateNewlyWatchedMoviesList(tmdbMovies, newDiaryMovies);

            // Get or create stats file using scoped context
            var statsFile = context.FilePaths
                .Where(x => (x.Path.StartsWith("files/statistics") || x.Path.Contains("statistics")) && x.ApplicationUserId == userId)
                .FirstOrDefault();

            if (statsFile != null)
            {
                AppendAggregateMovieDataToFile(statsFile, newAggregateMovies);
            }
            else
            {
                string fileName = "statistics" + Guid.NewGuid().ToString("N") + ".csv";
                var newPath = Path.Combine(GetFilesPath(_environment), fileName);

                FilePath aggregateFilePath = new FilePath
                {
                    Path = GetStoredPath(fileName, _environment),
                    ApplicationUserId = userId
                };

                using var writer = new StreamWriter(newPath);
                using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
                csv.WriteRecords(newAggregateMovies);

                await context.FilePaths.AddAsync(aggregateFilePath);
            }

            // Reset new entries count using scoped context
            diaryFile.IsProcessing = false;
            diaryFile.NumOfNewEntrys = 0;
            await context.SaveChangesAsync();
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

        private async Task WriteAggregateMovieDataToFile(List<AggregateMovieData> aggregateMovieDataList, string userId)
        {
            if (aggregateMovieDataList == null) { return; }

            var currentAggregateFile = await GetStatsFileOfUser(userId);

            if (currentAggregateFile != null)
            {
                AppendAggregateMovieDataToFile(currentAggregateFile, aggregateMovieDataList);
                return;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) { return; }

            string fileName = String.Concat("statistics", Guid.NewGuid().ToString("N"), ".csv");
            var newCsvPath = Path.Combine(GetFilesPath(_environment), fileName);

            FilePath aggregateFilePath = new FilePath
            {
                Path = GetStoredPath(fileName, _environment),
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

        private void AppendAggregateMovieDataToFile(FilePath currentAggregateFile, List<AggregateMovieData> newAggregateMovieDataList)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var streamWriter = new StreamWriter(Path.IsPathRooted(currentAggregateFile.Path) ? currentAggregateFile.Path : Path.Combine(_environment.WebRootPath, currentAggregateFile.Path), true);
            using var csvWriter = new CsvWriter(streamWriter, config);
            csvWriter.WriteRecords(newAggregateMovieDataList);
        }

        public async Task WriteDiaryMovieDataToFile(IFormFile diaryFile, string? userId = null)
        {
            ApplicationUser? user;
            if (userId == null)
            {
                user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);
            }
            else
            {
                user = await _userManager.FindByIdAsync(userId);
            }

            if (user == null) return;

            var currentDiaryFile = await GetDiaryFileOfUser(userId);
            
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
            var newDiaryPath = Path.Combine(GetFilesPath(_environment), fileName);

            FilePath diaryFilePath = new FilePath
            {
                Path = GetStoredPath(fileName, _environment),
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

            using var streamWriter = new StreamWriter(Path.IsPathRooted(currentDiaryFile.Path) ? currentDiaryFile.Path : Path.Combine(_environment.WebRootPath, currentDiaryFile.Path), true);
            using (var csvWriter = new CsvWriter(streamWriter, config))
            {
                csvWriter.WriteRecords(newDiaryMovies);
            }
               
            currentDiaryFile.NumberOfRows += newDiaryMovies.Count;
            currentDiaryFile.NumOfNewEntrys += newDiaryMovies.Count;

            await _context.SaveChangesAsync();
        }

        private async Task<List<DiaryMovieData>?> GetNewlyWatchedMovies(string userId)
        {
            var diaryFile = await GetDiaryFileOfUser(userId);
            if (diaryFile == null) { return null; }
            if (diaryFile.NumOfNewEntrys == 0) { return null; }
            List<DiaryMovieData> newDiaryMovies;
            using (var reader = new StreamReader(Path.IsPathRooted(diaryFile.Path) ? diaryFile.Path : Path.Combine(_environment.WebRootPath, diaryFile.Path)))
            using (var csvReader = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var allMovies = csvReader.GetRecords<DiaryMovieData>().ToList();
                newDiaryMovies = allMovies
                    .Skip(diaryFile.NumberOfRows - diaryFile.NumOfNewEntrys)
                    .ToList();
            }
            return newDiaryMovies;
        }

        private async Task<FilePath?> GetDiaryFileOfUser(string? userId = null)
        {
            if (userId == null)
            {
                var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);
                if (user == null) return null;
                userId = user.Id;
            }

            return _context.FilePaths.Where(x =>
                (x.Path.StartsWith("files/diary") || x.Path.Contains("diary"))
                && x.ApplicationUserId == userId).FirstOrDefault();
        }

        private async Task<FilePath?> GetStatsFileOfUser(string? userId = null)
        {
            if (userId == null)
            {
                var user = await _userManager.GetUserAsync(_contextAccessor.HttpContext?.User);
                if (user == null) return null;
                userId = user.Id;
            }

            return _context.FilePaths.Where(x =>
                (x.Path.StartsWith("files/statistics") || x.Path.Contains("statistics"))
                && x.ApplicationUserId == userId).FirstOrDefault();
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
            var ids = new List<int>();
            int batchSize = 40;

            for(int i = 0; i < movies.Count; i += batchSize)
            {
                var batch = movies.Skip(i).Take(batchSize).ToList();
                List<Task<int>> tasks = batch
                    .Select(m => _apiClient.GetTMDBMovieId(m.Title, m.ReleaseYear))
                    .ToList();

                var batchIds = await Task.WhenAll(tasks);
                ids.AddRange(batchIds);

                if (i + batchSize < movies.Count)
                {
                    await Task.Delay(250);
                }
                
            }

            return ids;
        }

        private async Task<List<TMDBMovieData?>> GetTMDBMovies(List<int> TMDBIds)
        {
            var results = new List<TMDBMovieData?>();
            int batchSize = 40;

            for (int i = 0; i < TMDBIds.Count; i += batchSize)
            {
                var batch = TMDBIds.Skip(i).Take(batchSize).ToList();
                List<Task<TMDBMovieData?>> tasks = batch
                    .Select(id => _apiClient.GetTMDBMovieData(id))
                    .ToList();

                var batchResults = await Task.WhenAll(tasks);
                results.AddRange(batchResults);

                if (i + batchSize < TMDBIds.Count)
                {
                    await Task.Delay(250);
                }
            }

            return results;
        }

        public async Task PopulateStatisticsFileInBackground(string? userId)
        {
            if (userId == null) return;
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

                await PopulateStatisticsFileScoped(userId, context, userManager, environment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Background TMDB enrichment error: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        public async Task<bool> NeedsProcessing(string? userId)
        {
            if (userId == null) return false;
            var diaryFile = await GetDiaryFileOfUser(userId);
            return diaryFile != null && diaryFile.NumOfNewEntrys > 0;
        }

        public async Task SetProcessingFlag(string? userId, bool isProcessing)
        {
            if (userId == null) return;
            var diaryFile = await GetDiaryFileOfUser(userId);
            if (diaryFile == null) return;
            diaryFile.IsProcessing = isProcessing;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsProcessing(string? userId)
        {
            if (userId == null) return false;
            var diaryFile = await GetDiaryFileOfUser(userId);
            return diaryFile?.IsProcessing ?? false;
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
        Task WriteDiaryMovieDataToFile(IFormFile file, string? userId = null);
        Task PopulateStatisticsFileInBackground(string? userId);
        Task<bool> NeedsProcessing(string? userId);
        Task SetProcessingFlag(string? userId, bool isProcessing);
        Task<bool> IsProcessing(string? userId);
    }
}