using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;
using TeamsAIAssistant.Bot.Dialogs.Common;
using TeamsAIAssistant.Bot.Interfaces.Azure;
using TeamsAIAssistant.Bot.Interfaces.Common;
using TeamsAIAssistant.Bot.Interfaces.Teams;
using TeamsAIAssistant.Bot.Services.Azure;
using TeamsAIAssistant.Bot.Services.Cards;
using TeamsAIAssistant.Bot.Services.Teams;
using TeamsAIAssistant.Data.Models.Common;
using TeamsAIAssistant.Data.Models.Teams;
using TeamsAIAssistant.Data.TableEntities.AzureTableEntity;
using TeamsAIAssistant.Data.TableEntities.AzureTableEntity.RequestTimeOff;

namespace TeamsAIAssistant.Bot.Dialogs.RequestTimeOff
{
    public class RequestTimeOffDialog : CancelAndHelpDialog
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;
        private readonly ITeamsHelper _teamsHelper;
        public RequestTimeOffDialog(IConfiguration configuration,IAzureStorageHelper azureStorageHelper, ITeamsHelper teamsHelper)
            : base(nameof(RequestTimeOffDialog))
        {
            _configuration = configuration;
            _azureStorageHelper = azureStorageHelper;
            _teamsHelper = teamsHelper;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new LoginDialog(_configuration));

            var waterfallSteps = new WaterfallStep[]
            {
                LoginStepAsync,
                GetTimeOffBalanceStepAsync,
                RequestTimeOffStepAsync,
                ActStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(LoginDialog), null, cancellationToken);
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

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> RequestTimeOffStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timeOffBalance = (TimeOffBalanceEntity)stepContext.Values["TimeOffBalance"];

