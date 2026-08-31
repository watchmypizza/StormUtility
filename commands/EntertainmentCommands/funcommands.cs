using System.ComponentModel;
using System.Formats.Tar;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Microsoft.Win32.SafeHandles;

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
    }
}
