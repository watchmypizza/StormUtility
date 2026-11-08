using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using Stormtility.Helpers;

namespace Stormtility.commands.Link_Commands {
    [Command("link")]
    [Description("Link commands for links duh")]
    public class LinkCommands {
        private static async Task<bool> PermissionCheck(SlashCommandContext ctx)
        {
            if (!ctx.Member!.PermissionsIn(ctx.Interaction.Channel).HasPermission(DiscordPermission.ManageMessages))
            {
                await ctx.RespondAsync("You do not have permissions to use this command", true);
                return false;
            }
            return true;
        }

        [Command("add")]
        [Description("Add a link to the link blacklist")]
        public static async Task AddLink(SlashCommandContext ctx, [Parameter("link")][Description("Link to add")] string link)
        {
            if (!await PermissionCheck(ctx)) return;

            FileHelper helper = new();
            await helper.Insert("linkblacklist.txt", link);

            var embed = new DiscordEmbedBuilder()
            {
                Title = "Success",
                Description = $"{link} has been added to the blacklist.",
                Color = DiscordColor.Green
            };

            await LinkHelper.RefreshCache();

            await ctx.RespondAsync(embed, true);
            return;
        }

        [Command("remove")]
        [Description("Remove a link from the blacklist")]
        public static async Task RemoveLink(SlashCommandContext ctx, [Parameter("link")][Description("Link to remove")] string link)
        {
            if (!await PermissionCheck(ctx)) return;

            FileHelper helper = new();
            await helper.Remove("linkblacklist.txt", link);

            var embed = new DiscordEmbedBuilder()
            {
                Title = "Success",
                Description = $"{link} has been removed from the blacklist.",
                Color = DiscordColor.Red
            };

            await LinkHelper.RefreshCache();

            await ctx.RespondAsync(embed, true);
            return;
        }

        [Command("get")]
        [Description("Get the links in the blacklist")]
        public static async Task GetLinks(SlashCommandContext ctx)
        {
            if (!await PermissionCheck(ctx)) return;

            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Preparing file...").AsEphemeral());
            FileStream fileStream = new("linkblacklist.txt", FileMode.OpenOrCreate, FileAccess.Read, FileShare.None, 4096, useAsync: true);

            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddFile(fileStream).WithContent("File ready!"));
            return;
        }
    }
}
