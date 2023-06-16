using AdaptiveCards;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
using System;
using Polly;
using Polly.CircuitBreaker;
using Microsoft.Bot.Builder.Teams;
using TeamsAIAssistant.Bot.Interfaces.Azure;
using Microsoft.Extensions.Configuration;
using TeamsAIAssistant.Data.Models.Teams;
using TeamsAIAssistant.Data.TableEntities.AzureTableEntity;

namespace TeamsAIAssistant.Bot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly string _appPassword;
        private readonly string _proactiveNotificationAPIKey;
        private readonly TableClient _tableClient;
        static readonly Random random = new Random();
        private readonly IAzureStorageHelper _storageHelper;
        private readonly IConfiguration _configuration;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, IAzureStorageHelper storageHelper)
        {
            _adapter = adapter;
            _storageHelper = storageHelper;
            _configuration = configuration;
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            _appPassword = configuration["MicrosoftAppPassword"];
            _proactiveNotificationAPIKey = configuration["ProactiveNotoficationAPIKey"];

            // <client_credentials> 
            // New instance of the TableClient class
            TableServiceClient tableServiceClient = new TableServiceClient(configuration["StorageAccount:ConnectionString"]);
            // </client_credentials>

            // <create_table>
            // New instance of TableClient class referencing the server-side table
            _tableClient = tableServiceClient.GetTableClient(
                tableName: configuration["StorageAccount:UserDetailsTableName"]
            );
        }

        [HttpPost("SendProactiveMessage")]
        public async Task<IActionResult> SendProactiveMessageAsync([FromBody] MessagePayloadDto messagePayload)
        {
            //Verify API Key
            string apiKey = Request.Headers["api-key"].ToString();

            if (string.IsNullOrEmpty(apiKey))
            {
                // Auth failed
                return new ContentResult()
                {
                    Content = "<html><body><h1>Authentication Failed. Key cannot be empty</h1></body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                };
            }
            else
            {
                if (apiKey.Equals(_proactiveNotificationAPIKey))
                {
                    if (messagePayload.Attachment == null)
                    {
                        return new ContentResult()
                        {
                            Content = "<html><body><h1>Message cannot be empty</h1></body></html>",
                            ContentType = "text/html",
                            StatusCode = (int)HttpStatusCode.BadRequest,
                        };
                    }

                    var credentials = new MicrosoftAppCredentials(_appId, _appPassword);

                    // send the proactive message to all users of UserDetails
                    var user = await _storageHelper.GetEntityAsync<UserDetailsEntity>(_configuration["StorageAccount:UserDetailsTableName"], messagePayload.TenantId, messagePayload.AadObjectId);

                    try
                    {
                        var activity = CreateBotActivity(messagePayload.SummaryText, JObject.Parse(messagePayload.Attachment));
                        await SendProactiveMessage(credentials, user.ServiceURL, user.ConversationID, activity);
                    }
                    catch (Exception ex)
                    {
                        // Let the caller know proactive messages have been sent
                        return new ContentResult()
                        {
                            Content = $"{{Error: {ex.Message}}}",
                            ContentType = "application/json",
                            StatusCode = (int)HttpStatusCode.InternalServerError,
                        };
                    }
                    

                    // Let the caller know proactive messages have been sent
                    return new ContentResult()
                    {
                        Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                        ContentType = "text/html",
                        StatusCode = (int)HttpStatusCode.OK,
                    };
                }
                else
                {
                    // Auth failed
                    return new ContentResult()
                    {
                        Content = "<html><body><h1>Authentication Failed.</h1></body></html>",
                        ContentType = "text/html",
                        StatusCode = (int)HttpStatusCode.Unauthorized,
                    };
                }
            }
        }

        private Activity CreateBotActivity(string message, JObject attachment)
        {
            var attachments = new List<Attachment>();
            Activity activity = (Activity)MessageFactory.Attachment(attachments);

            // add the card to the activity
            activity.Attachments = new List<Attachment>()
                    {
                        new Attachment()
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = attachment,
                        }
                    };

            activity.Summary = message; // Ensure that the summary text is populated so the toast notifications aren't generic text.
            activity.TeamsNotifyUser(); // Send the message into the activity feed.

            return activity;
        }

        private async Task SendProactiveMessage(MicrosoftAppCredentials credentials, string serviceUrl, string conversationId, Activity activity)
        {
            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl); // Required or the activity will be sent w/o auth headers.

            var connectorClient = new ConnectorClient(new Uri(serviceUrl), credentials);
            await SendWithRetries(async () =>
                    await connectorClient.Conversations.SendToConversationAsync(conversationId, activity));
        }

        private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            string message = this.Request.QueryString.Value;
            message = Uri.UnescapeDataString(message.Replace("?message=", String.Empty));
            // If you encounter permission-related errors when sending this message, see
            // https://aka.ms/BotTrustServiceUrl

            //decode the base 64 card and create a attachment response

            await turnContext.SendActivityAsync($"{message}");
        }

        // Create the send policy for Microsoft Teams
        // For more information about these policies
        // see: http://www.thepollyproject.org/
        static IAsyncPolicy CreatePolicy()
        {
            // Policy for handling the short-term transient throttling.
            // Retry on throttling, up to 3 times with a 2,4,8 second delay between with a 0-1s jitter.
            var transientRetryPolicy = Policy
                    .Handle<ErrorResponseException>(ex => ex.Message.Contains("429"))
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        (attempt) => TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(random.Next(0, 1000)));

            // Policy to avoid sending even more messages when the long-term throttling occurs.
            // After 5 messages fail to send, the circuit breaker trips & all subsequent calls will throw
            // a BrokenCircuitException for 10 minutes.
            // Note, in this application this cannot trip since it only sends one message at a time!
            // This is left in for completeness / demonstration purposes.
            var circuitBreakerPolicy = Policy
                .Handle<ErrorResponseException>(ex => ex.Message.Contains("429"))
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 5, TimeSpan.FromMinutes(10));

            // Policy to wait and retry when long-term throttling occurs. 
            // This will retry a single message up to 5 times with a 10 minute delay between each attempt.
            // Note, in this application this cannot trip since the circuit breaker above cannot trip.
            // This is left in for completeness / demonstration purposes.
            var outerRetryPolicy = Policy
                .Handle<BrokenCircuitException>()
                .WaitAndRetryAsync(
                    retryCount: 5,
                    (_) => TimeSpan.FromMinutes(10));

            // Combine all three policies so that it will first attempt to retry short-term throttling (inner-most)
            // After 15 (5 messages, 3 failures each) consecutive failed attempts to send a message it will trip the circuit breaker
            // which will fail all messages for the next ten minutes. It will attempt to send messages up to 5 times for a total
            // wait of 50 minutes before failing a message.
            return
                outerRetryPolicy.WrapAsync(
                    circuitBreakerPolicy.WrapAsync(
                        transientRetryPolicy));
        }

        static readonly IAsyncPolicy RetryPolicy = CreatePolicy();

        static Task SendWithRetries(Func<Task> callback)
        {
            return RetryPolicy.ExecuteAsync(callback);
        }
    }
}
