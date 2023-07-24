using AI_Use_Case_2._0.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;

namespace AI_Use_Case_2._0.Controllers
{
    public class HomeController : Controller
    {
        const string API_KEY = "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";
        private readonly ILogger<HomeController> _logger;
        static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(500) // Set a timeout of 300 seconds (5 minutes)
        };

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Get(string prompt1, string prompt2, string prompt3, string csvFileName1, string csvFileName2, string csvFileName3)
        {
            var prompts = new string[] { prompt1, prompt2, prompt3 };
            var resultFiles = new List<string>();
            var inputFiles = new List<string>();

            var options = new Dictionary<string, object>
            {
                { "model", "gpt-3.5-turbo" },
                { "max_tokens", 3500 },
                { "temperature", 0.2 }
            };

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

            try
            {
                for (int i = 0; i < prompts.Length; i++)
                {
                    string input = prompts[i];

                    if (!string.IsNullOrEmpty(input))
                    {
                        options["messages"] = new[]
                        {
                            new
                            {
                                role = "user",
                                content = input
                            }
                        };

                        var json = JsonConvert.SerializeObject(options);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        var startTime = DateTime.Now;
                        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                        response.EnsureSuccessStatusCode();

                        var responseBody = await response.Content.ReadAsStringAsync();
                        dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                        string result = jsonResponse.choices[0].message.content;
                        var endTime = DateTime.Now;
                        var responseTime = (endTime - startTime).TotalMilliseconds;

                        // Save the data to a CSV file with the user-provided file name
                        string csvContent = $"Prompt: {input}\nGenerated Response: {result}\nResponse Time (ms): {responseTime}";
                        string fileName = $"result_{i + 1}.csv"; // Default file name if user doesn't provide one

                        // Use the user-provided file name if available
                        if (i == 0 && !string.IsNullOrEmpty(csvFileName1))
                            fileName = $"{csvFileName1}.csv";
                        else if (i == 1 && !string.IsNullOrEmpty(csvFileName2))
                            fileName = $"{csvFileName2}.csv";
                        else if (i == 2 && !string.IsNullOrEmpty(csvFileName3))
                            fileName = $"{csvFileName3}.csv";

                        string filePath = Path.Combine(Path.GetTempPath(), fileName);
                        await System.IO.File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);
                        resultFiles.Add(fileName);

                        // Store the input files for the third prompt
                        if (i < 2)
                        {
                            inputFiles.Add(filePath);
                        }
                    }
                }

                ViewBag.FileNames = resultFiles;
                return View("Index");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        // Add a new action to download the CSV file for a specific prompt
        public IActionResult Download(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                string filePath = Path.Combine(Path.GetTempPath(), fileName);
                if (System.IO.File.Exists(filePath))
                {
                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(fileBytes, "text/csv", fileName);
                }
            }
            return NotFound();
        }
    }
}