            if (timeOffBalance == null)
            {
                var formText = stepContext.Context.Activity.Text;
                var formData = stepContext.Context.Activity.Value;

                if (formText != null)
                {
                    if (stepContext.Context.Activity.ReplyToId != null)
                        await stepContext.Context.DeleteActivityAsync(stepContext.Context.Activity.ReplyToId, cancellationToken);

                    if (formText.Equals("new-user-save-timeoff"))
                    {
                        JObject valueObject = (JObject)formData;
                        string newBalance = string.Empty;
                        string updateType = string.Empty;
                        string balanceRegularUpdate = string.Empty;

                        if ((string)valueObject["new-balance"] != null)
                            newBalance = (string)valueObject["new-balance"];

                        if ((string)valueObject["update-type"] != null)
                            updateType = (string)valueObject["update-type"];

                        if ((string)valueObject["balance-regular-update"] != null)
                            balanceRegularUpdate = (string)valueObject["balance-regular-update"];

                        if (string.IsNullOrEmpty(newBalance) && string.IsNullOrEmpty(updateType) && string.IsNullOrEmpty(balanceRegularUpdate))
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("No data is entered. Skipped."), cancellationToken);
                            return await stepContext.EndDialogAsync(null, cancellationToken);
                        }
                        else
                        {
                            timeOffBalance = new TimeOffBalanceEntity
                            {
                                PartitionKey = stepContext.Context.Activity.Conversation.TenantId,
                                RowKey = stepContext.Context.Activity.From.AadObjectId,
                                Balance = Convert.ToDouble(newBalance),
                                UpdateType = updateType,
                                BalanceRegularUpdate = Convert.ToDouble(balanceRegularUpdate)
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
                    }
                    else if (formText.Equals("cancel-timeoff"))
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("I skipped saving your time off initial data. No data is stored from the form."), cancellationToken);
                    }
                }
            }

            var getBalance = await _azureStorageHelper.GetEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], stepContext.Context.Activity.Conversation.TenantId, stepContext.Context.Activity.From.AadObjectId);

            if (getBalance == null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("I failed to get your current balance. Please try again later."), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }

            var paths = new string[] { ".", "Cards", "RequestTimeOff", "RequestTimeOff.json" };
            object data = new { CurrentBalance = getBalance.Balance, ManagerId = getBalance.ManagerId, ManagerYes = string.IsNullOrEmpty(getBalance.ManagerId) ? "false" : "true" };
            var attachment = CardsHelper.CreateAdaptiveCardWithData(paths, data);
            var reply = MessageFactory.Attachment(attachment);
            await stepContext.Context.SendActivityAsync(reply);

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var formText = stepContext.Context.Activity.Text;
            var formData = stepContext.Context.Activity.Value;

            if (formText != null)
            {
                if (stepContext.Context.Activity.ReplyToId != null)
                    await stepContext.Context.DeleteActivityAsync(stepContext.Context.Activity.ReplyToId, cancellationToken);

                if (formText.Equals("request-timeoff-submit"))
                {
                    double hoursRequested = 0;
                    string managerId = string.Empty;
                    bool useSameManager = false;
                    string reason = string.Empty;

                    JObject valueObject = (JObject)formData;

                    if ((string)valueObject["timeoff-requested"] != null)
                        hoursRequested = (double)valueObject["timeoff-requested"];

                    if ((string)valueObject["userId"] != null)
                        managerId = (string)valueObject["userId"];
                        
                    useSameManager = (bool)valueObject["same-manager"];

                    if ((string)valueObject["timeoff-reason"] != null)
                        reason = (string)valueObject["timeoff-reason"];

                    var currentBalance = await _azureStorageHelper.GetEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], stepContext.Context.Activity.Conversation.TenantId, stepContext.Context.Activity.From.AadObjectId);

                    if (currentBalance == null)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("I failed to get your current balance. Please try again later."), cancellationToken);
                        return await stepContext.EndDialogAsync(null, cancellationToken);
                    }
                    else
                    {
                        if (currentBalance.Balance < hoursRequested)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("You don't have enough balance to request this time off."), cancellationToken);
                            return await stepContext.EndDialogAsync(null, cancellationToken);
                        }

                        if (useSameManager)
                        {
                            currentBalance.ManagerId = managerId;
                        }
                        else
                        {
                            currentBalance.ManagerId = string.Empty;
                        }

                        await _azureStorageHelper.AddEntityAsync<TimeOffBalanceEntity>(_configuration["StorageAccount:TimeOffBalanceTableName"], currentBalance);

                        var requestRaised = await _azureStorageHelper.AddEntityAsync<TimeOffRequestsEntity>(_configuration["StorageAccount:TimeOffRequestTableName"], new TimeOffRequestsEntity
                        {
                            PartitionKey = stepContext.Context.Activity.Conversation.TenantId,
                            RowKey = Guid.NewGuid().ToString(),
                            ApproverId = currentBalance.ManagerId,
                            CurrentBalance = currentBalance.Balance,
                            HoursRequested = hoursRequested,
                            Reason = reason,
                            Status = "Pending",
                        });

                        if (requestRaised)
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your time off request has been submitted and sent for approval."), cancellationToken);
                            return await stepContext.EndDialogAsync(null, cancellationToken);
                            //var managerFound = await _azureStorageHelper.GetEntityAsync<UserDetailsEntity>(_configuration["StorageAccount:UserDetailsTableName"], stepContext.Context.Activity.Conversation.TenantId, managerId);
                            //bool appInstalled = false;
                            //if (managerFound == null)
                            //{
                            //    var appInstallationStatus = await _teamsHelper.InstalledAppsinPersonalScopeAsync(stepContext, managerId, cancellationToken);
                            //    if (appInstallationStatus)
                            //    {
                            //        appInstalled = true;
                            //    }
                            //}
                            //else
                            //{
                            //    appInstalled = true;
                            //}

                            //if (appInstalled)
                            //{
                            //    var paths = new string[] { ".", "Cards", "RequestTimeOff", "ApprovalMessage.json" };
                            //    var userDetails = await _azureStorageHelper.GetEntityAsync<UserDetailsEntity>(_configuration["StorageAccount:UserDetailsTableName"], stepContext.Context.Activity.Conversation.TenantId, stepContext.Context.Activity.From.AadObjectId);
                            //    object data = new { RequesterName = userDetails.UserDisplayName, HoursRequested = hoursRequested, Reason = reason, CurrentBalance = currentBalance.Balance, RequesterEmail = userDetails.UserEmail, RequesterId = userDetails.AadObjectID };
                            //    Attachment attachment = CardsHelper.CreateAdaptiveCardWithData(paths, data);

                            //    MessagePayloadDto messagePayloadDto = new MessagePayloadDto
                            //    {
                            //        Attachment = JsonConvert.SerializeObject(attachment),
                            //        AadObjectId = managerId,
                            //        TenantId = stepContext.Context.Activity.Conversation.TenantId,
                            //        SummaryText = "Time off request from " + userDetails.UserDisplayName,
                            //    };

                            //    var sendProactiveMessage = await _teamsHelper.SendProactiveMessageAsync(messagePayloadDto);
                            //    if (!sendProactiveMessage)
                            //    {
                            //        await stepContext.Context.SendActivityAsync(MessageFactory.Text("There was an error sending a proactive message to your manager."), cancellationToken);
                            //        return await stepContext.EndDialogAsync(null, cancellationToken);
                            //    }

                            //    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Your time off request has been submitted and sent for approval."), cancellationToken);
                            //    return await stepContext.EndDialogAsync(null, cancellationToken);
                            //}
                            //else
                            //{
                            //    await stepContext.Context.SendActivityAsync(MessageFactory.Text("We could not install the bot in your manager's teams personal scope. Ask your manager to manually install the bot."), cancellationToken);
                            //    return await stepContext.EndDialogAsync(null, cancellationToken);
                            //}
                        }
                        else
                        {
                            await stepContext.Context.SendActivityAsync(MessageFactory.Text("There was an error submitting your time off request."), cancellationToken);
                            return await stepContext.EndDialogAsync(null, cancellationToken);
                        }
                    }
                }
                else if (formText.Equals("skip-request-timeoff-submit"))
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("I skipped saving your time off request. No data is stored from the form."), cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
