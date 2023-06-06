using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TeamsAIAssistant.Bot.Interfaces.Azure;

namespace TeamsAIAssistant.Bot.Services.Azure
{
    public class AzureStorageHelper : IAzureStorageHelper
    {
        private readonly ILogger<AzureStorageHelper> _logger;
        private readonly IConfiguration _configuration;
        public AzureStorageHelper(IConfiguration configuration, ILogger<AzureStorageHelper> logger) 
        { 
            _configuration = configuration;
            _logger = logger;
        }

        // delete the teams user details from azure table storage
        public Task DeleteTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        // store the teams user details in azure table storage
        public async Task StoreTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            try
            {

            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Error while storing teams user details in azure table storage -> {ex.Message}");
                throw new System.Exception("Error while storing teams user details in azure table storage");
            }
        }
    }
}
