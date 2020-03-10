using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Services
{
    public class DispatcherService : IBotServices
    {
        public DispatcherService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            var httpClient = httpClientFactory.CreateClient();


            Dispatch = new LuisRecognizer(new LuisRecognizerOptionsV2(new LuisApplication(
                configuration["Dispatcher:LuisAppId"],
                configuration["Dispatcher:LuisAPIKey"],
                configuration["Dispatcher:LuisAPIHostName"])
                )
            {
                IncludeAPIResults = true,
                PredictionOptions = new LuisPredictionOptions()
                {
                    IncludeAllIntents = true,
                    IncludeInstanceData = true
                }
            });


            QnA = new QnAMaker(new QnAMakerEndpoint
            {
                KnowledgeBaseId = configuration["QnAMaker:KnowledgebaseId"],
                EndpointKey = configuration["QnAMaker:EndpointKey"],
                Host = configuration["QnAMaker:Host"]
            },
            null,
            httpClient);

            Luis = new LuisRecognizer(new LuisRecognizerOptionsV2(new LuisApplication(
                configuration["LUIS:LuisAppId"],
                configuration["LUIS:LuisAPIKey"],
                configuration["LUIS:LuisAPIHostName"])
                )
            {
                IncludeAPIResults = true,
                PredictionOptions = new LuisPredictionOptions()
                {
                    IncludeAllIntents = true,
                    IncludeInstanceData = true
                }
            });
        }

        public LuisRecognizer Dispatch { get; private set; }
        public LuisRecognizer Luis { get; private set; }
        public QnAMaker QnA { get; private set; }
    }
}
