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
    public class UCWAApplication : IUCWAApplication
    {
        private readonly string _ucwaApplicationsRootUrl;
        private readonly OAuthTokenRoot _authToken;
        private readonly UCWAMyApps _ucwaAppsObject;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public UCWAApplication(HttpClient httpClient, OAuthTokenRoot authToken, string ucwaApplicationsRootUrl,
            UCWAMyApps ucwaAppsObject, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            _ucwaApplicationsRootUrl = ucwaApplicationsRootUrl;
            _authToken = authToken;
            _ucwaAppsObject = ucwaAppsObject;            
            UCWAConfiguration._tc = tc;
        }
        public async Task<string> CreateUcwaApps(HttpClient httpClient, OAuthTokenRoot authToken, string ucwaApplicationsRootUrl,
            UCWAMyApps ucwaAppsObject, TelemetryClient tc)
        {
            try
            {
                string createUcwaAppsResults = string.Empty;

                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var createUcwaPostData = JsonConvert.SerializeObject(ucwaAppsObject);
                var httpResponseMessage = await httpClient.PostAsync(ucwaApplicationsRootUrl, new StringContent(createUcwaPostData, 
                    Encoding.UTF8, "application/json"));
                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    tc.TrackTrace("Application on the UCWA server created successfully.");
                    Console.WriteLine("Application on the UCWA server created successfully.");
                    createUcwaAppsResults = httpResponseMessage.Content.ReadAsStringAsync().Result;
                    ApplicationRoot obj = new ApplicationRoot();
                    JsonConvert.PopulateObject(createUcwaAppsResults, obj);
                    if (obj != null)
                    {
                        UCWAConfiguration.ucwaApplication = obj._links.self.href;
                        // ConfigData.ucwaApplications += ConfigData.ucwaApplication;
                        UCWAConfiguration.ucwaEvents = obj._links.events.href;
                    }
                }
                else
                {
                    tc.TrackTrace("Failed to create application on the UCWA server.");
                    tc.TrackEvent("CreateUcwaApps-Failed");
                    Console.WriteLine("Failed to create application on the UCWA server.");
                }
                return createUcwaAppsResults;
            }
            catch (Exception ex)
            {
                tc.TrackException(ex);
                tc.TrackEvent("CreateUcwaApps-Exception");
                Console.WriteLine("Failed to create application on the UCWA server.");
                throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                    " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
            }
        }

        public async Task<Boolean> DeleteUcwaApps(HttpClient httpClient, OAuthTokenRoot authToken, TelemetryClient tc)
        {
            try
            {
                string ucwaApplicationRootUrl = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaApplication;
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);
                var httpResponseMessage = await httpClient.DeleteAsync(ucwaApplicationRootUrl);
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
                tc.TrackException(ex);
                tc.TrackEvent("DeleteUcwaApps-Exception");
                Console.WriteLine("Failed to delete application on the UCWA server.");
                throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                    " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
            }
        }
    }
}
