using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    public class UCWAMakeMeAvailable : IUCWAMakeMeAvailable
    {
        private readonly string _ucwaMakeMeAvailableRootUrl;
        private readonly OAuthTokenRoot _authToken;
        private readonly BotAttributes _botAttributes;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public UCWAMakeMeAvailable(HttpClient httpClient, OAuthTokenRoot authToken, String ucwaMakeMeAvailableRootUrl,
            BotAttributes botAttributes, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            _ucwaMakeMeAvailableRootUrl = ucwaMakeMeAvailableRootUrl;
            _authToken = authToken;
            _botAttributes = botAttributes;
            UCWAConfiguration._tc = tc;
        }

        public async Task<Boolean> MakeMeAvailable(HttpClient httpClient, OAuthTokenRoot authToken, String ucwaMakeMeAvailableRootUrl,
            BotAttributes botAttributes, TelemetryClient tc)
        {
            try
            {
                string makeMeAvailableResults = string.Empty;

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var makeMeAvailablePostData = JsonConvert.SerializeObject(botAttributes);
                //Successfull response is Http 204 No content
                var httpResponseMessage = await
                    httpClient.PostAsync(ucwaMakeMeAvailableRootUrl, new StringContent(makeMeAvailablePostData, Encoding.UTF8,
                    "application/json"));
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                if (result == String.Empty)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set the status of the bot as Online");
                tc.TrackTrace("Failed to set the status of the bot as Online");
                tc.TrackException(ex);
                throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                    " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
            }
        }
    }
}
