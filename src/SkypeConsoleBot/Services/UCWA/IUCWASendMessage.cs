using Microsoft.ApplicationInsights;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.UCWA
{
    interface IUCWASendMessage
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task SendMessage(HttpClient httpClient, string msg, string url, TelemetryClient tc);
    }
}
