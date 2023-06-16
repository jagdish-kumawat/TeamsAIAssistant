using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TeamsAIAssistant.Data.TableEntities.AzureTableEntity.RequestTimeOff;
using TeamsAIAssistant.Functions.Interfaces;

namespace TeamsAIAssistant.Functions
{
    public class UpdateTimeOffBalance
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IAzureStorageHelper _azureStorageHelper;

        public UpdateTimeOffBalance(ILoggerFactory loggerFactory, IAzureStorageHelper azureStorageHelper, IConfiguration configuration)
        {
            _logger = loggerFactory.CreateLogger<UpdateTimeOffBalance>();
            _azureStorageHelper = azureStorageHelper;
            _configuration = configuration;
        }

        [Function("UpdateTimeOffBalance")]
        public async Task Run([TimerTrigger("0 0 9 1 * *")] MyInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var timeOffBalances = await _azureStorageHelper.GetAllEntitiesAsync<TimeOffBalanceEntity>(_configuration["TimeOffBalanceTableName"]);

            if (timeOffBalances == null)
            {
                _logger.LogInformation($"No time off balances found.");
                return;
            }
            else
            {
                DateTime currentDate = DateTime.Now;
                foreach (var timeOffBalance in timeOffBalances)
                {
                    switch (timeOffBalance.UpdateType)
                    {
                        case "Monthly":
                            timeOffBalance.Balance += timeOffBalance.BalanceRegularUpdate;
                            await _azureStorageHelper.AddEntityAsync<TimeOffBalanceEntity>(_configuration["TimeOffBalanceTableName"], timeOffBalance);
                            break;

                        case "Yearly":
                            if (currentDate.Month == 4 && currentDate.Day == 1)
                            {
                                timeOffBalance.Balance += timeOffBalance.BalanceRegularUpdate;
                                await _azureStorageHelper.AddEntityAsync<TimeOffBalanceEntity>(_configuration["TimeOffBalanceTableName"], timeOffBalance);
                            }
                            break;

                        case "Quarterly":
                            if (currentDate.Day == 1 && (currentDate.Month == 1 || currentDate.Month == 4 || currentDate.Month == 7 || currentDate.Month == 10))
                            {
                                timeOffBalance.Balance += timeOffBalance.BalanceRegularUpdate;
                                await _azureStorageHelper.AddEntityAsync<TimeOffBalanceEntity>(_configuration["TimeOffBalanceTableName"], timeOffBalance);
                            }
                            break;

                        default:
                            break;
                    }
                }
            }

            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }

    public class MyInfo
    {
        public MyScheduleStatus ScheduleStatus { get; set; }

        public bool IsPastDue { get; set; }
    }

    public class MyScheduleStatus
    {
        public DateTime Last { get; set; }

        public DateTime Next { get; set; }

        public DateTime LastUpdated { get; set; }
    }
}
