using CommunityToolkit.HighPerformance.Helpers;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.ModalCommands;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.WebRequestMethods;

namespace Stormtility.commands.TestingCommands
{
    [Command("testing")]
    public class testcommands
    {
        [SlashCommand("filetest", "Test command to test if inputting files like screenshots works")]
        [Command("filetest")]
        [Description("please help me")]
        public async Task filetest(SlashCommandContext ctx, [Parameter("file")][Description("Input file to test")]DiscordAttachment file)
        {
            using var http = new HttpClient();
            using var ms = new MemoryStream();
            var bytes = await http.GetByteArrayAsync(file.Url);
            await ms.WriteAsync(bytes);
            ms.Position = 0;

            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddFile(file.FileName, ms));
        } 

        [SlashCommand("verificationTest", "Test if verification works")]
        [Command("verificationtest")]
        [Description("this is unbearable")]
        public async Task verificationTest(SlashCommandContext ctx)
        {
            DiscordButtonComponent button = new DiscordButtonComponent(DiscordButtonStyle.Primary, "openVerifyModal", "Verify");
            var embed = new DiscordEmbedBuilder()
            {
                Title="Verification",
                Description="Codes are valid for 1 minute before expiration for each user.",
                Color=DiscordColor.Orange
            };
            DiscordChannel channel = ctx.Interaction.Channel;
            await channel.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed).AddActionRowComponent(button));
            await ctx.RespondAsync("Sending message...", true);
            return;
        }

        [SlashCommand("testmodal", "Test modal preview")]
        [Command("testmodal")]
        [Description("was it commandcontext? fuck")]
        public async Task modalTest(SlashCommandContext ctx, [Parameter("title")][Description("Title of the modal")] string title, [Parameter("TextInput1")][Description("First text input")] string textInput1, [Parameter("TextInput2")][Description("Second text input")] string? textInput2 = "")
        {
            if(textInput2 == null || textInput2 == string.Empty)
            {
                DiscordModalBuilder modal = new DiscordModalBuilder()
                                                                .WithTitle(title)
                                                                .AddTextInput(new DiscordTextInputComponent("exampleModalText1", required: true,style: DiscordTextInputStyle.Short), textInput1);

                await ctx.RespondWithModalAsync(modal);
                return;
            } else
            {
                DiscordModalBuilder modal = new DiscordModalBuilder()
                                                                .WithCustomId("exampleModal")
                                                                .WithTitle(title)
                                                                .AddTextInput(new DiscordTextInputComponent("exampleModalText1", required: true, style: DiscordTextInputStyle.Short), textInput1)
                                                                .AddTextInput(new DiscordTextInputComponent("exampleModalText2", required: false, style: DiscordTextInputStyle.Paragraph), textInput2);

                await ctx.RespondWithModalAsync(modal);
                return;
            }
        }

        [SlashCommand("honeypot", "Set the honeypot automation up")]
        [Command("honeypot")]
        [Description("no clue why i registered this")]
        public async Task honeypot(SlashCommandContext ctx)
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

        [SlashCommand("ticketmodal", "Test ticketmodal lmaoz")]
        [Command("ticketmodal")]
        [Description("my sanity is gone")]
        public async Task ticketmodal(SlashCommandContext ctx)
        {
            DiscordModalBuilder modal = new DiscordModalBuilder()
                                                            .WithCustomId("gameappealmodal")
                                                            .WithTitle("Create a game appeal...")
                                                            .AddTextInput(new DiscordTextInputComponent("robloxusername", "stormyxvreal", "", true, DiscordTextInputStyle.Short), "Your ROBLOX username")
                                                            .AddFileUpload(new DiscordFileUploadComponent("proof", 1, 1, false), "Screenshot or clip of you getting banned", "Incase you cannot screenshot or do not have a medal clip, leave this field out. It is not required.")
                                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree",
                                                                                new List<DiscordCheckboxGroupOption> {  new DiscordCheckboxGroupOption("agreed", "Agree to rules",
                                                                                description: "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", selectedByDefault: false)},
                                                                            1, 1, true), "Ticket Rules");

            await ctx.RespondWithModalAsync(modal);
        }

        [Command("speedtypistmodal")]
        [Description("I have reacquired my sanity")]
        public async Task speedtypistmodal(SlashCommandContext ctx)
        {
            DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("speedtypistmodal")
                                            .WithTitle("Apply for speedtypist role...")
                                            .AddTextInput(new DiscordTextInputComponent("monkeytypeprofile", "https://monkeytype.com/profile/peanutxvreal", "", required: true, style: DiscordTextInputStyle.Short), "Your MonkeyType profile", "You can find this under Username at the top → Public Profile → Copy link in the URL bar")
                                            .AddFileUpload(new DiscordFileUploadComponent("proof", 1, 1, true), "15 Second recording of your run", "Upload a 15s Monkeytype recording. Drag your Discord window into view at the end.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
        
            await ctx.RespondWithModalAsync(modal);
        }

        [Command("supportmodal")]
        [Description("I am very motivated for no reason today")]
        public async Task supportmodal(SlashCommandContext ctx)
        {
            DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("supportmodal")
                                            .WithTitle("Create a support ticket...")
                                            .AddTextInput(new DiscordTextInputComponent("problemoverview", "Short overview of your problem", null, true, DiscordTextInputStyle.Short, 6, 25), "A short overview of your problem.")
                                            .AddTextInput(new DiscordTextInputComponent("problemdescription", "Describe the problem in full detail.", null, true, DiscordTextInputStyle.Paragraph, 150, 4000), "Describe your problem in best detail.")
                                            .AddFileUpload(new DiscordFileUploadComponent("attachments", 0, 10, false), "Additional attachments", "Not needed if you don't have any, but is beneficial.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
        
            await ctx.RespondWithModalAsync(modal);
        }

        [Command("exploitermodal")]
        [Description("WEEEE LAST MODAL AWWWWW:( Anyways suki will kill me if I make more modals (she hates popups)")]
        public async Task exploitermodal(SlashCommandContext ctx)
        {
            DiscordModalBuilder modal = new DiscordModalBuilder()
                                            .WithCustomId("exploitermodal")
                                            .WithTitle("Report an exploiter...")
                                            .AddTextInput(new DiscordTextInputComponent("reportedusername", "John Doe", "", true, DiscordTextInputStyle.Short), "Name or UserId of the reported user.", "Do not use the display name.")
                                            .AddTextInput(new DiscordTextInputComponent("reporterusername", "Jane Doe", "", true, DiscordTextInputStyle.Short), "Your username", "Not your display name either.")
                                            .AddFileUpload(new DiscordFileUploadComponent("attachments", 0, 10, false), "Proof of the user exploiting", "If you do not have any, leave this field out.")
                                            .AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", new List<DiscordCheckboxGroupOption> { new DiscordCheckboxGroupOption("agreed", "Agree to the ticket rules", "Abuse of the ticket system may lead to warnings or tickets being closed without notice.", false)}, 1, 1, true), "Ticket ToS");
            
            await ctx.RespondWithModalAsync(modal);
        }
    }
}
