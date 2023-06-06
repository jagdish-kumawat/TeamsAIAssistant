using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using System.Threading;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Bot.Interfaces.Teams
{
    public interface ITeamsHelper
    {
        Task<TeamsChannelAccount> GetTeamsMemberAsync<T>(T turnContext, CancellationToken cancellationToken) where T : ITurnContext;
    }
}
