using Intermediator.CommandHandling;
using IntermediatorBotSample.Services;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IConfiguration _configuration;
        private readonly IBotServices _botServices;
        private readonly ILogger<MainDialog> _logger;

        public MainDialog(IConfiguration configuration, Services.IBotServices botServices, ILogger<MainDialog> logger) : base(nameof(MainDialog))
        {
            _configuration = configuration;
            _botServices = botServices;
            _logger = logger;

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
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);

            // Top intent tell us which cognitive service to use.
            var topIntent = recognizerResult.GetTopScoringIntent();

            // Next, we call the dispatcher with the top intent.
            return await DispatchToTopIntentAsync(stepContext, topIntent.intent, recognizerResult, cancellationToken);
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


        private async Task<DialogTurnResult> DispatchToTopIntentAsync(WaterfallStepContext stepContext, string intent, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "l_Airline_Reservation":
                    return await ProcessAirlineReservationAsync(stepContext, recognizerResult.Properties["luisResult"] as LuisResult, cancellationToken);
                case "q_Faq":
                    return await ProcessFaqAsync(stepContext, cancellationToken);
                default:
                    _logger.LogInformation($"Dispatch unrecognized intent: {intent}.");
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Dispatch unrecognized intent: {intent}."), cancellationToken);
                    return await stepContext.NextAsync(false);
            }
        }

        private async Task<DialogTurnResult> ProcessFaqAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessFaqAsync");

            var results = await _botServices.QnA.GetAnswersAsync(stepContext.Context);
            if (results.Any())
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
                return await stepContext.NextAsync(true);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer in the Q and A system."), cancellationToken);
                return await stepContext.NextAsync(false);
            }
        }

        private async Task<DialogTurnResult> ProcessAirlineReservationAsync(WaterfallStepContext stepContext, LuisResult luisResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessAirlineReservationAsync");

            var recognizerResult = await _botServices.Luis.RecognizeAsync(stepContext.Context, cancellationToken);
            var result = recognizerResult.Properties["luisResult"] as LuisResult;
            var topIntent = result.TopScoringIntent.Intent;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Airline Reservation top intent {topIntent}."), cancellationToken);
            if (result.Entities.Count > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Airline Reservation entities were found in the message:\n\n{string.Join("\n\n", result.Entities.Select(i => $"{i.Type}={i.Entity}"))}"), cancellationToken);
            }
            if (result.CompositeEntities?.Count > 0)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Airline Reservation composite entities were found in the message:\n\n{string.Join("\n\n", result.CompositeEntities.Select(i => $"{i.ParentType}={i.Value}"))}"), cancellationToken);
            }

            return await stepContext.NextAsync(true);
        }
    }
}
