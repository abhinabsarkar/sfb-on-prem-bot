using Microsoft.ApplicationInsights;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Net.Http;

namespace SkypeConsoleBot.Models
{
    static class UCWAConfiguration
    {
        // Requires Microsoft.IdentityModel.Clients.ActiveDirectory
        public static AuthenticationResult ucwaAuthenticationResult;

        public static HttpClient _httpClient = new HttpClient();
        public static TelemetryClient _tc = new TelemetryClient();

        public static string ucwaApplication { get; set; }

        public static string ucwaEvents = string.Empty;

        // UCWA URIs for different events
        public static string ucwaMakeMeAvailableUri = "/communication/makeMeAvailable";
        public static string ucwaKeepMeOnlineUri = "/reportMyActivity";
        public static string ucwaMessagingInvitations = "/communication/messagingInvitations";
        public static string ucwaConversationsUri = "/communication/conversations";
        public static string ucwaSetTypingUri = "/messaging/typing";

        public static string ucwaConversation { get; set; }
        public static string ucwaStopMessaging = "/terminate";

        public static string ucwaMessaging = "";
        internal static string ucwaFilter = "communication/conversations?filter=active";

        public static string ucwaPeopleContact = "/people/contacts";       

        public static string createUcwaAppsResults;

        public static OAuthTokenRoot authToken;

    }
}
