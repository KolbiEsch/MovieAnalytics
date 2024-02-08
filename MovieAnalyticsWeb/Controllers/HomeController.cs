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

            var fileExtension = Path.GetExtension(diaryFile.FileName);

            if (fileExtension != ".csv")
            {
                TempData["uploadMessage"] = "Incorrect file type.";
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
            return View(model);
        }

        [HttpGet]
        public async Task<MovieStatistics> GetDataByYear(string year)
        {
            var stats = await _service.GetStatistics(year);
            return stats;
        }
    }
}