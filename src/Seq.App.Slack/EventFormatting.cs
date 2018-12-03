using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Seq.Apps.LogEvents;

namespace Seq.App.Slack
{
    static class EventFormatting
    {
        private static readonly Regex PlaceholdersRegex = new Regex(@"(\[(?<key>[^\[\]]+?)(\:(?<format>[^\[\]]+?))?\])", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly IImmutableDictionary<LogEventLevel, string> LevelColorMap = (new Dictionary<LogEventLevel, string>
        {
            [LogEventLevel.Verbose] = "#D3D3D3",
            [LogEventLevel.Debug] = "#D3D3D3",
            [LogEventLevel.Information] = "#00A000",
            [LogEventLevel.Warning] = "#f9c019",
            [LogEventLevel.Error] = "#e03836",
            [LogEventLevel.Fatal] = "#e03836",
        }).ToImmutableDictionary();

        public static string LevelToColor(LogEventLevel level)
        {
            return LevelColorMap[level];
        }
    }
}
