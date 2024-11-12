using Azure;
using Azure.Data.Tables;
using ST10158190Part1_CLDV_B.Models;
using System.Threading.Tasks;

namespace ST10158190Part1_CLDV_B.Services
{
    public class TableService
    {
        private readonly TableClient _tableClient;

        public TableService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            var tableName = "CustomerProfiles"; 
            var tableServiceClient = new TableServiceClient(connectionString);
            _tableClient = tableServiceClient.GetTableClient(tableName);
            _tableClient.CreateIfNotExists();
        }

        public async Task AddCustomerProfileAsync(CustomerProfile profile)
        {
            if (profile != null)
            {
                await _tableClient.UpsertEntityAsync(profile);
            }
        }
    }
}
