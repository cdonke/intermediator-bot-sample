using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Middleware
{
    public class HandoffMiddleware : Intermediator.Middleware.HandoffMiddlewareBase
    {
        public HandoffMiddleware(IConfiguration configuration) : base(configuration) { }

        public override Task<bool> ShouldHandleToHumanAsync(ITurnContext turnContext, Activity activity)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(activity.Text) && activity.Text.ToLower().Contains("human"));
        }
    }
}
