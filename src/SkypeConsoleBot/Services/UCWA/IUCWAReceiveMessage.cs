using Microsoft.ApplicationInsights;
using SkypeConsoleBot.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.UCWA
{
    interface IUCWAReceiveMessage
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task GetMessage(HttpClient httpClient, TelemetryClient tc, OAuthTokenRoot authToken, bool isNextMsg = false);
    }
}
