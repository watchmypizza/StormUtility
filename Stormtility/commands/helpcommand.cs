using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stormtility.commands
{
    public class helpcommand : ApplicationCommandModule
    {
        [SlashCommand("help", "Display a list of available commands and query info to every single one if needed.")]
        public async Task help(InteractionContext ctx, [Option("command", "Command to query info on")] string? command = null)
        {
            bool isAdmin = await gamecommands.getAdmin(ctx);

            if(!isAdmin)
            {
                return;
            }

            var helpEmbed = new DiscordEmbedBuilder
            {
                Title = "Commands",
                Color = DiscordColor.Gold
            };
            helpEmbed.AddField("/game", "ban <UserID> <Reason>\ngetbans\ntempban <UserID> <Reason> <Duration>\nunban <UserId>\ngetuser <UserId>\nkick <UserId> <Reason>\ntest");
            helpEmbed.AddField("Legend", "Purple: Admin+\nBlue: Moderator\nGreen: Trial");

            if(command == null)
            {
                await ctx.CreateResponseAsync(helpEmbed.Build(), true);
                return;
            }

            switch(command.ToLower())
            {
                case "/game ban":
                    var gameBanEmbed = new DiscordEmbedBuilder
                    {
                        Title = "/game ban",
                        Description = "This command takes 2 arguments.\nUserId: The UserId of the user you want to ban\nReason: The reason for the issued punishment",
                        Color = DiscordColor.Purple
                    };

                    await ctx.CreateResponseAsync(gameBanEmbed, true);
                    break;
                case "/game getbans":
                    var gameGetBans = new DiscordEmbedBuilder
                    {
                        Title = "/game getbans",
                        Description = "Gets a list of the most recent bans issued.\nThis command does not take any arguments.",
                        Color = DiscordColor.Green
                    };

                    await ctx.CreateResponseAsync(gameGetBans, true);
                    break;
                case "/game tempban":
                    var gameTempBan = new DiscordEmbedBuilder
                    {
                        Title = "/game tempban",
                        Description = "Ban an user for a defined time.\nThis command takes 3 arguments.\nUserId: The ID of the user to ban\nReason: The reason for the issued punishment.\nDuration: Duration of the ban in <s\\:m\\:d\\:mo\\:y> format.",
                        Color = DiscordColor.Purple
                    };

                    await ctx.CreateResponseAsync(gameTempBan, true);
                    break;
                case "/game unban":
                    var gameUnban = new DiscordEmbedBuilder
                    {
                        Title = "/game unban",
                        Description = "Unban an user that had a previously imposed punishment.\nThis command takes one argument.\nUserId: The ID of the user to unban.",
                        Color = DiscordColor.Purple
                    };

                    await ctx.CreateResponseAsync(gameUnban, true);
                    break;
                case "/game getuser":
                    var gameGetUser = new DiscordEmbedBuilder
                    {
                        Title = "/game getuser",
                        Description = "Get an user by their ID and check if they have an active punishment.\nThis command takes one argument.\nUserId: The ID of the user to look up.",
                        Color = DiscordColor.Green
                    };

                    await ctx.CreateResponseAsync(gameGetUser, true);
                    break;
                case "/game kick":
                    var gameKick = new DiscordEmbedBuilder
                    {
                        Title = "/game kick",
                        Description = "Kick an user from the game.\nThis command takes 2 arguments.\nUserId: The ID of the user to kick.\nReason: The reason for the issued punishment.",
                        Color = DiscordColor.Blue
                    };

                    await ctx.CreateResponseAsync(gameKick, true);
                    break;
                case "/game info":
                    var gameInfo = new DiscordEmbedBuilder
                    {
                        Title = "/game info",
                        Description = "This command is intended solely for testing purposes and displays the current gateway version.\nThis command takes no arguments."
                    };

                    await ctx.CreateResponseAsync(gameInfo, true);
                    break;
                default:
                    await ctx.CreateResponseAsync($"There is no command named `{command}`", true);
                    return;
            }
        }
    }
}
