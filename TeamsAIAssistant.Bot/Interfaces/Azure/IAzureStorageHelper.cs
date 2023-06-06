using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using System.Threading;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Bot.Interfaces.Azure
{
    public interface IAzureStorageHelper
    {
        Task DeleteTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken);
        Task StoreTeamsUserDetailsAsync(TeamsChannelAccount teamsMember, ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken);
    }
}
