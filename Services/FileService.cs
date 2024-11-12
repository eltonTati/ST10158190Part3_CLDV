using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace ST10158190Part1_CLDV_B.Services
{
    public class FileService
    {
        private readonly ShareClient _shareClient;

        public FileService(IConfiguration configuration)
        {
          
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _shareClient = new ShareClient(connectionString, "contracts-and-logs");
        }

        public async Task UploadFileAsync(string directoryName, string fileName, Stream content)
        {
           
            await _shareClient.CreateIfNotExistsAsync();

          
            var directoryClient = _shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            
            var fileClient = directoryClient.GetFileClient(fileName);

            
            await fileClient.CreateAsync(content.Length);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, content.Length), content);
        }
    }
}
