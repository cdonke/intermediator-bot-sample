using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.QnA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IntermediatorBotSample.Services
{
    public interface IBotServices
    {
        LuisRecognizer Dispatch { get; }
        LuisRecognizer Luis { get; }
        QnAMaker QnA { get; }
    }
}
