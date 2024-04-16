using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace eShop.WebApp.Chatbot;

public static partial class MessageProcessor
{
    public static MarkupString AllowImages(string message)
    {
        // Having to process markdown and deal with HTML encoding isn't ideal. If the language model could return
        // search results in some defined format like JSON we could simply loop over it in .razor code. This is
        // fine for now though.

        var result = new StringBuilder();
        var prevEnd = 0;
        message = message.Replace("&lt;", "<").Replace("&gt;", ">");

        foreach (Match match in FindMarkdownImages().Matches(message))
        {
            var contentToHere = message.Substring(prevEnd, match.Index - prevEnd);
            result.Append(HtmlEncoder.Default.Encode(contentToHere));
            result.Append($"<img title=\"{(HtmlEncoder.Default.Encode(match.Groups[1].Value))}\" src=\"{(HtmlEncoder.Default.Encode(match.Groups[2].Value))}\" />");

            prevEnd = match.Index + match.Length;
        }
        result.Append(HtmlEncoder.Default.Encode(message.Substring(prevEnd)));

        return new MarkupString(result.ToString());
    }

    [GeneratedRegex(@"\!?\[([^\]]+)\]\s*\(([^\)]+)\)")]
    private static partial Regex FindMarkdownImages();
}
