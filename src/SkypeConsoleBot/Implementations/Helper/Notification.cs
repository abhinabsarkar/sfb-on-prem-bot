using Microsoft.ApplicationInsights;
using SkypeConsoleBot.Models;
using SkypeConsoleBot.Services.Helper;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations.Helper
{
    class Notification : INotification
    {
        private readonly string _mailSubject;
        private readonly string _mailBody;
        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        // Constructor dependency injection
        public Notification(String mailSubject, String mailBody, TelemetryClient tc)
        {
            _mailSubject = mailSubject;
            _mailBody = mailBody;
            UCWAConfiguration._tc = tc;
        }

        public async Task SendMail(String mailSubject, String mailbody, TelemetryClient tc)
        {
            try
            {
                SmtpClient mailClient = new SmtpClient(DI._config.GetSection("Email:Smtp:mail_server").Value);
                MailAddress from = new MailAddress(DI._config.GetSection("Email:Smtp:from_address").Value);
                MailAddress to = new MailAddress(DI._config.GetSection("Email:Smtp:to_address").Value);
                MailMessage message = new MailMessage(from, to);
                message.Subject = mailSubject;
                message.Body = mailbody;
                await mailClient.SendMailAsync(message);
                tc.TrackEvent("Email-Sent");
            }
            catch (Exception ex)
            {
                //Send the telemetry but don't stop the execution
                tc.TrackEvent("Email-Failed");
                tc.TrackException(ex);
            }
        }
    }
}
