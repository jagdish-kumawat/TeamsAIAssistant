using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsAIAssistant.Data.TableEntities.AzureTableEntity;

namespace TeamsAIAssistant.Data.TableEntities.AzureTableEntity.RequestTimeOff
{
    public class TimeOffBalanceEntity : CommonEntity
    {
        public double Balance { get; set; }
        public string UpdateType { get; set; }
        public double BalanceRegularUpdate { get; set; }
        public string? ManagerId { get; set; }
    }
}
