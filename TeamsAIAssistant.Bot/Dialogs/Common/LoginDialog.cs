using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.Configuration;

namespace TeamsAIAssistant.Bot.Dialogs.Common
{
    public class LoginDialog : LogoutDialog
    {
        private readonly IConfiguration _configuration;
        public LoginDialog(IConfiguration configuration)
            : base(nameof(LoginDialog), configuration["ConnectionName"])
        {
            _configuration = configuration;
            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = ConnectionName,
                    Text = "Please Sign In",
                    Title = "Sign In",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                    EndOnInvalidMessage = true
                }));

            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                PromptStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        //private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    // Get the token from the previous step. Note that we could also have gotten the
        //    // token directly from the prompt itself. There is an example of this in the next method.
        //    var tokenResponse = (TokenResponse)stepContext.Result;
        //    if (tokenResponse?.Token != null)
        //    {
        //        try
        //        {
        //            // Pull in the data from the Microsoft Graph.
        //            var client = new SimpleGraphClient(tokenResponse.Token, );
        //            var me = await client.GetMeAsync();
        //            var title = !string.IsNullOrEmpty(me.JobTitle) ?
        //                        me.JobTitle : "Unknown";

        //            await stepContext.Context.SendActivityAsync($"You're logged in as {me.DisplayName} ({me.UserPrincipalName}); you job title is: {title}");

        //            var photo = await client.GetPhotoAsync();
        //            var cardImage = new CardImage(photo);
        //            var card = new ThumbnailCard(images: new List<CardImage>() { cardImage });
        //            var reply = MessageFactory.Attachment(card.ToAttachment());

        //            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

        //            return await stepContext.PromptAsync(
        //                nameof(ConfirmPrompt),
        //                new PromptOptions { Prompt = MessageFactory.Text("Would you like to view your token?") },
        //                cancellationToken);
        //        }
        //        catch (Exception ex)
        //        {
        //            await Console.Out.WriteLineAsync(ex.Message);
        //            //_logger.LogError("Error occurred while processing your request.", ex.Message);

        //        }

        //    }
        //    else
        //    {
        //        //_logger.LogInformation("Response token is null or empty.");
        //    }

        //    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
        //    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        //}

        //private async Task<DialogTurnResult> DisplayTokenPhase1Async(
        //    WaterfallStepContext stepContext,
        //    CancellationToken cancellationToken)
        //{
        //    //_logger.LogInformation("DisplayTokenPhase1Async() method called.");

        //    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you."), cancellationToken);

        //    var result = (bool)stepContext.Result;
        //    if (result)
        //    {
        //        // Call the prompt again because we need the token. The reasons for this are:
        //        // 1. If the user is already logged in we do not need to store the token locally in the bot and worry
        //        // about refreshing it. We can always just call the prompt again to get the token.
        //        // 2. We never know how long it will take a user to respond. By the time the
        //        // user responds the token may have expired. The user would then be prompted to login again.
        //        //
        //        // There is no reason to store the token locally in the bot because we can always just call
        //        // the OAuth prompt to get the token or get a new token if needed.
        //        return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), cancellationToken: cancellationToken);
        //    }

        //    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        //}

        //private async Task<DialogTurnResult> DisplayTokenPhase2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{
        //    //_logger.LogInformation("DisplayTokenPhase2Async() method called.");

        //    var tokenResponse = (TokenResponse)stepContext.Result;
        //    if (tokenResponse != null)
        //    {
        //        await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is your token {tokenResponse.Token}"), cancellationToken);
        //    }

        //    return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        //}
    }
}
