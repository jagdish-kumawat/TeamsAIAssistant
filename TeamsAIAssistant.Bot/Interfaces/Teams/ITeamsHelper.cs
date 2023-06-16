using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using System.Threading;
using System.Threading.Tasks;
using TeamsAIAssistant.Data.Models.Teams;

namespace TeamsAIAssistant.Bot.Interfaces.Teams
{
    public interface ITeamsHelper
    {
        Task<TeamsChannelAccount> GetTeamsMemberAsync<T>(T turnContext, CancellationToken cancellationToken) where T : ITurnContext;
        Task<bool> InstalledAppsinPersonalScopeAsync(WaterfallStepContext stepContext, string aadObjectId, CancellationToken cancellationToken);
        Task<bool> SendProactiveMessageAsync(MessagePayloadDto messagePayloadDto);
    }
}
