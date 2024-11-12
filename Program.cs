using Azure.Storage.Blobs;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using ST10158190Part1_CLDV_B.Services;
using System.Net.Http;

namespace ST10158190Part1_CLDV_B
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("AzureStorage");

            // Register your services
            builder.Services.AddSingleton<BlobService>();
            builder.Services.AddSingleton<TableService>();
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<QueueService>();

            // Register HttpClient
            builder.Services.AddHttpClient();

            // Add controllers with views
            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            // Configure middleware
            app.UseRouting();
            app.UseAuthorization();

            // Configure routes
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
