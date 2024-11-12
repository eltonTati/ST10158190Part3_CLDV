using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Threading.Tasks;

 namespace ST10158190Part1_CLDV_B.Services
{
    public class QueueService
    {
        private readonly QueueClient _queueClient;

        public QueueService(IConfiguration configuration)
        {
            
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _queueClient = new QueueClient(connectionString, "order-processing-queue");
            _queueClient.CreateIfNotExists();
        }

        public async Task EnqueueOrderAsync(string orderId)
        {
            if (!string.IsNullOrEmpty(orderId))
            {
                var message = Encoding.UTF8.GetBytes(orderId);
                await _queueClient.SendMessageAsync(Convert.ToBase64String(message));
            }
        }
    }
}
