﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsAIAssistant.Data.TableEntities.AzureTableEntity;

namespace TeamsAIAssistant.Data.TableEntities.AzureTableEntity.RequestTimeOff
{
    public class TimeOffRequestsEntity : CommonEntity
    {
        public double HoursRequested { get; set; }
        public double CurrentBalance { get; set; }
        public string Reason { get; set; }
        public string ApproverId { get; set; }
        public string Status { get; set; }
    }
}
