using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Microsoft.AspNetCore.Mvc;
using MovieAnalyticsWeb.Data;
using MovieAnalyticsWeb.Models;
using System.Diagnostics;
using System.Linq;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Newtonsoft.Json;

namespace MovieAnalyticsWeb.Controllers
{
    public class HomeController : Controller
    {

        private readonly IService _service;
        

        public HomeController(IService service)
        {
            _service = service;
        }

        public IActionResult Index()
        {
            
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> UploadDiaryFile(IFormFile diaryFile)
        {
            if (diaryFile == null || diaryFile.Length == 0)
            {
                TempData["uploadMessage"] = "Please select a file or make sure the file is not empty.";
                return RedirectToAction("Index");
            }

            if (diaryFile.Length > 10 * 2014 * 1024)
            {
                TempData["uploadMessage"] = "File is too large. Maximum size is 10MB.";
                return RedirectToAction("Indext");
            }

            var fileExtension = Path.GetExtension(diaryFile.FileName).ToLowerInvariant();

            if (fileExtension != ".csv")
            {
                TempData["uploadMessage"] = "Incorrect file type. Please upload a CSV file";
                return RedirectToAction("Index");
            }

            var allowedMimeTypes = new[] { "text/csv", "text/plain", "application/csv", "application/vnd.ms-excel" };
            if (!allowedMimeTypes.Contains(diaryFile.ContentType.ToLowerInvariant()))
            {
                TempData["uploadMessage"] = "Incorrect file type. Please upload a .csv file";
                return RedirectToAction("Index");
            }

            using var reader = new StreamReader(diaryFile.OpenReadStream());
            var firstLine = await reader.ReadLineAsync();
            if (firstLine == null || firstLine.Any(c => char.IsControl(c) && c != '\t'))
            {
                TempData["uploadMessage"] = "File does not appear to be a valid CSV.";
                return RedirectToAction("Index");
            }

            await _service.WriteDiaryMovieDataToFile(diaryFile);
            TempData["uploadMessage"] = "File was successfully uploaded.";
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> VisualizeData()
        {
            var model = new VisualizeDataViewModel();
            model.YearsOfData = await _service.GetViewData();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool needsProcessing = await _service.NeedsProcessing(userId);

            if (needsProcessing)
            {
                await _service.SetProcessingFlag(userId, true);
                _ = Task.Run(() => _service.PopulateStatisticsFileInBackground(userId));
                ViewBag.IsProcessing = true;
            } else
            {
                ViewBag.IsProcessing = await _service.IsProcessing(userId);
            }
           
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public async Task<MovieStatistics> GetDataByYear(string year)
        {
            var stats = await _service.GetStatistics(year);
            return stats;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ProcessingStatus()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            bool isProcessing = await _service.NeedsProcessing(userId) ||
                                await _service.IsProcessing(userId);
            return Json(new { isProcessing });
        }
    }
}