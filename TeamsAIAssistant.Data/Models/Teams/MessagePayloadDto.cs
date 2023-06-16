using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace TeamsAIAssistant.Data.Models.Teams
{
    public class MessagePayloadDto
    {
        public string TenantId { get; set; }
        public string AadObjectId { get; set; }
        public string Attachment { get; set; }
        public string SummaryText { get; set; }
    }
}
