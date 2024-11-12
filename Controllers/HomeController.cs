using Microsoft.AspNetCore.Mvc;
using ST10158190Part1_CLDV_B.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using ST10158190Part1_CLDV_B.Services;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
using Azure.Data.Tables;
using Microsoft.Data.SqlClient;
//ST10158190



namespace ST10158190Part1_CLDV_B.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeController> _logger;
        private readonly TableService _tableService;
        private readonly FileService _fileService;
        private readonly QueueService _queueService;

        public HomeController(HttpClient httpClient, ILogger<HomeController> logger, TableService tableService, FileService fileService, QueueService queueService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tableService = tableService;
            _fileService = fileService;
            _queueService = queueService;
        }
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var containerName = "product-image";
                var blobName = file.FileName;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    // Prepare Blob Storage upload request content
                    var content = new MultipartFormDataContent();
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    content.Add(streamContent, "file", file.FileName);

                    // Azure Function URL for Blob Storage upload
                    var blobFunctionUrl = "https://st10158190-function.azurewebsites.net/api/UploadBlob?code=vpmuYfgCeISL5QC0EPdEJOgZ43lt--QJDIvfLCPVDeSPAzFuMhi4gQ%3D%3D";
                    var blobResponse = await _httpClient.PostAsync(blobFunctionUrl, content);

                    if (!blobResponse.IsSuccessStatusCode)
                    {
                        ViewBag.Message = $"File upload to Blob Storage ";
                        return View();
                    }

                    
                    var imageMetadata = new
                    {
                        FileName = file.FileName,
                        ContentType = file.ContentType,
                        BlobUrl = $"https://yourstorageaccount.blob.core.windows.net/{containerName}/{blobName}",
                        UploadDate = DateTime.UtcNow
                    };

                    // Azure Function URL for SQL Database metadata insertion
                    var sqlFunctionUrl = "https://st10158190-function.azurewebsites.net/api/StoreImageMetadata?code=YOUR_SQL_FUNCTION_CODE";
                    var jsonContent = JsonConvert.SerializeObject(imageMetadata);
                    var sqlContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Call SQL Database function
                    var sqlResponse = await _httpClient.PostAsync(sqlFunctionUrl, sqlContent);

                    if (!sqlResponse.IsSuccessStatusCode)
                    {
                        ViewBag.Message = "Image metadata storage failed in SQL Database.";
                        return View();
                    }

                    ViewBag.Message = "Image uploaded successfully!";
                }
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(string firstName, string lastName, string email, string phoneNumber)
        {
            
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phoneNumber))
            {
                ViewBag.Message = "All fields are required.";
                return View();
            }

            // Create a new customer profile object
            var customerProfile = new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phoneNumber
            };

            
            var tableResult = await InsertIntoTableStorageAsync(customerProfile);

            // Insert into Azure SQL Database
            var sqlResult = await InsertIntoSqlDatabaseAsync(customerProfile);

            
            if (tableResult && sqlResult)
            {
                ViewBag.Message = "An error occurred while adding the customer profile.";
            }
            else
            {
                ViewBag.Message = "Customer profile added successfully!";
            }

            return View();
        }

        private async Task<bool> InsertIntoTableStorageAsync(dynamic customerProfile)
        {
            try
            {
              
                var connectionString = Environment.GetEnvironmentVariable("TableStorageConnectionString");

                
                var serviceClient = new TableServiceClient(connectionString);
                var tableClient = serviceClient.GetTableClient("CustomerProfiles");
                await tableClient.CreateIfNotExistsAsync();

                // Create a TableEntity and add the customer profile data
                var entity = new TableEntity("Customer", Guid.NewGuid().ToString())
        {
            { "FirstName", customerProfile.FirstName },
            { "LastName", customerProfile.LastName },
            { "Email", customerProfile.Email },
            { "PhoneNumber", customerProfile.PhoneNumber }
        };
                await tableClient.AddEntityAsync(entity);
                return true;
            }
            catch (Exception ex)
            {
                
                _logger.LogError($"Error inserting into Table Storage: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InsertIntoSqlDatabaseAsync(dynamic customerProfile)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

                // Create SQL query to insert the customer profile into the database
                var query = "INSERT INTO CustomerProfiles (FirstName, LastName, Email, PhoneNumber) " +
                            "VALUES (@FirstName, @LastName, @Email, @PhoneNumber)";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@FirstName", customerProfile.FirstName);
                        command.Parameters.AddWithValue("@LastName", customerProfile.LastName);
                        command.Parameters.AddWithValue("@Email", customerProfile.Email);
                        command.Parameters.AddWithValue("@PhoneNumber", customerProfile.PhoneNumber);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError($"Error inserting into SQL Database: {ex.Message}");
                return false;
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var functionUrl = "https://st10158190-function.azurewebsites.net/api/UploadFile?code=GhcFvXce2y6wLKkW4UVPqQ9-mGixahh1b0VeD3b6hOtCAzFu3EoTZQ%3D%3D";

                using var httpClient = new HttpClient();
                using var stream = file.OpenReadStream();
                using var content = new MultipartFormDataContent();

               
                var fileContent = new StreamContent(stream)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue(file.ContentType) }
                };
                content.Add(fileContent, "file", file.FileName);

                
                var response = await httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "File upload failed: " + await response.Content.ReadAsStringAsync();
                }
                else
                {
                    ViewBag.Message = "Contract uploaded successfully.";
                }
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult UploadLod()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> UploadLog(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    await _fileService.UploadFileAsync("logs", file.FileName, stream);
                }
                ViewBag.Message = "Log uploaded successfully.";
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            if (!string.IsNullOrEmpty(orderId))
            {
                var functionUrl = "https://st10158190-function.azurewebsites.net/api/ProcessQueueMessage?code=lS8llMjR20GdA6MCGc281jRtZ3z8HeTy4y8y3eVUY5aFAzFuzg8a1A%3D%3D";

                using var httpClient = new HttpClient();
                var content = new StringContent(orderId, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    ViewBag.Message = "Order processing failed: " + await response.Content.ReadAsStringAsync();
                }
            }
            else
            {
                ViewBag.Message = "Please enter a valid Order ID.";
            }

            return View();
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
//____________________________________________________THIS IS MY PART 2 CODE BELOW____________________________________________________________________+
/* using Microsoft.AspNetCore.Mvc;
using ST10158190Part1_CLDV_B.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using ST10158190Part1_CLDV_B.Services;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;
//ST10158190



namespace ST10158190Part1_CLDV_B.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HomeController> _logger;
        private readonly TableService _tableService;
        private readonly FileService _fileService;
        private readonly QueueService _queueService;

        public HomeController(HttpClient httpClient, ILogger<HomeController> logger, TableService tableService, FileService fileService, QueueService queueService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tableService = tableService;
            _fileService = fileService;
            _queueService = queueService;
        }
        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var containerName = "product-image";
                var blobName = file.FileName;

                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    var content = new MultipartFormDataContent();
                    stream.Position = 0;
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    content.Add(streamContent, "file", file.FileName);

                  
                    var functionUrl ="https://st10158190-function.azurewebsites.net/api/UploadBlob?code=vpmuYfgCeISL5QC0EPdEJOgZ43lt--QJDIvfLCPVDeSPAzFuMhi4gQ%3D%3D";
                    var response = await _httpClient.PostAsync(functionUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Message = $"File upload failed: {response.ReasonPhrase}";
                    }
                    else
                    {
                        ViewBag.Message = "File uploaded successfully!";
                    }
                }
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }
        [HttpGet]
        public IActionResult AddCustomerProfile()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddCustomerProfile(string firstName, string lastName, string email, string phoneNumber)
        {
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) ||
                string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phoneNumber))
            {
                ViewBag.Message = "All fields are required.";
                return View();
            }

            var customerProfile = new
            {
                tableName = "CustomerProfiles", 
                partitionKey = "Customer",
                rowKey = Guid.NewGuid().ToString(),
                firstName,
                lastName,
                email,
                phoneNumber
            };
                                
            
            var functionUrl = "https://st10158190-function.azurewebsites.net/api/StoreTableInfo?code=LIzgRDD2ONaxjBSYfA7dw9tkSY4p6zgV3x59d8PvU122AzFuJWyKGw%3D%3D";

            using var httpClient = new HttpClient();
            var jsonContent = JsonConvert.SerializeObject(customerProfile);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            
            var response = await httpClient.PostAsync(functionUrl, content);

            if (response.IsSuccessStatusCode)
            {
                ViewBag.Message = await response.Content.ReadAsStringAsync();
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError("CustomerProfile", $"Error adding customer profile: {errorMessage}");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadContract(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var functionUrl = "https://st10158190-function.azurewebsites.net/api/UploadFile?code=GhcFvXce2y6wLKkW4UVPqQ9-mGixahh1b0VeD3b6hOtCAzFu3EoTZQ%3D%3D";

                using var httpClient = new HttpClient();
                using var stream = file.OpenReadStream();
                using var content = new MultipartFormDataContent();

               
                var fileContent = new StreamContent(stream)
                {
                    Headers = { ContentType = new MediaTypeHeaderValue(file.ContentType) }
                };
                content.Add(fileContent, "file", file.FileName);

                
                var response = await httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = "File upload failed: " + await response.Content.ReadAsStringAsync();
                }
                else
                {
                    ViewBag.Message = "Contract uploaded successfully.";
                }
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }

        [HttpGet]
        public IActionResult UploadLod()
        {
            return View();
        }
        
        [HttpPost]
        public async Task<IActionResult> UploadLog(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    await _fileService.UploadFileAsync("logs", file.FileName, stream);
                }
                ViewBag.Message = "Log uploaded successfully.";
            }
            else
            {
                ViewBag.Message = "No file selected.";
            }

            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ProcessOrder(string orderId)
        {
            if (!string.IsNullOrEmpty(orderId))
            {
                var functionUrl = "https://st10158190-function.azurewebsites.net/api/ProcessQueueMessage?code=lS8llMjR20GdA6MCGc281jRtZ3z8HeTy4y8y3eVUY5aFAzFuzg8a1A%3D%3D";

                using var httpClient = new HttpClient();
                var content = new StringContent(orderId, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(functionUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Message = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    ViewBag.Message = "Order processing failed: " + await response.Content.ReadAsStringAsync();
                }
            }
            else
            {
                ViewBag.Message = "Please enter a valid Order ID.";
            }

            return View();
        }


        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}*/