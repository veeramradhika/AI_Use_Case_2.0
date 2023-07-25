using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Use_Case_2.Controllers
{
    public class HomeController : Controller
    {
        const string API_KEY = "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";
        private readonly ILogger<HomeController> _logger;
        static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(500) // Set a timeout of 500 seconds (5 minutes)
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
        public async Task<IActionResult> Get(bool useFileAsReference, string prompt, IFormFile file, string filename)
        {
            string inputPrompt = prompt;

            if (useFileAsReference && file != null)
            {
                try
                {
                    // Read and parse the CSV file
                    using (var reader = new StreamReader(file.OpenReadStream()))
                    {
                        List<string[]> rows = new List<string[]>();
                        while (!reader.EndOfStream)
                        {
                            string line = await reader.ReadLineAsync();
                            string[] data = line.Split(',');
                            rows.Add(data);
                        }

                        // Filter the data you want to include in the prompt from the CSV file
                        // For example, you can join specific columns or rows to create the input prompt
                        List<string> filteredData = rows.Select(row => string.Join(", ", row)).ToList();

                        // Combine the filtered data and the user-provided prompt to create the final input prompt
                        inputPrompt = string.Join("\n", filteredData) + "\n\n" + prompt;
                    }
                }
                catch (Exception ex)
                {
                    return Json("Error reading the CSV file: " + ex.Message);
                }
            }

            if (string.IsNullOrEmpty(inputPrompt))
            {
                return Json("Please provide a prompt.");
            }

            var options = new Dictionary<string, object>
            {
                { "model", "gpt-3.5-turbo" },
                { "max_tokens", 3500 },
                { "temperature", 0.1 }
            };

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", API_KEY);

            try
            {
                options["messages"] = new[]
                {
                    new
                    {
                        role = "user",
                        content = inputPrompt
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

                // Save the data to a CSV file with the specified filename
                string csvContent = $"Generated Response: {result}";
                string filePath = Path.Combine(Path.GetTempPath(), $"{filename}.csv");
                await System.IO.File.WriteAllTextAsync(filePath, csvContent, Encoding.UTF8);

                ViewBag.FileNames = new List<string> { $"{filename}.csv" };
                return View("Index");
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        // Add a new action to download the CSV file for a specific filename
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
