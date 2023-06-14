using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Data.AzureTableEntity
{
    public class CommonEntity : ITableEntity
    {
        public string RowKey { get; set; } = default!;

        public string PartitionKey { get; set; } = default!;

        public ETag ETag { get; set; } = default!;

        public DateTimeOffset? Timestamp { get; set; } = default!;
    }
}
