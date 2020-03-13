using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Underscore.Bot.MessageRouting;
using Underscore.Bot.MessageRouting.DataStore;
using Underscore.Bot.MessageRouting.DataStore.Azure;
using Underscore.Bot.MessageRouting.DataStore.Local;

namespace Intermediator.Middleware
{
    public class TimeoutMiddleware : IMiddleware
    {
        private const string KeyAzureTableStorageConnectionString = "AzureTableStorageConnectionString";
        private const string KeyConversationTimeout = "BotConversationTimeout";

        private readonly ILogger<TimeoutMiddleware> _logger;
        private readonly TimeSpan _timeout;
        private readonly MessageRouter _messageRouter;
        private readonly IRoutingDataStore _routingDataStore;
        private readonly IMessageActivity _endOfConversationActivity;

        public TimeoutMiddleware(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TimeoutMiddleware>();

            string connectionString = configuration[KeyAzureTableStorageConnectionString];
            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogError($"WARNING!!! No connection string found - using {nameof(InMemoryRoutingDataStore)}");
                _routingDataStore = new InMemoryRoutingDataStore();
            }
            else
            {
                _logger.LogDebug($"Found a connection string - using {nameof(AzureTableRoutingDataStore)}");
                _routingDataStore = new AzureTableRoutingDataStore(connectionString, new Underscore.Bot.MessageRouting.Logging.ConsoleLogger(loggerFactory.CreateLogger<AzureTableRoutingDataStore>()));
            }

            _timeout = TimeSpan.FromSeconds(Double.Parse(configuration[KeyConversationTimeout] ?? "3600"));
            _logger.LogInformation($"Bot Conversation Timeout set to {_timeout.ToString()}");

            _messageRouter = new MessageRouter(
               _routingDataStore,
               new MicrosoftAppCredentials(configuration["MicrosoftAppId"], configuration["MicrosoftAppPassword"]),
                logger: new Underscore.Bot.MessageRouting.Logging.ConsoleLogger(loggerFactory.CreateLogger<MessageRouter>()));

            _endOfConversationActivity = Activity.CreateMessageActivity();
            _endOfConversationActivity.Type = ActivityTypes.EndOfConversation;
        }

        public async Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default)
        {
            Activity activity = context.Activity;

            if (activity.Type is ActivityTypes.Message)
            {
                var connections = _routingDataStore.GetConnections();
                var elapsedConnections = connections.Where(q => DateTime.UtcNow - q.TimeSinceLastActivity >= _timeout);
#if DEBUG
                _logger.LogDebug($"{connections.Count} connections. {elapsedConnections.Count()} inactive.");
#endif
                
                foreach (var conn in elapsedConnections)
                {
                    _logger.LogDebug($"Disconnecting {conn}");

                    if (!conn.ConversationReference1.User.Id.Equals(activity.From.Id) || !conn.ConversationReference2.User.Id.Equals(activity.From.Id))
                    {
                        await Disconnect(conn.ConversationReference1);
                        await Disconnect(conn.ConversationReference2);
                    }
                }
            }

            await next(cancellationToken).ConfigureAwait(false);
        }

        private async Task Disconnect(ConversationReference recipient)
        {
            await _messageRouter.SendMessageAsync(recipient, $"Conversation disconnected for inactivity of more than {_timeout.TotalMinutes} minutes.");
            await _messageRouter.SendMessageAsync(recipient, _endOfConversationActivity);
            _messageRouter.Disconnect(recipient);
        }
    }
}
