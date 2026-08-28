using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Stormtility.commands.UtilityCommands
{
    public class HelpCommand
    {
        [SlashCommand("help", "Receive a list of commands that are available")]
        [Command("help")]
        [Description("Receive a list of commands that are available.")]
        public async Task Help(SlashCommandContext ctx, [Parameter("command")][Description("More information about a certain command")] string? command = null)
        {
            if (command == null)
            {
                var helpEmbed = new DiscordEmbedBuilder
                {
                    Title = "Available Commands",
                    Color = DiscordColor.Gold
                };

                helpEmbed.AddField("/ticket", "setup\ntranscript <TicketID>\nclose\ndelete\nclaim\nadd <user>\nremove <user>\nstaff\naddrole <role>\nremoverole <role>", false);
                helpEmbed.AddField("/antiraid", "slowmode <s ; m ; h>\nmassban <s ; m ; h>\nlift\ntest", false);
                helpEmbed.AddField("General commands", "/blacklist <user> <reason>\n/unblacklist <user>\n/checkblacklist <user>\n/mutecounter", false);
                helpEmbed.AddField("Legend", "Purple: Administrator\nBlue: Moderators\nPink: Members");

                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(helpEmbed).AsEphemeral());
                return;
            }

            switch (command.ToLower())
            {
                case "/ticket setup":

                    var ticketSetup = new DiscordEmbedBuilder
                    {
                        Title = "/ticket setup",
                        Description = "This command creates the multiselect interface for users to open tickets.",
                        Color = DiscordColor.Purple
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketSetup).AsEphemeral());
                    break;
                case "/ticket transcript":
                    var ticketTranscript = new DiscordEmbedBuilder
                    {
                        Title = "/ticket transcript <TicketID>",
                        Description = "See the automatically generated transcript after a ticket gets deleted or closes.",
                        Color = DiscordColor.Blue
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketTranscript).AsEphemeral());
                    break;
                case "/ticket close":
                    var ticketClose = new DiscordEmbedBuilder
                    {
                        Title = "/ticket close",
                        Description = "Close the ticket you are in currently. This command only works inside of a ticket channel.",
                        Color = DiscordColor.Blue
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketClose).AsEphemeral());
                    break;
                case "/ticket delete":
                    var ticketDelete = new DiscordEmbedBuilder
                    {
                        Title = "/ticket delete",
                        Description = "Delete the ticket you are in currently. This does not prevent transcripts.",
                        Color = DiscordColor.Blue
                    };

                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketDelete).AsEphemeral());
                    break;
                case "/ticket claim":
                    var ticketClaim = new DiscordEmbedBuilder
                    {
                        Title = "/ticket claim",
                        Description = "Claim the ticket you are in currently. This command is normally not needed because of the autoclaim feature.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketClaim).AsEphemeral());
                    break;
                case "/ticket add":
                    var ticketAdd = new DiscordEmbedBuilder
                    {
                        Title = "/ticket add <user>",
                        Description = "Add a user to the current ticket.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketAdd).AsEphemeral());
                    break;
                case "/ticket remove":
                    var ticketRemove = new DiscordEmbedBuilder
                    {
                        Title = "/ticket remove <user>",
                        Description = "Remove a user from the current ticket.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketRemove).AsEphemeral());
                    break;
                case "/ticket staff":
                    var ticketStaff = new DiscordEmbedBuilder
                    {
                        Title = "/ticket staff",
                        Description = "Create a plain ticket without mentioning the ticket ping role.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketStaff).AsEphemeral());
                    break;
                case "/ticket addrole":
                    var ticketAddRole = new DiscordEmbedBuilder
                    {
                        Title = "/ticket addrole <role>",
                        Description = "Add a role and all its members to a ticket.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketAddRole).AsEphemeral());
                    break;
                case "/ticket removerole":
                    var ticketRemoveRole = new DiscordEmbedBuilder
                    {
                        Title = "/ticket removerole <role>",
                        Description = "Remove a role and all its members from the current ticket.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(ticketRemoveRole).AsEphemeral());
                    break;
                case "/antiraid slowmode":
                    var antiraidSlowmode = new DiscordEmbedBuilder
                    {
                        Title = "/antiraid slowmode <s ; m ; h>",
                        Description = "Specify a custom slowmode for the channel you are currently in.\nFormat is seconds, minutes or hours.",
                        Color = DiscordColor.Purple
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(antiraidSlowmode).AsEphemeral());
                    break;
                case "/antiraid massban":
                    var antiraidMassban = new DiscordEmbedBuilder
                    {
                        Title = "/antiraid massban <s ; m ; h>",
                        Description = "Ban members that joined in that timeframe.\nFormat is seconds, minutes or hours",
                        Color = DiscordColor.Purple
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(antiraidMassban).AsEphemeral());
                    break;
                case "/antiraid lift":
                    var antiraidLift = new DiscordEmbedBuilder
                    {
                        Title = "/antiraid lift",
                        Description = "If the raid mode has been enabled, you can remove slowmode of all the channels that have been affected by the antiraid measures automatically.",
                        Color = DiscordColor.Purple
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(antiraidLift).AsEphemeral());
                    break;
                case "/antiraid test":
                    var antiraidTest = new DiscordEmbedBuilder
                    {
                        Title = "/antiraid test",
                        Description = "Test the antiraid measures by applying slowmode to all antiraid protected channels and removing it shortly after again.",
                        Color = DiscordColor.Purple
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(antiraidTest).AsEphemeral());
                    break;
                case "/blacklist":
                    var blacklist = new DiscordEmbedBuilder
                    {
                        Title = "/blacklist <user> <reason>",
                        Description = "The actual reason for the bot's existence. Blacklist someone that broke a or multiple server rule(s).",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(blacklist).AsEphemeral());
                    break;
                case "/unblacklist":
                    var unblacklist = new DiscordEmbedBuilder
                    {
                        Title = "/unblacklist <user>",
                        Description = "Unblacklist someone that had been blacklisted.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(unblacklist).AsEphemeral());
                    break;
                case "/checkblacklist":
                    var checkBlacklist = new DiscordEmbedBuilder
                    {
                        Title = "/checkblacklist <user>",
                        Description = "Check the reason someone was blacklisted.",
                        Color = DiscordColor.Blue
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(checkBlacklist).AsEphemeral());
                    break;
                case "/mutecounter":
                    var muteCounter = new DiscordEmbedBuilder
                    {
                        Title = "/mutecounter",
                        Description = "Check how many times you have been muted for saying ts = this or similiar",
                        Color = new DiscordColor(192, 129, 119)
                    };
                    await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(muteCounter).AsEphemeral());
                    break;
            }
        }
    }
}
