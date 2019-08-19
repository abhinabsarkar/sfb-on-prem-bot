using Microsoft.ApplicationInsights;
using SkypeConsoleBot.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.UCWA
{
    public interface ILyncAuthentication
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }

        Task<OAuthTokenRoot> GetOAuthToken(HttpClient httpClient, string userName, string password, string domain,
            string lyncOAuthUri, TelemetryClient tc);
    }
}
