using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MovieAnalyticsWeb.Models;
using MovieAnalyticsWeb.Data;

namespace MovieAnalyticsWeb.Controllers
{
    public class DemoController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IService _service;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        private const string DemoSessionKey = "DemoUserEmail";
        private const string DemoPassword = "Demo1234!";

        public DemoController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IService service,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _service = service;
            _context = context;
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> Start()
        {
            // Generate a unique email for this demo session
            var demoEmail = $"demo-{Guid.NewGuid():N}@movieanalytics.com";

            var demoUser = new ApplicationUser
            {
                FirstName = "Demo",
                LastName = "Account",
                UserName = demoEmail,
                Email = demoEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(demoUser, DemoPassword);
            if (!result.Succeeded)
            {
                TempData["uploadMessage"] = "Failed to create demo account. Please try again.";
                return RedirectToAction("Index", "Home");
            }

            // Store the unique email in session so End() knows which account to delete
            HttpContext.Session.SetString(DemoSessionKey, demoEmail);

            await _signInManager.SignInAsync(demoUser, isPersistent: false);

            var bytes = System.Text.Encoding.UTF8.GetBytes(GetSampleCsv());
            var stream = new MemoryStream(bytes);
            IFormFile sampleFile = new FormFile(stream, 0, bytes.Length, "diaryFile", "sample_diary.csv")
            {
                Headers = new HeaderDictionary(),
                ContentType = "text/csv"
            };

            await _service.WriteDiaryMovieDataToFile(sampleFile, demoUser.Id);

            TempData["uploadMessage"] = "Demo loaded! Explore your stats below.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> End()
        {
            var demoEmail = HttpContext.Session.GetString(DemoSessionKey);

            await _signInManager.SignOutAsync();
            HttpContext.Session.Remove(DemoSessionKey);

            if (!string.IsNullOrEmpty(demoEmail))
            {
                var demoUser = await _userManager.FindByEmailAsync(demoEmail);
                if (demoUser != null)
                {
                    await DeleteDemoFiles(demoUser.Id);
                    await _userManager.DeleteAsync(demoUser);
                }
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task DeleteDemoFiles(string userId)
        {
            var filePaths = _context.FilePaths
                .Where(x => x.ApplicationUserId == userId)
                .ToList();

            foreach (var filePath in filePaths)
            {
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.Path);

                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    try
                    {
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                        break;
                    }
                    catch (IOException) when (attempt < 5)
                    {
                        Console.WriteLine($"=== File locked, retry {attempt}/5: {fullPath}");
                        await Task.Delay(attempt * 500);
                    }
                }
            }

            _context.FilePaths.RemoveRange(filePaths);
            await _context.SaveChangesAsync();
        }

        private string GetSampleCsv() =>
            "Name,Watched Date,Year,Rewatch\n" +
            "The Dark Knight,2024-01-05,2008,No\n" +
            "Inception,2024-01-12,2010,No\n" +
            "Interstellar,2024-01-20,2014,No\n" +
            "Parasite,2024-02-03,2019,No\n" +
            "Mad Max: Fury Road,2024-02-14,2015,No\n" +
            "Arrival,2024-02-22,2016,No\n" +
            "Get Out,2024-03-01,2017,No\n" +
            "The Revenant,2024-03-10,2015,No\n" +
            "Dune,2024-03-18,2021,No\n" +
            "Spider-Man: No Way Home,2024-03-25,2021,Yes\n" +
            "Everything Everywhere All at Once,2024-04-01,2022,No\n" +
            "The Batman,2024-04-08,2022,No\n" +
            "Top Gun: Maverick,2024-04-15,2022,No\n" +
            "Tár,2024-04-22,2022,No\n" +
            "Nope,2024-05-01,2022,No\n" +
            "The Whale,2024-05-08,2022,No\n" +
            "Babylon,2024-05-15,2022,No\n" +
            "Avatar: The Way of Water,2024-05-22,2022,No\n" +
            "The Fabelmans,2024-06-01,2022,No\n" +
            "Aftersun,2024-06-08,2022,No";
    }
}