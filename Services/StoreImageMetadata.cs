using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;

public static class StoreImageMetadata
{
    private static readonly string _connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

    [FunctionName("StoreImageMetadata")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Processing request to store image metadata in SQL Database.");

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var imageMetadata = JsonConvert.DeserializeObject<ImageMetadata>(requestBody);

        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("INSERT INTO ImageMetadata (FileName, ContentType, BlobUrl, UploadDate) VALUES (@FileName, @ContentType, @BlobUrl, @UploadDate)", connection);

                command.Parameters.AddWithValue("@FileName", imageMetadata.FileName);
                command.Parameters.AddWithValue("@ContentType", imageMetadata.ContentType);
                command.Parameters.AddWithValue("@BlobUrl", imageMetadata.BlobUrl);
                command.Parameters.AddWithValue("@UploadDate", imageMetadata.UploadDate);

                await command.ExecuteNonQueryAsync();
            }

            return new OkObjectResult("Image metadata stored successfully in SQL Database.");
        }
        catch (SqlException ex)
        {
            log.LogError($"SQL error: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private class ImageMetadata
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public string BlobUrl { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
