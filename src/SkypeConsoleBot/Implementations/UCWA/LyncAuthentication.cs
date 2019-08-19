using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    public class LyncAuthentication : ILyncAuthentication
    {
        private readonly string _userName;
        private readonly string _password;
        private readonly string _domain;
        private readonly string _lyncOAuthUri;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public LyncAuthentication(HttpClient httpClient, string userName, string password, string domain,
            string lyncOAuthUrl, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            _userName = userName;
            _password = password;
            _domain = domain;
            _lyncOAuthUri = lyncOAuthUrl;
            UCWAConfiguration._tc = tc;
        }
        public async Task<OAuthTokenRoot> GetOAuthToken(HttpClient httpClient, string userName, string password, string domain,
            string lyncOAuthUri, TelemetryClient tc)
        {

            //Console.WriteLine("Write Message called from AppInsightsLogger - ");
            //return Task.FromResult(0);
            try
            {
                OAuthTokenRoot oAuthTokenResult = new OAuthTokenRoot();
                httpClient.DefaultRequestHeaders.Clear();
                var authDic = new Dictionary<string, string>();
                authDic.Add("grant_type", "password");
                authDic.Add("username", domain + "\\" + userName);
                authDic.Add("password", password);
                HttpResponseMessage httpResponseMessage = await httpClient.PostAsync(lyncOAuthUri,
                    new FormUrlEncodedContent(authDic));               
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    var getOAuthTokenResult = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    // Add reference to Newtonsoft.Json
                    JsonConvert.PopulateObject(getOAuthTokenResult, oAuthTokenResult);
                    tc.TrackTrace("Authentication token for the Skype Id received successfully.");
                    Console.WriteLine("Authentication token for the Skype Id received successfully.");
                }
                else
                {
                    tc.TrackTrace("Failed to receive authentication token for the Skype Id. " + httpResponseMessage.ToString());
                    tc.TrackEvent("GetOAuthToken-Failed");
                    Console.WriteLine("Unable to get the authentication token.");
                }
                // oAuthTokenResult;
                return oAuthTokenResult;
            }
            catch (Exception ex)
            {
                tc.TrackException(ex);
                tc.TrackEvent("GetOAuthToken-Exception");
                Console.WriteLine("Failed to acquire authentication token for the Skype Id.");
                throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                    " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
            }
        }
    }
}
