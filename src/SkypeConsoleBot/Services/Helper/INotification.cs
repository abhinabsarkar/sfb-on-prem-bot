using Microsoft.ApplicationInsights;
using System;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.Helper
{
    interface INotification
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }
        Task SendMail(String mailSubject, String mailbody, TelemetryClient tc);
    }
}
