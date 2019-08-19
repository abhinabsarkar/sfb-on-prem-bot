using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Models.CommonEventRoot;
using SkypeConsoleBot.Models.MessageRoot;
using SkypeConsoleBot.Services.Bot;
using SkypeConsoleBot.Services.Helper;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    class UCWAReceiveMessage : IUCWAReceiveMessage
    {
        private readonly OAuthTokenRoot _authToken;
        private readonly bool _isNextMsg;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public UCWAReceiveMessage(HttpClient httpClient, TelemetryClient tc, OAuthTokenRoot authToken, bool isNextMsg = false)
        {
            UCWAConfiguration._httpClient = httpClient;
            UCWAConfiguration._tc = tc;            
            _authToken = authToken;
            _isNextMsg = isNextMsg;
        }

        public async Task GetMessage(HttpClient httpClient, TelemetryClient tc, OAuthTokenRoot authToken, bool isNextMsg = false)
        {
            await GetIM_Step01_Application(httpClient, tc);
        }

        public async Task GetIM_Step01_Application(HttpClient httpClient, TelemetryClient tc)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Remove("Accept");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UCWAConfiguration.authToken.access_token);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                string url_00 = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaApplication;// + ConfigData.ucwaPeopleContact;

                var res_00 = await httpClient.GetAsync(url_00);
                string res_00_request = res_00.RequestMessage.ToString();
                string res_00_headers = res_00.Headers.ToString();
                string res_00_status = res_00.StatusCode.ToString();
                var res_00_content = await res_00.Content.ReadAsStringAsync();

                if (res_00_status == "OK")
                {
                    ApplicationRoot obj = new ApplicationRoot();
                    JsonConvert.PopulateObject(res_00_content, obj);
                    if (obj != null)
                    {
                        UCWAConfiguration.ucwaApplication = obj._links.self.href;
                        UCWAConfiguration.ucwaEvents = obj._links.events.href;
                    }

                    //GetIM_Step02_Contact(httpClient);
                    GetIM_Step03_Events(httpClient, tc);
                }
                else
                {
                    //ConfigData.Log("2", String.Format(">> GetIM ended abnormally. {0}", "STEP01"));
                }
            }
            catch (Exception ex)
            {
                tc.TrackException(ex);
                throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                    " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
            }
        }

        async public static void GetIM_Step03_Events(HttpClient httpClient, TelemetryClient tc)
        {
            // Generic GetService< T> method is an extension method. Add namespace Microsoft.Extensions.DependencyInjection
            var textExtractionService = DI._serviceProvider.GetService<ITextExtraction>();
            var ucwaSetTypingService = DI._serviceProvider.GetService<IUCWASetTyping>();            

            try
            {
                httpClient.DefaultRequestHeaders.Remove("Accept");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UCWAConfiguration.authToken.access_token);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                string url_00 = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaEvents;
                var res_00 = await httpClient.GetAsync(url_00);

                string res_00_request = res_00.RequestMessage.ToString();
                string res_00_headers = res_00.Headers.ToString();
                string res_00_status = res_00.StatusCode.ToString();
                var res_00_content = await res_00.Content.ReadAsStringAsync();

                if (res_00_status == "OK")
                {
                    bool hendle = false;

                    EventRoot eventRoot = JsonConvert.DeserializeObject<EventRoot>(res_00_content);
                    //JsonConvert.PopulateObject(res_00_content, eventRoot);
                    if (eventRoot.sender?.Exists(s => s.events?.Exists(e => (e.status?.Equals("Success", StringComparison.OrdinalIgnoreCase) ?? false) && (e.type?.Equals("completed", StringComparison.OrdinalIgnoreCase) ?? false) && (e._embedded?.message?.direction?.Equals("Incoming", StringComparison.OrdinalIgnoreCase) ?? false)) ?? false) ?? false)
                    //if (res_00_content.Contains("Incoming") && res_00_content.Contains("completed"))
                    {
                        if (eventRoot != null)
                        {

                            if (eventRoot._links != null)
                            {
                                hendle = true;

                                List<Models.CommonEventRoot.Sender> sender = eventRoot.sender.FindAll(x => x.rel.Equals("conversation"));
                                if (sender != null)
                                {
                                    foreach (var item in sender)
                                    {
                                        List<Models.CommonEventRoot.Event> msgInvitation = item.events.FindAll(x => x.link.rel.Equals("message"));
                                        if (msgInvitation != null)
                                        {
                                            foreach (var item1 in msgInvitation)
                                            {
                                                if (item1._embedded != null && item1._embedded.message != null && item1._embedded.message.direction == "Incoming")
                                                {
                                                    string sendMessageUrl = item1._embedded.message._links.messaging.href;
                                                    UCWAConfiguration.ucwaEvents = eventRoot._links.next.href;

                                                    string message = string.Empty;

                                                    if (item1._embedded.message._links.htmlMessage != null)
                                                    {
                                                        message = await textExtractionService.GetMessageFromHtml(item1._embedded.message._links.htmlMessage.href);
                                                    }
                                                    else if (item1._embedded.message._links.plainMessage != null)
                                                    {
                                                        message = await textExtractionService.GetMessageFromHref(item1._embedded.message._links.plainMessage.href);
                                                    }

                                                    var conversationId = item.href.Split('/').Last();
                                                    var fromId = item1._embedded.message._links.participant.title;
                                                    var emailLink = item1._embedded.message._links.participant.href;
                                                    var emailID = emailLink.Substring(emailLink.LastIndexOf("/") + 1);

                                                    //Send the user an impression that the bot is typing a message   
                                                    string ucwaSetTypingUrl = DI._config.GetSection("UCWA:ucwa_applications_host").Value + UCWAConfiguration.ucwaApplication +
                                                        UCWAConfiguration.ucwaConversationsUri + "/" + conversationId + UCWAConfiguration.ucwaSetTypingUri;                                                    
                                                    await ucwaSetTypingService.SetTyping(UCWAConfiguration._httpClient, UCWAConfiguration.authToken, ucwaSetTypingUrl,
                                                        UCWAConfiguration._tc);                                                    

                                                    // Create the Skype For Business On-Premise Adapter, and add Conversation State
                                                    // to the Bot. The Conversation State will be stored in memory.                                                    
                                                    var skypeForBusinessAdapter = DI._serviceProvider.GetService<ISkypeForBusinessOnPremAdapter>();

                                                    // Create the instance of our Bot.                                                    
                                                    var echoBotService = DI._serviceProvider.GetService<IEchoBot>();

                                                    // Connect the Skype For Business On-Premise Adapter to the Bot.                                                    
                                                    skypeForBusinessAdapter.ProcessActivityAsync(fromId, conversationId, sendMessageUrl, message,
                                                        async (turnContext, cancellationToken) => await echoBotService.OnTurnAsync(turnContext)).Wait();
                                                }
                                            }

                                        }
                                    }
                                }
                            }
                            GetIM_Step03_Events(httpClient, tc);
                        }
                    }
                    else if (eventRoot.sender?.Exists(s => s.events?.Exists(e => (e?._embedded?.messagingInvitation?.state?.Equals("Connecting", StringComparison.OrdinalIgnoreCase) ?? false) && (e.type?.Equals("started", StringComparison.OrdinalIgnoreCase) ?? false) && (e._embedded?.messagingInvitation?.direction?.Equals("Incoming", StringComparison.OrdinalIgnoreCase) ?? false)) ?? false) ?? false)
                    //else if (res_00_content.Contains("Incoming") && res_00_content.Contains("Connecting"))
                    {
                        string message = string.Empty;
                        string acceptUrl = string.Empty;
                        if (eventRoot != null)
                        {
                            if (eventRoot._links != null)
                            {
                                hendle = true;
                                UCWAConfiguration.ucwaEvents = eventRoot._links.next.href;

                                List<Models.CommonEventRoot.Sender> sender = eventRoot.sender.FindAll(x => x.rel.Equals("communication"));
                                if (sender != null)
                                {

                                    foreach (var item in sender)
                                    {
                                        List<Models.CommonEventRoot.Event> msgInvitation = item.events.FindAll(x => x.link.rel.Equals("messagingInvitation"));

                                        if (msgInvitation != null)
                                        {
                                            foreach (var item1 in msgInvitation)
                                            {
                                                if (item1._embedded != null && item1._embedded.messagingInvitation._links != null && item1._embedded.messagingInvitation._links.accept != null)
                                                {
                                                    acceptUrl = item1._embedded.messagingInvitation._links.accept.href;
                                                    message = await textExtractionService.GetMessageFromHref(item1._embedded.messagingInvitation._links.message.href);
                                                    await GetIM_Step04_MessageAccept(httpClient, tc, acceptUrl, message);
                                                }
                                            }
                                        }
                                    }
                                }
                                GetIM_Step03_Events(httpClient, tc);
                            }
                        }
                    }
                    else // if (hendle == false)
                    {
                        MessageRoot obj = new MessageRoot();
                        JsonConvert.PopulateObject(res_00_content, obj);
                        if (obj != null)
                        {
                            if (obj._links != null)
                            {
                                UCWAConfiguration.ucwaEvents = obj._links.next.href;
                            }
                        }
                        GetIM_Step03_Events(httpClient, tc);
                    }

                }
                else
                {
                    //ConfigData.Log("2", String.Format(">> Error in step 03. {0}", "No OK received"));
                }
            }
            catch (Exception ex)
            {
                if (ex.Message == "A task was canceled.")
                {
                    GetIM_Step03_Events(httpClient, tc);
                }
                else
                //ConfigData.Log("2", String.Format(">> Error in step 03. {0}", ex.InnerException.Message));
                {
                    //Send the exception to App Insights
                    tc.TrackException(ex);
                    //Throw the exception
                    Console.WriteLine("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                        " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
                    //throw new CustomException("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                    //    " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
                }
            }
        }

        async public static Task GetIM_Step04_MessageAccept(HttpClient httpClient, TelemetryClient tc, string acceptUrl, string message)
        {
            try
            {
                httpClient.DefaultRequestHeaders.Remove("Accept");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", UCWAConfiguration.authToken.access_token);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                string url_00 = DI._config.GetSection("UCWA:ucwa_applications_host").Value + acceptUrl;
                var res_00 = await httpClient.PostAsync(url_00, null);

                string res_00_request = res_00.RequestMessage.ToString();
                string res_00_headers = res_00.Headers.ToString();
                string res_00_status = res_00.StatusCode.ToString();
                var res_00_content = await res_00.Content.ReadAsStringAsync();

                if (res_00_status == "NoContent")
                {
                    //await GetIM_Step05_Event_Send_Msg(httpClient, message);
                }
                else
                {
                    Console.WriteLine("Error in step 04. No OK received");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error occured in " + MethodBase.GetCurrentMethod().Name + ":" + ex.Message +
                 " TargetSite:" + ex.TargetSite + " StackTrace: " + ex.StackTrace);
                tc.TrackException(ex);
            }
        }

    }
}
