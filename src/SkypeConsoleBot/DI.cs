using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkypeConsoleBot.Implementations;
using SkypeConsoleBot.Implementations.Bot;
using SkypeConsoleBot.Implementations.Helper;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.Bot;
using SkypeConsoleBot.Services.Helper;
using SkypeConsoleBot.Services.UCWA;
using System;
using System.IO;

namespace SkypeConsoleBot
{
    static class DI
    {
        internal static IServiceProvider _serviceProvider;
        internal static IConfiguration _config;

        // Build the configuration with JSON file.         
        public static void LoadConfiguration()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                // Other configuration files like xml, environment variables can also be added
                // Set the property "Copy to output directory" for the json file as "Copy always"
                .AddJsonFile("configsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        // Register the collection of services along with their implementations to the service container
        // These services will be injected to the Main method using constructor injection
        internal static void RegisterServices()
        {
            var collection = new ServiceCollection();
            collection.AddScoped<ILyncAuthentication, LyncAuthentication>(sp => new LyncAuthentication(UCWAConfiguration._httpClient,
                "", "", "", "", UCWAConfiguration._tc));
            collection.AddScoped<IUCWAApplication, UCWAApplication>(sp => new UCWAApplication(UCWAConfiguration._httpClient,
                UCWAConfiguration.authToken, "", new UCWAMyApps(), UCWAConfiguration._tc));
            collection.AddScoped<IManageIMStatus, ManageIMStatus>(sp => new ManageIMStatus(UCWAConfiguration._httpClient, 
                UCWAConfiguration._tc));
            collection.AddScoped<IUCWAMakeMeAvailable, UCWAMakeMeAvailable>(sp => new UCWAMakeMeAvailable(
                UCWAConfiguration._httpClient, UCWAConfiguration.authToken, "", new BotAttributes(), UCWAConfiguration._tc));
            collection.AddScoped<IUCWAReportMyActivity, UCWAReportMyActivity>(sp => new UCWAReportMyActivity(UCWAConfiguration._httpClient,
                UCWAConfiguration.authToken, "", new BotAttributes(), UCWAConfiguration._tc));
            collection.AddScoped<INotification, Notification>(sp => new Notification("", "", UCWAConfiguration._tc));
            collection.AddScoped<ITextExtraction, TextExtraction>(sp => new TextExtraction(""));
            collection.AddScoped<IUCWAReceiveMessage, UCWAReceiveMessage>(sp => new UCWAReceiveMessage(UCWAConfiguration._httpClient,
                UCWAConfiguration._tc, UCWAConfiguration.authToken, false));
            collection.AddScoped<IUCWASendMessage, UCWASendMessage>(sp => new UCWASendMessage(UCWAConfiguration._httpClient,
                "", "", UCWAConfiguration._tc));
            collection.AddScoped<IUCWASetTyping, UCWASetTyping>(sp => new UCWASetTyping(UCWAConfiguration._httpClient,
                UCWAConfiguration.authToken, "", UCWAConfiguration._tc));
            collection.AddScoped<IEchoBot, EchoBot>();
            collection.AddScoped<ISkypeForBusinessOnPremAdapter, SkypeForBusinessOnPremAdapter>(sp =>
                new SkypeForBusinessOnPremAdapter("", "", "", null));
            // ...
            // Keep adding other services
            // ...
            _serviceProvider = collection.BuildServiceProvider();
        }

        // Dispose the service collection
        internal static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }
            if (_serviceProvider is IDisposable)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }
    }
}
