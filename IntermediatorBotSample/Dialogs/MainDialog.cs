using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        public MainDialog() : base(nameof(MainDialog))
        {
            var waterfallSteps = new WaterfallStep[]
            {
                TalkToHumanAsync,
                ThankYouAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }


        private async Task<DialogTurnResult> TalkToHumanAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to talk to a human?")
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ThankYouAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync();
        }

    }
}
