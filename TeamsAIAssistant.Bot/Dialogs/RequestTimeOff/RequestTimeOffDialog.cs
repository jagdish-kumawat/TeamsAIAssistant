using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;
using TeamsAIAssistant.Bot.Interfaces.Azure;
using TeamsAIAssistant.Bot.Services.Azure;
using TeamsAIAssistant.Bot.Services.Cards;
using TeamsAIAssistant.Data.AzureTableEntity.RequestTimeOff;

namespace TeamsAIAssistant.Bot.Dialogs.RequestTimeOff
{
    public class RequestTimeOffDialog : CancelAndHelpDialog
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly string RequestTimeOffDialogID = "RequestTimeOffDlg";
        public RequestTimeOffDialog(IConfiguration configuration,IAzureStorageHelper azureStorageHelper)
            : base(nameof(RequestTimeOffDialog))
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new TextPrompt(RequestTimeOffDialogID, RequestTimeOffValidatorAsync));

            var waterfallSteps = new WaterfallStep[]
            {
                GetTimeOffBalanceStepAsync,
                RequestTimeOffStepAsync,
                ActStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> GetTimeOffBalanceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // check if user has time off balance entry in table storage and if not, create one
            var timeOffBalance = await _azureStorageHelper.GetEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], stepContext.Context.Activity.Conversation.TenantId, stepContext.Context.Activity.From.AadObjectId);

            stepContext.Values["TimeOffBalance"] = timeOffBalance;

            if (timeOffBalance == null)
            {
                var paths = new string[] { ".", "Cards", "RequestTimeOff", "NewUserTimeOffCollection.json" };
                var attachment = CardsHelper.CreateAdaptiveCard(paths);
                var reply = MessageFactory.Attachment(attachment);
                await stepContext.Context.SendActivityAsync(reply);
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { }, cancellationToken);
            }
            else
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your current time off balance is " + timeOffBalance.Balance.ToString("0.00") + " hours."), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> RequestTimeOffStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timeOffBalance = (TimeOffBalanceEntity)stepContext.Values["TimeOffBalance"];

            if (timeOffBalance == null)
            {
                string balance = (string)stepContext.Result;
                timeOffBalance = new TimeOffBalanceEntity
                {
                    PartitionKey = stepContext.Context.Activity.Conversation.TenantId,
                    RowKey = stepContext.Context.Activity.From.AadObjectId,
                    Balance = Convert.ToDouble(balance)
                };

                bool entityAdded = await _azureStorageHelper.AddEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], timeOffBalance);
                if (entityAdded)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your time off balance has been saved."), cancellationToken);
                    stepContext.Values["TimeOffBalance"] = await _azureStorageHelper.GetEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], stepContext.Context.Activity.Conversation.TenantId, stepContext.Context.Activity.From.AadObjectId);
                }  
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("There was an error saving your time off balance."), cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                } 
            }

            return await stepContext.PromptAsync(RequestTimeOffDialogID, new PromptOptions { Prompt = MessageFactory.Text("Please enter the number of hours you would like to request off.") }, cancellationToken);
        }

        private async Task<bool> RequestTimeOffValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            string hoursRequested = promptContext.Recognized.Value;
            double hoursRequestedDouble;
            if (double.TryParse(hoursRequested, out hoursRequestedDouble))
            {
                if (hoursRequestedDouble > 0)
                {
                    var currentBalance = await _azureStorageHelper.GetEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], promptContext.Context.Activity.Conversation.TenantId, promptContext.Context.Activity.From.AadObjectId);
                    if (currentBalance.Balance < hoursRequestedDouble)
                    {
                        await promptContext.Context.SendActivityAsync(MessageFactory.Text("You do not have enough time off balance to request " + hoursRequested + " hours off. Please request for time off less than " + currentBalance.Balance.ToString("0.00") + " hours."), cancellationToken);
                        return await Task.FromResult(false);
                    }
                    return await Task.FromResult(true);
                }
                else
                {
                    await promptContext.Context.SendActivityAsync(MessageFactory.Text("Please enter a number greater than 0."), cancellationToken);
                    return await Task.FromResult(false);
                }
            }
            else
            {
                await promptContext.Context.SendActivityAsync(MessageFactory.Text("Please enter a valid number."), cancellationToken);
                return await Task.FromResult(false);
            }
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string hoursRequested = (string)stepContext.Result;
            var timeOffBalance = (TimeOffBalanceEntity)stepContext.Values["TimeOffBalance"];

            timeOffBalance.Balance = timeOffBalance.Balance - Convert.ToDouble(hoursRequested);
            bool entityAdded = await _azureStorageHelper.AddEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], timeOffBalance);

            if (entityAdded)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your time off balance has been updated. Your new balance is {timeOffBalance.Balance.ToString("0.00")} hours."), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("There was an error updating your time off balance."), cancellationToken);
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
