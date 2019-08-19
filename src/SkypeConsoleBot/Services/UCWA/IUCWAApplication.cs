using Microsoft.ApplicationInsights;
using SkypeConsoleBot.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.UCWA
{
    public interface IUCWAApplication
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task<string> CreateUcwaApps(HttpClient httpClient, OAuthTokenRoot authToken, string ucwaApplicationsRootUrl,
            UCWAMyApps ucwaAppsObject, TelemetryClient tc);
        Task<Boolean> DeleteUcwaApps(HttpClient httpClient, OAuthTokenRoot authToken, TelemetryClient tc);
    }
}