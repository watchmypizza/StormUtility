using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
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
            await ctx.CreateResponseAsync("Sending message...", true);
            return;
        }

        [SlashCommand("testmodal", "Test modal preview")]
        public async Task modalTest(InteractionContext ctx, [Option("title", "Title of the modal")] string title, [Option("TextInput1", "First text input")] string textInput1, [Option("TextInput2", "Second text input")] string? textInput2 = "")
        {
            if(textInput2 == null || textInput2 == string.Empty)
            {
                DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
                                                                .WithCustomId("exampleModal")
                                                                .WithTitle(title)
                                                                .AddComponents(new TextInputComponent(textInput1, "exampleModalText1", required: true, style: TextInputStyle.Short));

                await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
                return;
            } else
            {
                DiscordInteractionResponseBuilder modal = new DiscordInteractionResponseBuilder()
                                                                .WithCustomId("exampleModal")
                                                                .WithTitle(title)
                                                                .AddComponents(new TextInputComponent(textInput1, "exampleModalText1", required: true, style: TextInputStyle.Short))
                                                                .AddComponents(new TextInputComponent(textInput2, "exampleModalText2", required: false, style: TextInputStyle.Paragraph));

                await ctx.CreateResponseAsync(InteractionResponseType.Modal, modal);
                return;
            }
        }

        [SlashCommand("honeypot", "Set the honeypot automation up")]
        public async Task honeypot(InteractionContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = "DO NOT SEND MESSAGES IN THIS DISCORD CHANNEL!",
                Description = "# Sending messages in this discord channel will get you automatically, permanently banned from the discord server!\n### This channel is supposed to be a honeypot for scammers and compromised accounts.",
                Color = DiscordColor.Red
            };

            DiscordChannel channel = ctx.Interaction.Channel;
            await channel.SendMessageAsync(embed);
            return;
        }
    }
}
