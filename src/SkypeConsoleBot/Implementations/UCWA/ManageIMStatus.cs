using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    public class ManageIMStatus : IManageIMStatus
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public ManageIMStatus(HttpClient httpClient, TelemetryClient tc)
        {
            UCWAConfiguration._httpClient = httpClient;
            UCWAConfiguration._tc = tc;
        }

        public async Task SetIMStatus(HttpClient httpClient, TelemetryClient tc)
        {
            List<string> MessageFormats = new List<string>();
            MessageFormats.Add("Plain");
            MessageFormats.Add("Html");

            List<string> Modalities = new List<string>();
            // Modalities.Add("PhoneAudio");
            Modalities.Add("Messaging");

            BotAttributes botAttributes = new BotAttributes
            {
                phoneNumber = "",
                signInAs = "Online", // Status - Online, Busy, DoNotDisturb, BeRightBack, Away, or Offwork
                supportedMessageFormats = MessageFormats,
                supportedModalities = Modalities
            };

            var ucwaMakeMeAvailableRootUrl = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaApplication + 
                UCWAConfiguration.ucwaMakeMeAvailableUri;
            if (ucwaMakeMeAvailableRootUrl != String.Empty)
            {
                // Generic GetService< T> method is an extension method. Add namespace Microsoft.Extensions.DependencyInjection
                var ucwaMakeMeAvailableService = DI._serviceProvider.GetService<IUCWAMakeMeAvailable>();
                // Set the status online
                if (await ucwaMakeMeAvailableService.MakeMeAvailable(httpClient, UCWAConfiguration.authToken, 
                    ucwaMakeMeAvailableRootUrl, botAttributes, tc))
                {
                    Console.WriteLine("Skype ID's status set to Online.");
                    tc.TrackTrace("Skype ID's status set to Online.");
                }
                else
                {
                    Console.WriteLine("Unable to set Skype ID's status as Online.");
                    tc.TrackTrace("Unable to set Skype ID's status as Online.");
                }
                return;
            }
            else
            {
                Console.WriteLine("ucwaMakeMeAvailableRootUrl is empty. Unable to set Skype ID's status as Online.");
                tc.TrackTrace("ucwaMakeMeAvailableRootUrl is empty. Unable to set Skype ID's status as Online.");
            }
        }

        public async Task KeepStatusOnline(HttpClient httpClient, TelemetryClient tc)
        {
            var ucwaKeepMeOnlineUrl = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaApplication 
                + UCWAConfiguration.ucwaKeepMeOnlineUri;
            // Generic GetService< T> method is an extension method. Add namespace Microsoft.Extensions.DependencyInjection
            var ucwaReportMyActivityService = DI._serviceProvider.GetService<IUCWAReportMyActivity>();
            await ucwaReportMyActivityService.KeepMeOnline(UCWAConfiguration._httpClient, UCWAConfiguration.authToken, 
                ucwaKeepMeOnlineUrl, UCWAConfiguration._tc);
        }
    }
}
