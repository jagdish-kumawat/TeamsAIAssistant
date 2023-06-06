using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using System;
using TeamsAIAssistant.Bot.Interfaces.Teams;

namespace TeamsAIAssistant.Bot.Services.Teams
{
    public class TeamsHelper : ITeamsHelper
    {
        public async Task<TeamsChannelAccount> GetTeamsMemberAsync<T>(T turnContext, CancellationToken cancellationToken) where T : ITurnContext
        {
            var member = new TeamsChannelAccount();

            try
            {
                member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
                return member;
            }
            catch (ErrorResponseException e)
            {
                if (e.Body.Error.Code.Equals("MemberNotFoundInConversation", StringComparison.OrdinalIgnoreCase))
                {
                    await turnContext.SendActivityAsync("Member not found.");
                    return null;
                }
                else
                {
                    throw e;
                }
            }
        }
    }
}
