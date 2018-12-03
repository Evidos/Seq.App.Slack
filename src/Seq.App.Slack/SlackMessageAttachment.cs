using System.Collections.Generic;
using Newtonsoft.Json;

namespace Seq.App.Slack
{
    public class SlackMessageAttachment
    {
        [JsonProperty("color")]
        public string Color { get; }

        [JsonProperty("text")]
        public string Text { get; }

        [JsonProperty("fields")]
        public List<SlackMessageAttachmentField> Fields { get; }

        public SlackMessageAttachment(string color, string text = null)
        {
            this.Color = color;
            this.Text = text;
            this.Fields = new List<SlackMessageAttachmentField>();
        }
    }
}
