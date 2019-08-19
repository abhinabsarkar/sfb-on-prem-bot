using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using SkypeConsoleBot.Services.Bot;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SkypeConsoleBot.Implementations
{
    internal class SkypeForBusinessOnPremAdapter : BotAdapter, ISkypeForBusinessOnPremAdapter
    {
        private readonly string _fromId;
        private readonly string _conversationId;
        private readonly string _sendMessageUrl;

        // Added this property to resolve the named instance 
        // Since Microsoft.Extensions.DependencyInjection doesn't support registering multiple implementations 
        // of the same interface, this name is used to resolve the instance using LINQ
        public string Name { get { return this.GetType().Name; } }

        public SkypeForBusinessOnPremAdapter(string fromId, string conversationId, string sendMessageUrl, string msg,
            BotCallbackHandler callback = null)
            : base()
        {
            _fromId = fromId;
            _conversationId = conversationId;
            _sendMessageUrl = sendMessageUrl;
        }

        // Adds middleware to the adapter's pipeline.
        public new SkypeForBusinessOnPremAdapter Use(IMiddleware middleware)
        {
            base.Use(middleware);
            return this;
        }

        // Performs the actual translation of input coming from the console
        // into the "Activity" format that the Bot consumes.
        public async Task ProcessActivityAsync(string fromId, string conversationId, string sendMessageUrl, string msg,
            BotCallbackHandler callback = null)
        {
            // Performing the conversion from Skype text to an Activity for
            // which the system handles all messages (from all unique services).
            // All processing is performed by the broader bot pipeline on the Activity
            // object.
            var activity = new Activity()
            {
                Text = msg,

                // Note on ChannelId:
                // The Bot Framework channel is identified by a unique ID.
                // For example, "skype" is a common channel to represent the Skype service.
                // We are inventing a new channel here.
                ChannelId = "skype",
                From = new ChannelAccount(id: fromId, name: fromId),
                Recipient = new ChannelAccount(id: "bot", name: "Bot"),
                Conversation = new ConversationAccount(id: conversationId),
                ServiceUrl = "https://skype.botframework.com",
                Timestamp = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString(),
                Type = ActivityTypes.Message,
                ChannelData = sendMessageUrl
            };

            using (var context = new TurnContext(this, activity))
            {
                await this.RunPipelineAsync(context, callback, default(CancellationToken)).ConfigureAwait(false);
            }
        }

        // Sends activities to the conversation.
        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext context, Activity[] activities, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (activities == null)
            {
                throw new ArgumentNullException(nameof(activities));
            }

            if (activities.Length == 0)
            {
                throw new ArgumentException("Expecting one or more activities, but the array was empty.", nameof(activities));
            }

            var responses = new ResourceResponse[activities.Length];

            for (var index = 0; index < activities.Length; index++)
            {
                var activity = activities[index];

                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                        {
                            IMessageActivity message = activity.AsMessageActivity();

                            // A message exchange between user and bot can contain media attachments
                            // (e.g., image, video, audio, file).  In this particular example, we are unable
                            // to create Attachments to messages, but this illustrates processing.
                            if (message.Attachments != null && message.Attachments.Any())
                            {
                                var attachment = message.Attachments.Count == 1 ? "1 attachment" : $"{message.Attachments.Count()} attachments";
                                Console.WriteLine($"{message.Text} with {attachment} ");
                            }
                            else
                            {
                                Console.WriteLine($"{message.Text}");
                            }
                        }

                        break;

                    case ActivityTypesEx.Delay:
                        {
                            // The Activity Schema doesn't have a delay type build in, so it's simulated
                            // here in the Bot. This matches the behavior in the Node connector.
                            int delayMs = (int)((Activity)activity).Value;
                            await Task.Delay(delayMs).ConfigureAwait(false);
                        }

                        break;

                    case ActivityTypes.Trace:
                        // Do not send trace activities unless you know that the client needs them.
                        // For example: BF protocol only sends Trace Activity when talking to emulator channel.
                        break;

                    default:
                        Console.WriteLine("Bot: activity type: {0}", activity.Type);
                        break;
                }

                responses[index] = new ResourceResponse(activity.Id);
            }

            return responses;
        }

        // Normally, replaces an existing activity in the conversation.
        // Not implemented for this sample.
        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        // Deletes an existing activity in the conversation.
        // Not implemented for this sample.
        public override Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}