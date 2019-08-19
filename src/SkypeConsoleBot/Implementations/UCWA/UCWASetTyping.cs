using Microsoft.ApplicationInsights;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    class UCWASetTyping : IUCWASetTyping
    {
        private readonly OAuthTokenRoot _authToken;
        private readonly string _setTypingUrl;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public UCWASetTyping(HttpClient httpClient, OAuthTokenRoot authToken, String setTypingUrl, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            _setTypingUrl = setTypingUrl;
            _authToken = authToken;
            UCWAConfiguration._tc = tc;
        }

        public async Task SetTyping(HttpClient httpClient, OAuthTokenRoot authToken, String setTypingUrl, TelemetryClient tc)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Successfull response is Http 204 No content
                var httpResponseMessage = await httpClient.PostAsync(setTypingUrl, new StringContent(string.Empty));
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                if (result != String.Empty)
                {
                    tc.TrackTrace("Failed to set the typing status.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set the typing status.");
                tc.TrackTrace("Failed to set the typing status. More details can be found in the exception.");
                tc.TrackException(ex);
            }
        }
    }
}
