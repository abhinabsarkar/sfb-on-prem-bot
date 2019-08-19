using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkypeConsoleBot
{
    class Program
    {   
        static async Task Main(string[] args)
        {
            string commandString = string.Empty;
            Console.ForegroundColor = ConsoleColor.White;

            try
            {
                // Register the dependencies
                DI.RegisterServices();

                // Load the configuration settings file
                DI.LoadConfiguration();

                //Configure the app insights instrumentation key
                TelemetryConfiguration.Active.InstrumentationKey = DI._config["appinsights_key"];

                // Get the authentication token from Lync Server
                var lyncAuthenticationService = DI._serviceProvider.GetService<ILyncAuthentication>();
                UCWAConfiguration.authToken = await lyncAuthenticationService.GetOAuthToken(UCWAConfiguration._httpClient, DI._config.GetSection("Skypebot:user_name").Value,
                    DI._config.GetSection("Skypebot:password").Value, DI._config.GetSection("Skypebot:domain").Value, DI._config.GetSection("UCWA:lync_oauth_url").Value, UCWAConfiguration._tc);

                //Since the auth token expires in around 7.5 hours, get a new token only 100 seconds before it expires.
                //This call is made in succession here because "ConfigData.authToken.expires_in" value is fetched only 
                //after getting the token for the first time. So initially the authentication calls are back to back.
                var tokenExpirationTimer = new System.Threading.Timer(
                    async e => await lyncAuthenticationService.GetOAuthToken(UCWAConfiguration._httpClient, DI._config.GetSection("Skypebot:user_name").Value,
                    DI._config.GetSection("Skypebot:password").Value, DI._config.GetSection("Skypebot:domain").Value, DI._config.GetSection("UCWA:lync_oauth_url").Value, UCWAConfiguration._tc),
                    null,
                    TimeSpan.Zero,
                    TimeSpan.FromSeconds(UCWAConfiguration.authToken.expires_in - 100));

                if (UCWAConfiguration.authToken.access_token != null)
                {
                    //Create an application on the UCWA server
                    List<string> Modalities = new List<string>();
                    Modalities.Add("PhoneAudio");
                    Modalities.Add("Messaging");
                    //Set the properties for the applications resource
                    UCWAMyApps ucwaMyAppsObject = new UCWAMyApps()
                    {
                        UserAgent = "IntranetBot",
                        EndpointId = Guid.NewGuid().ToString(),
                        Culture = "en-US"
                    };
                    //Create an application on the Skype UCWA server and register it.
                    var ucwaApplicationService = DI._serviceProvider.GetService<IUCWAApplication>();
                    UCWAConfiguration.createUcwaAppsResults = await ucwaApplicationService.CreateUcwaApps(UCWAConfiguration._httpClient,
                        UCWAConfiguration.authToken, DI._config.GetSection("UCWA:ucwa_applications_url").Value, ucwaMyAppsObject, UCWAConfiguration._tc);

                    //Set IM status as online
                    var manageIMStatusService = DI._serviceProvider.GetService<IManageIMStatus>();
                    await manageIMStatusService.SetIMStatus(UCWAConfiguration._httpClient, UCWAConfiguration._tc);

                    //Keep the bot Online all the time else it will show away after 5 minutes
                    var timer = new System.Threading.Timer(
                        async e => await manageIMStatusService.KeepStatusOnline(UCWAConfiguration._httpClient, UCWAConfiguration._tc),
                        null,
                        TimeSpan.Zero,
                        TimeSpan.FromMinutes(3));

                    //Get the message. The method "GetIM_Step03_Events" in the class UCWAReceiveMessage is a recursive function 
                    //which will keep listening for the incoming messages
                    await GetMessage(UCWAConfiguration._tc, true);
                }

                // Calling readline to avoid calling the dispose services
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                UCWAConfiguration._tc.TrackException(ex);
                UCWAConfiguration._tc.TrackEvent("MainFunction-Exception");
            }
            finally
            {
                // Calling Dispose() on service provider is mandatory as otherwise registered instances will not get disposed. 
                DI.DisposeServices();
            }
        }

        static async Task GetMessage(TelemetryClient tc, bool IsNextMsg = false)
        {
            if (UCWAConfiguration.authToken == null)
            {
                Console.WriteLine("You haven't logged in yet!");
                tc.TrackTrace("You haven't logged in yet!");
                return;
            }

            Console.WriteLine("Bot is listening to messages...");
            tc.TrackEvent("BotActive");
            // Get message from skype channel and pass it to the bot
            var ucwaReceiveMessageService = DI._serviceProvider.GetService<IUCWAReceiveMessage>();
            await ucwaReceiveMessageService.GetMessage(UCWAConfiguration._httpClient, tc, UCWAConfiguration.authToken, IsNextMsg);
        }
    }
}
