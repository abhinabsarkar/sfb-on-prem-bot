using Microsoft.ApplicationInsights;
using SkypeConsoleBot.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.UCWA
{
    interface IUCWAMakeMeAvailable
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task<Boolean> MakeMeAvailable(HttpClient httpClient, OAuthTokenRoot authToken, String ucwaMakeMeAvailableRootUrl,
            BotAttributes botAttributes, TelemetryClient tc);
    }
}
