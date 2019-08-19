using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Services.UCWA
{
    public interface IManageIMStatus
    {
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        string Name { get; }
        Task SetIMStatus(HttpClient httpClient, TelemetryClient tc);
        Task KeepStatusOnline(HttpClient httpClient, TelemetryClient tc);
    }
}
