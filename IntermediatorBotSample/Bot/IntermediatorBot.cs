using Intermediator.CommandHandling;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Bot
{
    public class IntermediatorBot<T> : ActivityHandler
        where T : Dialog
    {
        private const string SampleUrl = "https://github.com/tompaana/intermediator-bot-sample";

        protected readonly Dialog Dialog;
        protected readonly BotState ConversationState;

        public IntermediatorBot(ConversationState conversationState, T dialog)
        {
            Dialog = dialog;
            ConversationState = conversationState;
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }
        
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>(nameof(DialogState)), cancellationToken);
        }
    }
}
