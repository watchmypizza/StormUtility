using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.VisualBasic;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stormtility.commands.MakeItAQuote
{
    public class quotecommand : ApplicationCommandModule
    {
        [SlashCommand("quote", "Make it a quote!")]
        public async Task quotify(InteractionContext ctx, [Option("Message", "The message link to quote")] string message)
        {
            await ctx.DeferAsync(true);

            var pattern = @"https://discord\.com/channels/(\d+)/(\d+)/(\d+)";
            var match = Regex.Match(message, pattern);

            if (!match.Success)
                return;

            ulong guildId = ulong.Parse(match.Groups[1].Value);
            ulong channelId = ulong.Parse(match.Groups[2].Value);
            ulong messageId = ulong.Parse(match.Groups[3].Value);

            var channel = ctx.Interaction.Guild.GetChannel(channelId);
            if (channel == null)
                return;

            try
            {
                var msg = await channel.GetMessageAsync(messageId);
                var img = await quote.turnQuote(ctx, msg);
                using var stream = new MemoryStream();
                await img.SaveAsPngAsync(stream);
                stream.Position = 0;

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddFile("img.png", stream));
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("An error occurred while fetching the message."));
                return;
            }
        }
    }
}
