using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Functions.Interfaces
{
    public interface IAzureStorageHelper
    {
        Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task<List<T>?> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();
        Task<List<T>?> GetAllEntitiesAsync<T>(string tableName, FormattableString filter) where T : class, ITableEntity, new();
        Task<bool> AddEntityAsync<T>(string tableName, T data) where T : class, ITableEntity, new();
    }
}
