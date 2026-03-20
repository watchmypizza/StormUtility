using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.WebRequestMethods;

namespace Stormtility.commands.TestingCommands
{
    public class testcommands : ApplicationCommandModule
    {
        [SlashCommand("filetest", "Test command to test if inputting files like screenshots works")]
        public async Task filetest(InteractionContext ctx, [Option("file", "Input file to test")]DiscordAttachment file)
        {
            using var http = new HttpClient();
            using var ms = new MemoryStream();
            var bytes = await http.GetByteArrayAsync(file.Url);
            await ms.WriteAsync(bytes);
            ms.Position = 0;

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddFile(file.FileName, ms));
        } 

        [SlashCommand("verificationTest", "Test if verification works")]
        public async Task verificationTest(InteractionContext ctx)
        {
            DiscordButtonComponent button = new DiscordButtonComponent(ButtonStyle.Primary, "openVerifyModal", "Verify");
            var embed = new DiscordEmbedBuilder()
            {
                Title="Verification",
                Description="Codes are valid for 1 minute before expiration for each user.",
                Color=DiscordColor.Orange
            };
            DiscordChannel channel = ctx.Interaction.Channel;
            await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed).AddComponents(button));
            return;
        }
    }
}
