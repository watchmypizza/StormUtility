using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Stormtility.Helpers;

namespace Stormtility.commands.EntertainmentCommands
{
    [Command("entertainment")]
    [Description("Some funny entertainment commands")]
    [AllowedProcessors(typeof(SlashCommandProcessor))]
    public class FunCommands
    {
        //i decided to comment my code to make it easier to read in the future, so yay
        // declerations
        private static Random RNG = new();
        private static Dictionary<(ulong, ulong), int> ShipCache = [];
        private static Dictionary<string, string> EightBallResponses = [];

        // headpats the user in an ephemeral by using FileStreams
        [Command("headpat")]
        [Description("Get headpats from the bot! :3")]
        public async Task Headpats(SlashCommandContext ctx)
        {
            await ctx.DeferResponseAsync(true);

            string fPath = "Assets/boykisser.PNG";
            FileStream fileStream = new(fPath, FileMode.Open, FileAccess.Read, FileShare.None);
            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddFile("headpats.png", fileStream));
            return;
        }

        // Determines the ship probability for 2 users
        [Command("ship")]
        [Description("Determines the probability of a ship between the 2 users")]
        public async Task Ship(SlashCommandContext ctx, [Parameter("User1")][Description("First user to pass")] DiscordUser member, [Parameter("User2")][Description("The second user to pass")] DiscordUser target)
        {
            await ctx.DeferResponseAsync(true);

            if (member.Id == target.Id) { await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("You cannot ship yourself buddy...")); return; }

            //check if user exists in cache lalready (fuck the type idgaf)
            if (ShipCache.TryGetValue((member.Id, target.Id), out int percentage))
            {
                await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"{member.Mention} and {target.Mention}'s ship probability is about {percentage}%"));
                return;
            }

            //get random percentage, we dont need one cryptographically secure
            int random = RNG.Next(0, 101);
            ShipCache.TryAdd((member.Id, target.Id), random);
            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"{member.Mention} and {target.Mention}'s ship probability is about {random}%"));
        }

        [Command("8ball")]
        [Description("Let 8ball decide.")]
        public async Task EightBall(SlashCommandContext ctx, string input)
        {
            await ctx.DeferResponseAsync(true);

            var embed = new DiscordEmbedBuilder
            {
                Title = "🎱 8ball 🎱",
                Color = DiscordColor.Red
            };

            embed.AddField("Question:", $"```{input}```", false);

            if (EightBallResponses.TryGetValue(input.ToLower(), out string? response))
            {
                embed.AddField("Response:", $"```{response}```", false);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            int rdmIdx = RNG.Next(EightBallResponsesList.responses.Length);
            string resp = EightBallResponsesList.responses.GetValue(rdmIdx) as string ?? "Error occured.";

            EightBallResponses.TryAdd(input.ToLower(), resp);
            embed.AddField("Response:", $"```{resp}```", false);
            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

            return;
        }

        [Command("sendmessage")]
        [Description("Send an semi-anonymous message to someone! The recipient will not be able to see who sent it.")]
        public async Task SendMessage(SlashCommandContext ctx)
        {
            bool isCooldown = CommandCooldown.CheckCooldown(ctx.Command.Name, ctx.Interaction.User.Id);
            if (!isCooldown)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"You are currently on cooldown for {CommandCooldown.GetRemainingTime(ctx.Command.Name, ctx.Interaction.User.Id)}").AsEphemeral());
                return;
            }

            var modal = new DiscordModalBuilder();
            modal.WithTitle("Send a message to someone.");
            modal.AddSelectMenu(new DiscordUserSelectComponent("recipient", "ilikepeanutbutter.", required: true), "Select a recipient");
            modal.AddTextInput(new DiscordTextInputComponent("message", "I like peanut butter", null, true, DiscordTextInputStyle.Paragraph), "Contents of the message");
            modal.AddFileUpload(new DiscordFileUploadComponent("fileuploads", maxValues: 10, isRequired: false), "Files to attach");
            modal.AddCheckboxGroup(new DiscordCheckboxGroupComponent("tosagree", [new("tosagree", "Agree to messaging ToS", "By checking this, you agree that the following content may be subject to moderation.")], required: true), "Terms of Service");
            modal.WithCustomId($"sendmessagemodal:{ctx.Command.Name}");

            await ctx.RespondWithModalAsync(modal);

            // DiscordChannel logChannel = await ctx.Client.GetChannelAsync(1533206494181462137);

            // var embed = new DiscordEmbedBuilder
            // {
            //     Title = "You have received a new message!",
            //     Color = DiscordColor.Red
            // };

            // embed.AddField("Content:", message, false);
            // embed.WithFooter("Anonymous to you • May be subject to moderation");

            // try
            // {
            //     await logChannel.SendMessageAsync($"User: {ctx.Interaction.User.Mention} sent ```{message}``` to {recipient.Mention}");
            //     await logChannel.SendMessageAsync(new DiscordMessageBuilder().WithContent("Message with embed:").AddEmbed(embed));
            //     await recipient.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed));
            //     await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Message sent!").AsEphemeral());
            //     return;
            // } catch(Exception e) {
            //     await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Something went wrong processing your request.").AsEphemeral());
            //     await Logger.LOGGER(e);
            //     cooldownDict[ctx.Interaction.User.Id] = DateTimeOffset.UtcNow;
            //     return;
            // }
        }


    }
}
