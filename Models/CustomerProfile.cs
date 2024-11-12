using Azure;
using Azure.Data.Tables;

namespace ST10158190Part1_CLDV_B.Models
{
    public class CustomerProfile : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public CustomerProfile()
        {
            PartitionKey = "Customer";
            RowKey = Guid.NewGuid().ToString();
        }
    }
}
