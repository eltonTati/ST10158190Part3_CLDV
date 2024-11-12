
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
namespace ST10158190Part1_CLDV_B.Services
{
    public static class StoreInSqlDb
    {
        private static readonly string _connectionString = Environment.GetEnvironmentVariable("Server=tcp:cldvb-poe-server.database.windows.net,1433;Initial Catalog=cdlv-sql-DB-poe;Persist Security Info=False;User ID=eltonTati;Password=Heltontatysam0$;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");

        [FunctionName("StoreInSqlDb")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Processing request to store data in SQL Database.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var customerProfile = JsonConvert.DeserializeObject<CustomerProfile>(requestBody);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand("INSERT INTO CustomerProfiles (FirstName, LastName, Email, PhoneNumber) VALUES (@FirstName, @LastName, @Email, @PhoneNumber)", connection);

                    command.Parameters.AddWithValue("@FirstName", customerProfile.FirstName);
                    command.Parameters.AddWithValue("@LastName", customerProfile.LastName);
                    command.Parameters.AddWithValue("@Email", customerProfile.Email);
                    command.Parameters.AddWithValue("@PhoneNumber", customerProfile.PhoneNumber);

                    await command.ExecuteNonQueryAsync();
                }

                return new OkObjectResult("Customer profile stored successfully in SQL Database.");
            }
            catch (SqlException ex)
            {
                log.LogError($"SQL error: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private class CustomerProfile
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
        }
    }
}