using Microsoft.ApplicationInsights;
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
    class UCWASendMessage : IUCWASendMessage
    {
        private readonly string _msg;
        private readonly string _sendMessageUrl;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public UCWASendMessage(HttpClient httpClient, string msg, string sendMessageurl, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            _msg = msg;
            _sendMessageUrl = sendMessageurl;
            UCWAConfiguration._tc = tc;
        }

        public async Task SendMessage(HttpClient httpClient, string msg, string url, TelemetryClient tc)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Remove("Accept");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", 
                    UCWAConfiguration.authToken.access_token);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                string url_00;
                if (url != "")
                {
                    url_00 = DI._config.GetSection("UCWA:ucwa_applications_host").Value + url + "/messages?OperationContext=" + 
                        Guid.NewGuid().ToString();
                }
                else
                    url_00 = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaMessaging + 
                        "/messages?OperationContext=" + Guid.NewGuid().ToString();               

                string postdata_05 = msg;
                HttpContent html_05 = new StringContent(postdata_05, Encoding.UTF8, "text/html");

                var res_00 = await httpClient.PostAsync(url_00, html_05);
                string res_00_request = res_00.RequestMessage.ToString();
                string res_00_headers = res_00.Headers.ToString();
                string res_00_status = res_00.StatusCode.ToString();
                var res_00_content = await res_00.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {
                //send the exception to appInsights
                tc.TrackException(ex);                
                throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                   " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
            }
        }
    }
}
