using Microsoft.Bot.Builder;
using System.Threading;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.Bot
{
    interface IEchoBot
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken));
    }
}
