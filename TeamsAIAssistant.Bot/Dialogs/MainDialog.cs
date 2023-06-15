// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.18.1

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TeamsAIAssistant.Bot.Dialogs.RequestTimeOff;
using TeamsAIAssistant.Bot.Interfaces.Azure;
using TeamsAIAssistant.Bot.Interfaces.Cards;

namespace TeamsAIAssistant.Bot.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ICardsHelper _cardsHelper;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(ILogger<MainDialog> logger, IConfiguration configuration, IAzureStorageHelper azureStorageHelper, ICardsHelper cardsHelper)
            : base(nameof(MainDialog))
        {
            _logger = logger;
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _cardsHelper = cardsHelper;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new RequestTimeOffDialog(_configuration, _azureStorageHelper));

            var waterfallSteps = new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var options = (string)stepContext.Options;
            if (string.IsNullOrEmpty(options))
                await _cardsHelper.SendWelcomeCard(stepContext, cancellationToken);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var lastActivityId = stepContext.Context.Activity.ReplyToId;
            if (lastActivityId != null)
                await stepContext.Context.DeleteActivityAsync(lastActivityId, cancellationToken);

            var message = stepContext.Context.Activity.Text.ToLowerInvariant();
            switch (message)
            {
                case "request-timeoff":
                    return await stepContext.BeginDialogAsync(nameof(RequestTimeOffDialog), null, cancellationToken);

                case "menu":
                case "home":
                case "help":
                case "hello":
                case "hi":
                    await _cardsHelper.SendWelcomeCard(stepContext, cancellationToken);
                    break;

                default:
                    //if (!await GetFAQResponse(stepContext, cancellationToken))
                    //    return await stepContext.BeginDialogAsync(nameof(SearchDialog), null, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
