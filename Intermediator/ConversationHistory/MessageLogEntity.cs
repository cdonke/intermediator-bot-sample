using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace Intermediator.ConversationHistory
{
    public class MessageLogEntity : TableEntity
    {
        public string Body
        {
            get;
            set;
        }
    }
}
