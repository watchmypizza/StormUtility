using System.ComponentModel;
using System.Runtime.InteropServices;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using Stormtility.Helpers;

namespace Stormtility.commands.Booster_Commands {
    [Command("booster")]
    [Description("Booster only commands")]
    [AllowedProcessors(typeof(SlashCommandProcessor))]
    public class BoosterCommands
    {
        private static ulong BoosterRole { get; } = 1373145844303527937;
        private static Random RNG { get; } = new Random();
        private static Dictionary<ulong, int> gayStore = [];

        private static async Task<bool> CheckBoosterRole(SlashCommandContext ctx)
        {
            if (((DiscordMember)ctx.Interaction.User).Roles.Any(r => r.Id == 1370390655528079442)) return true;
            if (!((DiscordMember)ctx.Interaction.User).Roles.Any(r => r.Id == BoosterRole))
            {
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You have to be a server booster in order to use this command.").AsEphemeral());
                return false;
            }
            return true;
        }

        [Command("kill")]
        [Description("Mute someone for 2 minutes")]
        public static async Task Kill(SlashCommandContext ctx, [Parameter("User")][Description("User to kill")] DiscordMember member)
        {
            try
            {
                if (!await CheckBoosterRole(ctx))
                {
                    return;
                }
                if (!CommandCooldown.CheckCooldown(ctx.Command.Name, ctx.Interaction.User.Id))
                {
                    await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You are currently on cooldown.").AsEphemeral());
                }

                await member.TimeoutAsync(DateTimeOffset.UtcNow.AddMinutes(2), $"{ctx.Interaction.User.Username} ({ctx.Interaction.User.Id}) used /kill on {member.Username} ({member.Id})");
                CommandCooldown.AddCooldown(ctx.Command.Name, ctx.User.Id, 24);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{member.Mention} has been timed out successfully.").AsEphemeral());
                return;
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                CommandCooldown.RemoveCooldown(ctx.Command.Name, ctx.User.Id);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("An unknown error has occured.").AsEphemeral());
                return;
            }
        }

        [Command("gay")]
        [Description("Determines how gay a user is")]
        public static async Task GayTest(SlashCommandContext ctx, [Parameter("User")][Description("User to gay check")] DiscordMember member)
        {
            try
            {
                if (!await CheckBoosterRole(ctx))
                {
                    return;
                }

                if (!gayStore.TryGetValue(member.Id, out int gayMeter))
                {
                    gayMeter = RNG.Next(-1, 101);
                }

                if (ctx.Interaction.User.Id == 1444912002525954110)
                {
                    gayMeter = 100;
                }

                gayStore[member.Id] = gayMeter;
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{member.Mention} is {gayMeter}% gay.").AsEphemeral());
            }
            catch (Exception e)
            {
                await Logger.LOGGER(e);
                CommandCooldown.RemoveCooldown(ctx.Command.Name, ctx.User.Id);
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("An unknown error has occured.").AsEphemeral());
                return;
            }
        }

        [Command("hug")]
        [Description("Hug someone or make someone hug someone")]
        public async Task Hug(
            SlashCommandContext ctx,
            [Parameter("user")][Description("The user to hug:3")] DiscordUser user,
            [Parameter("hugger")][Description("The person that hugs the user:3")] DiscordUser? hugger = null
        )
        {
            if (!await CheckBoosterRole(ctx)) return;
            if (ctx.Interaction.User == user || user == hugger)
            {
                await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("You cannot hug yourself.").AsEphemeral());
                return;
            }

            await ctx.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{hugger?.Mention ?? ctx.Interaction.User.Mention} hugged {user.Mention} :3"));
            return;
        }

        // TODO:
        // /bump <user>: u have a crush on someone and we store the value of that, 12 hour cooldown, automatic renewals(
        // /marry <user> <user(optional)>
        // /kiss <user> <user(optional)>

        // STRUCTURE SQLITE BUMP: User (ulong) | Bumps (long) | HasAutomaticRenewalsForUserId (ulong) | LastRenewal (DateTime)
    }
}
