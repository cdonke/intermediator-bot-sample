using Microsoft.Bot.Schema;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.Logging;

namespace Intermediator.ConversationHistory
{
    public class MessageLogs
    {
        private const string MessageLogsTableName = "MessageLogs";
        private const string PartitionKey = "IntermediatorBot";

        private CloudTable _messageLogsTable;
        private readonly IList<MessageLog> _inMemoryMessageLogs;
        private readonly ILogger _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="connectionString">The connection string for Azure Table Storage.</param>
        public MessageLogs(string connectionString, Underscore.Bot.MessageRouting.Logging.ILogger logger)
        {
            _logger = logger;

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError("WARNING!!! No connection string - storing message logs in memory");
                _inMemoryMessageLogs = new List<MessageLog>();
            }
            else
            {
                _logger.LogInformation("Using Azure Table Storage for storing message logs");
                _messageLogsTable = AzureStorageHelper.GetTable(connectionString, MessageLogsTableName);
                MakeSureConversationHistoryTableExistsAsync().Wait();
            }

        }

        /// <returns>All the message logs.</returns>
        public IList<MessageLog> GetMessageLogs()
        {
            if (_messageLogsTable != null)
            {
                var entities = GetAllEntitiesFromTable(_messageLogsTable).Result;
                return GetAllMessageLogsFromEntities(entities);
            }

            return _inMemoryMessageLogs;
        }

        /// <summary>
        /// Finds the message log associated with the given user.
        /// </summary>
        /// <param name="user">The user whose message log to find.</param>
        /// <returns>The message log of the user or null, if not found.</returns>
        public MessageLog GetMessageLog(ConversationReference user)
        {
            var messageLogs = GetMessageLogs();

            foreach (MessageLog messageLog in messageLogs)
            {
                if (RoutingDataManager.Match(user, messageLog.User))
                {
                    return messageLog;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds a message (an activity) to the log associated with the given user.
        /// </summary>
        /// <param name="activity">The activity to add.</param>
        /// <param name="user">The user associated with the message.</param>
        public async Task AddMessageLog(Microsoft.Bot.Schema.Activity activity, ConversationReference user, bool replace = false)
        {
            if (_messageLogsTable != null)
            {
                // Add to AzureTable
                var body = new MessageLog(user);
                body.AddMessage(activity);

                var msg = new MessageLogEntity
                {
                    PartitionKey = PartitionKey,
                    RowKey = activity.Conversation.Id,
                    Body = body.ToJson()
                };

                if (!replace)
                    await AzureStorageHelper.InsertAsync(_messageLogsTable, msg);
                else
                    await AzureStorageHelper.ReplaceAsync(_messageLogsTable, msg);

            }

            else
            {
                // Add to InMemory storage
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user">The user whose message log to delete.</param>
        public void DeleteMessageLog(ConversationReference user)
        {
            // TODO
        }

        /// <summary>
        /// Makes sure the cloud table for storing message logs exists.
        /// A table is created, if one doesn't exist.
        /// </summary>
        private async Task MakeSureConversationHistoryTableExistsAsync()
        {
            try
            {
                await _messageLogsTable.CreateIfNotExistsAsync();
                _logger.LogInformation($"Table '{_messageLogsTable.Name}' created or did already exist");
            }
            catch (StorageException e)
            {
                _logger.LogError($"Failed to create table '{_messageLogsTable.Name}' (perhaps it already exists): {e.Message}");
            }
        }

        private async Task<IList<MessageLogEntity>> GetAllEntitiesFromTable(CloudTable table)
        {
            var query = new TableQuery<MessageLogEntity>()
                .Where(TableQuery.GenerateFilterCondition(
                    "PartitionKey", QueryComparisons.Equal, PartitionKey));

            return await table.ExecuteTableQueryAsync(query);
        }

        private IList<MessageLog> GetAllMessageLogsFromEntities(IList<MessageLogEntity> entities)
        {
            IList<MessageLog> messageLogs = new List<MessageLog>();

            foreach (var entity in entities)
            {
                var messageLog = JsonConvert.DeserializeObject<MessageLog>(entity.Body);
                messageLogs.Add(messageLog);
            }

            return messageLogs;
        }
    }
}