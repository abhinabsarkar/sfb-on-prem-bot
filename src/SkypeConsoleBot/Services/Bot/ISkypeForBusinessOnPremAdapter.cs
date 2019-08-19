using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.Bot
{
    interface ISkypeForBusinessOnPremAdapter
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task ProcessActivityAsync(string fromId, string conversationId, string sendMessageUrl, string msg,
            BotCallbackHandler callback = null);
    }
}
