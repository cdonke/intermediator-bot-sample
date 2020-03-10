using Intermediator.CommandHandling;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public MainDialog(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base(nameof(MainDialog))
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;

            var waterfallSteps = new WaterfallStep[]
            {
                WelcomeAsync,
                MakeAQuestion,
                TalkToHumanAsync,
                ThankYouAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> WelcomeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Command showOptionsCommand = new Command(Commands.ShowOptions);

            //HeroCard heroCard = new HeroCard()
            //{
            //    Title = "Hello!",
            //    Subtitle = "I am Intermediator Bot",
            //    Text = $"My purpose is to serve as a sample on how to implement the human hand-off. Click/tap the button below or type \"{new Command(Commands.ShowOptions).ToString()}\" to see all possible commands.",
            //    Buttons = new List<CardAction>()
            //        {
            //            new CardAction()
            //            {
            //                Title = "Show options",
            //                Value = showOptionsCommand.ToString(),
            //                Type = ActionTypes.ImBack
            //            }
            //        }
            //};

            //Activity replyActivity = stepContext.Context.Activity.CreateReply();
            //replyActivity.Attachments = new List<Attachment>() { heroCard.ToAttachment() };
            //await stepContext.Context.SendActivityAsync(replyActivity);
            //return await stepContext.NextAsync();

            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("What do you want to know?") }, cancellationToken);
        }

        private async Task<DialogTurnResult> MakeAQuestion(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var httpClient = _httpClientFactory.CreateClient();

            var qnaMaker = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = _configuration["QnAMaker:KnowledgebaseId"],
                EndpointKey = _configuration["QnAMaker:EndpointKey"],
                Host = _configuration["QnAMaker:Host"]
            },
            null,
            httpClient);

            var options = new QnAMakerOptions { Top = 1, ScoreThreshold = .7f };

            // The actual call to the QnA Maker service.
            var response = await qnaMaker.GetAnswersAsync(stepContext.Context, options);
            if (response != null && response.Length > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(response[0].Answer), cancellationToken);
                return await stepContext.NextAsync(true);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Could not find any answers."), cancellationToken);
                return await stepContext.NextAsync(false);
            }
        }

        private async Task<DialogTurnResult> TalkToHumanAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result == true)
                return await stepContext.NextAsync();

            return await stepContext.PromptAsync(nameof(ChoicePrompt), new PromptOptions
            {
                Prompt = MessageFactory.Text("Would you like to talk to a human?"),
                Choices = new List<Choice> { new Choice { Value = "Call me an human!" }, new Choice("No, nevermind!") }
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> ThankYouAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you!"), cancellationToken);
            return await stepContext.EndDialogAsync();
        }

    }
}
