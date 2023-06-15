using Azure.Data.Tables;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using TeamsAIAssistant.Bot.Interfaces.Azure;
using TeamsAIAssistant.Bot.Interfaces.Teams;
using TeamsAIAssistant.Data.AzureTableEntity;

namespace TeamsAIAssistant.Bot.Services.Azure
{
    public class AzureStorageHelper : IAzureStorageHelper
    {
        private readonly ILogger<AzureStorageHelper> _logger;
        private readonly IConfiguration _configuration;
        private readonly ITeamsHelper _teamsHelper;
        public AzureStorageHelper(IConfiguration configuration, ILogger<AzureStorageHelper> logger, ITeamsHelper teamsHelper) 
        { 
            _configuration = configuration;
            _logger = logger;
            _teamsHelper = teamsHelper;
        }

        // delete the teams user details from azure table storage
        public async Task DeleteTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            string aadObjectId = turnContext.Activity.From.AadObjectId;
            string tenantId = turnContext.Activity.Conversation.TenantId;

            if (!string.IsNullOrEmpty(aadObjectId) && !string.IsNullOrEmpty(tenantId))
            {
                try
                {
                    switch (turnContext.Activity.Conversation.ConversationType)
                    {
                        case "personal":
                            if (string.IsNullOrEmpty(_configuration["StorageAccount:ConnectionString"]))
                            {
                                throw new Exception("NOTE: Storage Account is not configured.");
                            }

                            var tableClient = await GetTableClient(_configuration["StorageAccount:UserDetailsTableName"]);

                            try
                            {
                                // delete item in server-side table
                                await tableClient.DeleteEntityAsync(tenantId, aadObjectId);
                                // </delete_object>
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("User details not found");
                            }
                            break;

                        case "channel":
                            if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
                            {
                                throw new Exception("NOTE: Storage Account is not configured for Tweet Chatbot.");
                            }

                            var tableClient1 = await GetTableClient(_configuration["TableData:StorageAccountGroupTable"]);

                            try
                            {
                                var channelData = (JObject)turnContext.Activity.ChannelData;
                                var groupId = (string)channelData["team"]["id"];
                                // Add new item to server-side table
                                await tableClient1.DeleteEntityAsync(tenantId, groupId);
                                // </create_object_add>
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("Group details not found");
                            }
                            break;

                        default: break;
                    }
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        // store the teams user details in azure table storage
        public async Task StoreTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {
                if (turnContext.Activity.ChannelId == "msteams")
                {
                    try
                    {
                        switch (turnContext.Activity.Conversation.ConversationType)
                        {
                            case "personal":
                                TeamsChannelAccount teamsUser = await _teamsHelper.GetTeamsMemberAsync(turnContext, cancellationToken);
                                if (teamsUser != null && !teamsUser.UserPrincipalName.ToLower().Contains("#ext#"))
                                {
                                    if (string.IsNullOrEmpty(_configuration["StorageAccount:ConnectionString"]))
                                    {
                                        throw new Exception("NOTE: Storage Account is not configured.");
                                    }

                                    var tableClient = await GetTableClient(_configuration["StorageAccount:UserDetailsTableName"]);

                                    // <create_object_add> 
                                    // Create new item using composite key constructor
                                    var teamsUserDetails = new UserDetailsEntity()
                                    {
                                        RowKey = turnContext.Activity.From.AadObjectId,
                                        PartitionKey = turnContext.Activity.Conversation.TenantId,
                                        UserDisplayName = teamsUser.Name,
                                        UserEmail = teamsUser.Email.ToLower(),
                                        UserID = turnContext.Activity.From.Id,
                                        UserRole = turnContext.Activity.From.Role,
                                        ConversationID = turnContext.Activity.Conversation.Id,
                                        ServiceURL = turnContext.Activity.ServiceUrl,
                                        AadObjectID = turnContext.Activity.From.AadObjectId,
                                        TenantID = turnContext.Activity.Conversation.TenantId
                                    };

                                    // Add new item to server-side table
                                    await tableClient.UpsertEntityAsync<UserDetailsEntity>(teamsUserDetails);
                                    // </create_object_add>
                                }
                                else
                                {
                                    await turnContext.SendActivityAsync(MessageFactory.Text("I am having some trouble getting your teams profile. Please contact your Administrator."));

                                    throw new Exception("NOTE: Teams User Details not found.");
                                }

                                break;

                            case "channel":
                                // Gets the details for the given team id. This only works in team scoped conversations.
                                // TeamsGetTeamInfo: Gets the TeamsInfo object from the current activity.
                                TeamDetails teamDetails = await TeamsInfo.GetTeamDetailsAsync(turnContext, turnContext.Activity.TeamsGetTeamInfo().Id);
                                if (teamDetails != null)
                                {
                                    if (string.IsNullOrEmpty(_configuration["ConnectionStrings:StorageAccount"]))
                                    {
                                        throw new Exception("NOTE: Storage Account is not configured.");
                                    }

                                    var tableClient = await GetTableClient(_configuration["TableData:StorageAccountGroupTable"]);

                                    // <create_object_add> 
                                    // Create new item using composite key constructor
                                    //var teamsGroupDetails = new GroupDetails()
                                    //{
                                    //    RowKey = teamDetails.Id,
                                    //    PartitionKey = dc.Context.Activity.Conversation.TenantId,
                                    //    TeamName = teamDetails.Name,
                                    //    AadGroupID = teamDetails.AadGroupId,
                                    //    ServiceURL = dc.Context.Activity.ServiceUrl,
                                    //};

                                    // Add new item to server-side table
                                    //await tableClient.UpsertEntityAsync<GroupDetails>(teamsGroupDetails);
                                    // </create_object_add>
                                }
                                else
                                {
                                    await turnContext.SendActivityAsync(MessageFactory.Text("I am having some trouble getting teams profile. Please contact your Administrator."));

                                    throw new Exception("NOTE: Teams Group Details not found.");
                                }
                                break;

                            default: break;
                        }
                    }
                    catch (Exception)
                    {
                        await turnContext.SendActivityAsync(MessageFactory.Text("I am having some trouble getting your teams profile. Please contact your Administrator."));
                        throw;
                    }

                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error while storing teams user details in azure table storage -> {ex.Message}");
                throw new System.Exception("Error while storing teams user details in azure table storage");
            }
        }

        // create the table if not exists and return the table client
        private async Task<TableClient> GetTableClient(string tableName)
        {
            // <client_credentials> 
            // New instance of the TableClient class
            TableServiceClient tableServiceClient = new TableServiceClient(_configuration["StorageAccount:ConnectionString"]);
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
                if (string.IsNullOrEmpty(_configuration["StorageAccount:ConnectionString"]))
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

        // add entity data in azure table storage
        public async Task<bool> AddEntityAsync<T>(string tableName, T data) where T : class, ITableEntity, new()
        {
            try
            {
                if (string.IsNullOrEmpty(_configuration["StorageAccount:ConnectionString"]))
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
