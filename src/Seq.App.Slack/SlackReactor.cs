using System;
using System.Linq;
using DotLiquid;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack
{
    [SeqApp("Slack Notifier", Description = "Sends events to a Slack channel.")]
    public class SlackReactor
        : Reactor
        , ISubscribeTo<LogEventData>
    {
        private const uint AlertEventType = 0xA1E77000;
        private const string DefaultIconUrl = "https://getseq.net/images/nuget/seq-apps.png";

        [SeqAppSetting(
            DisplayName = "Webhook URL",
            HelpText = "Add the Incoming WebHooks app to your Slack to get this URL.")]
        public string WebhookUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Channel",
            IsOptional = true,
            HelpText = "The channel to be used for the Slack notification. If not specified, uses the webhook default.")]
        public string Channel { get; set; }

        [SeqAppSetting(
            DisplayName = "App name",
            IsOptional = true,
            HelpText = "The name that Seq uses when posting to Slack. If not specified, uses the name of the Seq app instance. The name can also be read from a property in the event data like {{Properties.ProcessName}}")]
        public string Username { get; set; }

        [SeqAppSetting(
            DisplayName = "Suppression time (minutes)",
            IsOptional = true,
            HelpText = "Once an event type has been sent to Slack, the time to wait before sending again. The default is zero.")]
        public int SuppressionMinutes { get; set; } = 0;

        [SeqAppSetting(
            DisplayName = "Message",
            InputType = SettingInputType.LongText,
            HelpText = "The message to send to Slack. Refer to https://api.slack.com/docs/formatting for formatting options. "
                    +  "Event property values can be added in the format {{PropertyName}}. "
                    +  "If you want a direct link to this event this can be done by adding '<[YourSeqEndpoint]/#/events?filter=@Id%3D'{{Id}}'|Look at me on Seq!>'. "
                    +  "The default is {{Message}}. This also supports markdown. ",
            IsOptional = true)]
        public string MessageTemplate { get; set; }

        [SeqAppSetting(
            DisplayName = "Attachment",
            InputType = SettingInputType.LongText,
            IsOptional = true,
            HelpText = "This adds an attachment to the message. The color of the sidebar is decided by the logevent level. This also supports markdown.")]
        public string Attachment { get; set; }

        [SeqAppSetting(
            DisplayName = "Icon URL",
            HelpText = "The image to show in the room for the message. The default is https://getseq.net/images/nuget/seq-apps.png",
            IsOptional = true)]
        public string IconUrl { get; set; }

        [SeqAppSetting(
            DisplayName = "Proxy Server",
            HelpText = "Proxy server to be used when making HTTPS request to slack api, uses default credentials",
            IsOptional = true)]
        public string ProxyServer { get; set; }

        private Template messageTemplate;
        private Template usernameTemplate;
        private Template attachmentTemplate;
        private EventTypeSuppressions suppressions;
        private static SlackApi slackApi;

        public void On(Event<LogEventData> evt)
        {
            suppressions = suppressions ?? new EventTypeSuppressions(SuppressionMinutes);
            if (suppressions.ShouldSuppressAt(evt.EventType, DateTime.UtcNow))
            {
                return;
            }

            if (slackApi == null)
            {
                slackApi = new SlackApi(ProxyServer);
            }

            messageTemplate = Template.Parse(MessageTemplate);
            usernameTemplate = Template.Parse(Username);
            attachmentTemplate = Template.Parse(Attachment);

            var param = new
            {
                Id = evt.Data.Id,
                Level = evt.Data.Level.ToString(),
                Message = evt.Data.RenderedMessage,
                Exception = evt.Data.Exception,
                Properties = evt.Data.Properties,
                Timestamp = evt.Data.LocalTimestamp,
                EventType = evt.EventType,
            };

            var message = new SlackMessage("[" + evt.Data.Level + "] " + evt.Data.RenderedMessage,
                GenerateMessageText(evt, param),
                string.IsNullOrWhiteSpace(Username) ? App.Title : usernameTemplate.Render(Hash.FromAnonymousObject(evt.Data)),
                string.IsNullOrWhiteSpace(IconUrl) ? DefaultIconUrl : IconUrl,
                Channel);

            if (!string.IsNullOrEmpty(Attachment))
            {
                var color = EventFormatting.LevelToColor(evt.Data.Level);

                attachmentTemplate.Render(Hash.FromAnonymousObject(param));

                var attachment = new SlackMessageAttachment(color, attachmentTemplate.Render(Hash.FromAnonymousObject(param)));
                message.Attachments.Add(attachment);
            }

            slackApi.SendMessage(WebhookUrl, message);
        }

        private string GenerateMessageText(Event<LogEventData> evt, object param)
        {
            var seqUrl = Host.ListenUris.FirstOrDefault();
            return messageTemplate.Render(Hash.FromAnonymousObject(param));
        }
    }
}
