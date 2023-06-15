using Azure.Data.Tables;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Bot.Interfaces.Azure
{
    public interface IAzureStorageHelper
    {
        Task DeleteTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken);
        Task StoreTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken);
        Task<T> GetEntityAsync<T>(string tableName, string partitionKey, string rowKey) where T : class, ITableEntity, new();
        Task<bool> AddEntityAsync<T>(string tableName, T data) where T : class, ITableEntity, new();
        Task<List<T>?> GetAllEntitiesAsync<T>(string tableName) where T : class, ITableEntity, new();
        Task<List<T>?> GetAllEntitiesAsync<T>(string tableName, FormattableString filter) where T : class, ITableEntity, new();
    }
}
