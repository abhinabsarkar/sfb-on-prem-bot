using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.Helper;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    class UCWAReportMyActivity : IUCWAReportMyActivity
    {
        int counter = 0;
        private readonly string _keepMeOnlineUrl;
        private readonly BotAttributes _botAttributes;
        private readonly OAuthTokenRoot _authToken;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public UCWAReportMyActivity(HttpClient httpClient, OAuthTokenRoot authToken, String ucwaKeepMeOnlineUrl,
            BotAttributes botAttributes, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            _keepMeOnlineUrl = ucwaKeepMeOnlineUrl;
            _authToken = authToken;
            _botAttributes = botAttributes;
            UCWAConfiguration._tc = tc;
        }

        public async Task KeepMeOnline(HttpClient httpClient, OAuthTokenRoot authToken, String ucwaKeepMeOnlineUrl,
            TelemetryClient tc)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken.access_token);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //Successfull response is Http 204 No content
                var httpResponseMessage = await
                    httpClient.PostAsync(ucwaKeepMeOnlineUrl, new StringContent(string.Empty, Encoding.UTF8,
                    "application/json"));
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                if (result != String.Empty)
                {
                    //Trace the response message if reportMyActivity fails. Also send the response message why it failed.
                    tc.TrackTrace("Failed to set the bot status active. " + httpResponseMessage.ToString());
                    //Retry 3 times by calling the method recursively with an interval of 20 seconds
                    if (counter < 3)
                    {
                        //If the status code is 401, get the token again
                        if (httpResponseMessage.ToString().Contains("StatusCode: 401"))
                        {
                            tc.TrackEvent("Retry-401");
                            //Send email reporting this error
                            // Generic GetService< T> method is an extension method. Add namespace Microsoft.Extensions.DependencyInjection
                            var notificationService = DI._serviceProvider.GetService<INotification>();
                            await notificationService.SendMail("Bot Error 401", httpResponseMessage.ToString(), tc);
                            // Get the authentication token from Lync Server. 
                            // Generic GetService< T> method is an extension method. Add namespace Microsoft.Extensions.DependencyInjection
                            var lyncAuthenticationService = DI._serviceProvider.GetService<ILyncAuthentication>();
                            UCWAConfiguration.authToken = await lyncAuthenticationService.GetOAuthToken(UCWAConfiguration._httpClient, DI._config.GetSection("Skypebot:user_name").Value,
                                DI._config.GetSection("Skypebot:password").Value, DI._config.GetSection("Skypebot:domain").Value, DI._config.GetSection("UCWA:lync_oauth_url").Value, UCWAConfiguration._tc);
                            tc.TrackEvent("BotRecovered-401");
                            // Recursively call itself
                            await KeepMeOnline(httpClient, UCWAConfiguration.authToken, ucwaKeepMeOnlineUrl, tc);
                        }
                        // Address 404 Not found - Added below if statement
                        else if (httpResponseMessage.ToString().Contains("StatusCode: 404"))
                        {
                            tc.TrackEvent("Retry-404");
                            //Delete the exisiting UCWA application. The UCWA application identifier is the GUID (EndpointID),
                            //but to delete the ucwa application, you need the root application url.
                            //The UCWA application url will be of format /ucwa/oauth/v1/applications/103004647364
                            var ucwaApplicationService = DI._serviceProvider.GetService<IUCWAApplication>();
                            await ucwaApplicationService.DeleteUcwaApps(httpClient, UCWAConfiguration.authToken, tc);

                            //Get the Authentication token
                            //LyncAuth.GetOAuthToken(httpClient, ConfigData.userName, ConfigData.password,
                            //    ConfigData.domain, ConfigData.lyncOAuthUri, tc);

                            //Create an application on the UCWA server
                            List<string> Modalities = new List<string>();
                            Modalities.Add("PhoneAudio");
                            Modalities.Add("Messaging");
                            //Set the properties for the applications resource
                            UCWAMyApps ucwaMyAppsObject = new UCWAMyApps()
                            {
                                UserAgent = "Skypebot",
                                EndpointId = Guid.NewGuid().ToString(),
                                Culture = "en-US"
                            };
                            //Create an application on the Skype UCWA server and register it.
                            string createUcwaAppsResults;
                            createUcwaAppsResults = await ucwaApplicationService.CreateUcwaApps(UCWAConfiguration._httpClient,
                                UCWAConfiguration.authToken, DI._config.GetSection("UCWA:ucwa_applications_url").Value, ucwaMyAppsObject, UCWAConfiguration._tc);

                            //Invoke MakeMeAvailable else it will give 409 conflict error i.e. invoking a resource before setting the status "online"
                            //Set IM status as online
                            var manageIMStatusService = DI._serviceProvider.GetService<IManageIMStatus>();
                            await manageIMStatusService.SetIMStatus(UCWAConfiguration._httpClient, UCWAConfiguration._tc);

                            //Start listening to messages    
                            var ucwaReceiveMessageService = DI._serviceProvider.GetService<IUCWAReceiveMessage>();
                            await ucwaReceiveMessageService.GetMessage(UCWAConfiguration._httpClient, tc, UCWAConfiguration.authToken, false);
                            
                            Console.WriteLine("Bot recovered. Bot is listening to messages...");
                            tc.TrackEvent("BotRecovered-404");

                            if (counter < 3)
                            {
                                //increment the counter so that it doesn't send infinite emails
                                counter++;
                                //Send email reporting this error
                                // Generic GetService< T> method is an extension method. Add namespace Microsoft.Extensions.DependencyInjection
                                var notificationService = DI._serviceProvider.GetService<INotification>();
                                await notificationService.SendMail("Bot Error 404", httpResponseMessage.ToString(), tc);                                
                            }
                        }
                        else
                        {
                            Thread.Sleep(20000);
                            counter++;
                            tc.TrackTrace("Retrying to set the bot status active. Attempt # " + counter);
                            tc.TrackEvent("Retry-KeepOnline");
                            // Recursively call itself
                            await KeepMeOnline(httpClient, UCWAConfiguration.authToken, ucwaKeepMeOnlineUrl, tc);
                        }
                    }
                }
                else
                {
                    //set counter back to zero
                    counter = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set the bot status active." + ex.Message);
                tc.TrackTrace("Failed to set the bot status active. More details can be found in the exception.");
                tc.TrackException(ex);
            }
        }
    }
}
