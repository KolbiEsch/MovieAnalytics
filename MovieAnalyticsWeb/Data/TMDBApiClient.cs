using MovieAnalyticsWeb.Models;
using System.Text.RegularExpressions;

namespace MovieAnalyticsWeb.Data
{
    public class TMDBApiClient : ITMDBApiClient
    {
        private readonly ApplicationDbContext _context;

        private static HttpClient _client = new();

        private readonly IConfiguration _config;
        

        public TMDBApiClient(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            
        }

        public async Task<int> GetTMDBMovieId(string title, int year)
        {
            var titleLower = title.ToLower();
            var response = await _client.GetAsync($"http://api.themoviedb.org/3/search/movie?api_key={_config.GetValue<string>("TMDB-Key")}&language=en-US&page=1&include_adult=false&query={title}&primary_release_year={year}");
            if (response.IsSuccessStatusCode)
            {
                var query = await response.Content.ReadFromJsonAsync<MovieQuery>();
                if (query != null && query.Results.Length > 0)
                {
                    var internalQuery = 
                        from result in query.Results
                        where result.Title.ToLower() == titleLower
                        select result.Id;

                    if (!internalQuery.Any())
                    {
                        var id = await GetTMDBMovieId(titleLower);
                        return id;
                    }

                    return internalQuery.First();
                }
                else
                {
                    var id = await GetTMDBMovieId(titleLower);
                    return id;
                }
                
            }
            return -1;
        }

        
        private async Task<int> GetTMDBMovieId(string title)
        {
            var titleNoPuncSpace = Regex.Replace(title, @"[^\w]", string.Empty);
            var response = await _client.GetAsync($"http://api.themoviedb.org/3/search/movie?api_key={_config.GetValue<string>("TMDB-Key")}&language=en-US&page=1&include_adult=false&query={title}");
            if (response.IsSuccessStatusCode)
            {
                var query = await response.Content.ReadFromJsonAsync<MovieQuery>();
                if (query != null && query.Results.Length > 0)
                {
                    var id = -1;
                    foreach(var result in query.Results)
                    {
                        var resultNoPuncSpace = Regex.Replace(result.Title.ToLower(), @"[^\w]", string.Empty);
                        if (resultNoPuncSpace == titleNoPuncSpace)
                        {
                            id = result.Id;
                            break;
                        }
                    }
                    
                    if (id == -1)
                    {
                        return -1;
                    }

                    return id;
                }
            }
            return -1;
        }
        
        public async Task<TMDBMovieData?> GetTMDBMovieData(int id)
        {
            if (id == -1)
            {
                return null;
            }
            
            var response = await _client.GetAsync($"http://api.themoviedb.org/3/movie/{id}?api_key=0a4c5931f3c0954caf17b48a337cd59e&language=en-US");
            if (response.IsSuccessStatusCode)
            {
                var movieData = await response.Content.ReadFromJsonAsync<TMDBMovieData>();
                if (movieData != null)
                {
                    return movieData;
                }
            }
            
            return null;
        }
    }

    public interface ITMDBApiClient
    {
        Task<TMDBMovieData?> GetTMDBMovieData(int id);

        Task<int> GetTMDBMovieId(string title, int year);
    }
}