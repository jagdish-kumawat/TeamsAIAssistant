using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsAIAssistant.Functions.Interfaces;

namespace TeamsAIAssistant.Functions.Services
{
    internal class AzureStorageHelper : IAzureStorageHelper
    {
        private readonly IConfiguration _configuration;
        public AzureStorageHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // create the table if not exists and return the table client
        private async Task<TableClient> GetTableClient(string tableName)
        {
            // <client_credentials> 
            // New instance of the TableClient class
            TableServiceClient tableServiceClient = new TableServiceClient(_configuration["StorageAccountConnectionString"]);
            // </client_credentials>

            // <create_table>
            // New instance of TableClient class referencing the server-side table
            TableClient tableClient = tableServiceClient.GetTableClient(
                tableName: tableName
            );

            await tableClient.CreateIfNotExistsAsync();
            // </create_table>

            return tableClient;
        }

        // check if entity data exists in azure table storage
        public async Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            try
            {
                if (string.IsNullOrEmpty(_configuration["StorageAccountConnectionString"]))
                {
                    throw new Exception("NOTE: Storage Account is not configured.");
                }

                var tableClient = await GetTableClient(tableName);

                // <get_object>
                // Get item from server-side table
                var response = await tableClient.GetEntityAsync<T>(partitionKey, rowKey);
                // </get_object>

                return response;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // get all entities data from azure table storage
        public async Task<List<T>?> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new()
        {
            try
            {
                if (string.IsNullOrEmpty(_configuration["StorageAccountConnectionString"]))
                {
                    throw new Exception("NOTE: Storage Account is not configured.");
                }

                var tableClient = await GetTableClient(tableName);

                // <get_object>
                // Get item from server-side table
                var response = tableClient.Query<T>();
                // </get_object>

                return response.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // get all entities data from azure table storage based on filter
        public async Task<List<T>?> GetAllEntitiesAsync<T>(string tableName, FormattableString filter) where T : class, ITableEntity, new()
        {
            try
            {
                if (string.IsNullOrEmpty(_configuration["StorageAccountConnectionString"]))
                {
                    throw new Exception("NOTE: Storage Account is not configured.");
                }

                var tableClient = await GetTableClient(tableName);

                // <get_object>
                // Get item from server-side table
                var response = tableClient.Query<T>(filter: TableClient.CreateQueryFilter(filter));
                // </get_object>

                return response.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        // add entity data in azure table storage
        public async Task<bool> AddEntityAsync<T>(string tableName, T data) where T : class, ITableEntity, new()
        {
            try
            {
                if (string.IsNullOrEmpty(_configuration["StorageAccountConnectionString"]))
                {
                    throw new Exception("NOTE: Storage Account is not configured.");
                }

                var tableClient = await GetTableClient(tableName);
                await tableClient.UpsertEntityAsync<T>(data);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
