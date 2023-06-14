using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Data.AzureTableEntity
{
    public class UserDetailsEntity : CommonEntity
    {
        public string UserDisplayName { get; set; } = default!;

        public string UserID { get; set; }

        public string ConversationID { get; set; }

        public string ServiceURL { get; set; }

        public string AadObjectID { get; set; }

        public string TenantID { get; set; }

        public string UserRole { get; set; }

        public string UserEmail { get; set; }
    }
}
