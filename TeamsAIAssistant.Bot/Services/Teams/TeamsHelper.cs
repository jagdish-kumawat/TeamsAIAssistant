using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Schema;
using System.Threading.Tasks;
using System.Threading;
using System;
using TeamsAIAssistant.Bot.Interfaces.Teams;
using Microsoft.Extensions.Configuration;
using TeamsAIAssistant.Data.Models.Teams;
using Microsoft.Bot.Builder.Dialogs;
using TeamsAIAssistant.Bot.Interfaces.Common;
using TeamsAIAssistant.Data.Models.Common;
using System.Net.Http;

namespace TeamsAIAssistant.Bot.Services.Teams
{
    public class TeamsHelper : ITeamsHelper
    {
        private readonly IConfiguration _configuration;
        private readonly IApiHelper<ApiResponseDto> _apiHelper;
        private readonly ProactiveAppIntallationHelper _helper = new ProactiveAppIntallationHelper();
        public TeamsHelper(IConfiguration configuration, IApiHelper<ApiResponseDto> apiHelper)
        {
            _configuration = configuration;
            _apiHelper = apiHelper;
        }

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

        public async Task<bool> InstalledAppsinPersonalScopeAsync(WaterfallStepContext stepContext, string aadObjectId, CancellationToken cancellationToken)
        {
            try
            {
                await _helper.AppinstallationforPersonal(aadObjectId, stepContext.Context.Activity.Conversation.TenantId, _configuration["MicrosoftAppId"], _configuration["MicrosoftAppPassword"], _configuration["AppCatalogTeamAppId"]);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SendProactiveMessageAsync(MessagePayloadDto messagePayloadDto)
        {
            try
            {
                var headers = new System.Collections.Generic.Dictionary<string, string>();
                headers.Add("api-key", _configuration["ProactiveNotoficationAPIKey"]);
                await _apiHelper.MakeApiCallAsync<ApiResponseDto>($"{_configuration["BaseAddress"]}/api/notify/SendProactiveMessage", HttpMethod.Post, messagePayloadDto, headers);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
