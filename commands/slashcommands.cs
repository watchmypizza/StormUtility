using DiscordRPC;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Stormtility.commands
{
    public class slashcommands : ApplicationCommandModule
    {
        [SlashCommand("blacklist", "Blacklist someone")]
        public async Task blacklist(InteractionContext ctx, [Option("Member", "Specify the member to blacklist")] DiscordUser user, [Option("Reason", "The reason for the blacklist")] string reason)
        {
            vars.commandTriggered = true;

            var member = (DiscordMember) user;

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            var jsonText = File.ReadAllText("blacklists.json");
            var json = JObject.Parse(jsonText);

            var entry = new JObject
            {
                [member.Id.ToString()] = new JObject
                {
                    ["reason"] = reason
                }
            };

            ((JArray)json["blacklist"]).Add(entry);

            File.WriteAllText("blacklists.json", json.ToString());

            foreach (var role in member.Roles)
            {
                if (role.Id == 1397404869702320260)
                {
                    var blacklisted = new DiscordEmbedBuilder
                    {
                        Title = "Blacklist",
                        Color = DiscordColor.Red,
                        Description = $"{member.Mention} is already blacklisted."
                    };
                    await ctx.DeferAsync(ephemeral:true);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(blacklisted));
                    return;
                }
            }

            var mod_log_ch = ctx.Guild.GetChannel(vars.modLogs);

            var message = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Red,
                Description = $"Successfully blacklisted {member.Mention}."
            };

            var logEmbed = new DiscordEmbedBuilder
            {
                Title = "Blacklist",
                Color = DiscordColor.Red,
                Description = $"{member.Mention} has been blacklisted for `{reason}` by {ctx.Interaction.User.Mention}"
            };

            await ctx.DeferAsync(ephemeral:true);
            await mod_log_ch.SendMessageAsync(logEmbed);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await member.GrantRoleAsync(ctx.Guild.GetRole(1397404869702320260));
            vars.commandTriggered = false;
        }

        [SlashCommand("unblacklist", "Unblacklist someone that is blacklisted")]
        public async Task unblacklist(InteractionContext ctx, [Option("Member", "Specify the member to unblacklist")] DiscordUser user)
        {
            vars.commandTriggered = true;
            var member = (DiscordMember)user;
            bool roleFound = false;

            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            var jsonText = File.ReadAllText("blacklists.json");
            var json = JObject.Parse(jsonText);

            var blacklist = (JArray)json["blacklist"];

            string targetId = member.Id.ToString();

            JObject toRemove = null;

            foreach(JObject entry in blacklist)
            {
                if (entry.ContainsKey(targetId))
                {
                    toRemove = entry;
                    break;
                }
            }

            if(toRemove != null)
            {
                blacklist.Remove(toRemove);
                File.WriteAllText("blacklists.json", json.ToString());
            } else
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Green,
                    Description = $"{member.Mention} is not blacklisted or is not in the blacklists.json file.",
                };
                embed.WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto));
                await ctx.DeferAsync(ephemeral: true);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            foreach (var role in member.Roles)
            {
                if (role.Id == 1397404869702320260)
                {
                    roleFound = true;
                }
            }

            if(!roleFound)
            {
                var notblacklisted = new DiscordEmbedBuilder
                {
                    Title = "Blacklist",
                    Color = DiscordColor.Red,
                    Description = $"{member.Mention} is not blacklisted."
                };
                await ctx.DeferAsync(ephemeral: true);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(notblacklisted));
                return;
            }

            var mod_log_ch = ctx.Guild.GetChannel(vars.modLogs);

            var message = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Red,
                Description = $"Successfully unblacklisted {member.Mention}."
            };
            var logEmbed = new DiscordEmbedBuilder
            {
                Title = "Blacklisted",
                Color = DiscordColor.Green,
                Description = $"{member.Mention} has been unblacklisted by {ctx.Interaction.User.Mention}."
            };

            await ctx.DeferAsync(ephemeral:true);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(message));
            await mod_log_ch.SendMessageAsync(logEmbed);
            await member.RevokeRoleAsync(ctx.Guild.GetRole(1397404869702320260));
            vars.commandTriggered = false;
        }

        [SlashCommand("checkblacklist", "Check whether a user is blacklisted or not.")]
        public async Task checkblacklist(InteractionContext ctx, [Option("User", "The user to check")] DiscordUser user)
        {
            var member = (DiscordMember)user;
            var IDString = member.Id.ToString();
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.ModerateMembers))
            {
                return;
            }

            var jsonText = File.ReadAllText("blacklists.json");
            var json = JObject.Parse(jsonText);
            var blacklist = (JArray)json["blacklist"];
            bool found = false;
            try
            {
                foreach (JObject entry in blacklist)
                {
                    if (entry.ContainsKey(IDString))
                    {
                        var reason = entry[IDString]["reason"].ToString();
                        var embed = new DiscordEmbedBuilder
                        {
                            Title = "Blacklist",
                            Color = DiscordColor.Red,
                            Description = $"{member.Mention} is blacklisted for `{reason}`."
                        };
                        await ctx.DeferAsync(ephemeral: true);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Blacklist",
                        Color = DiscordColor.Green,
                        Description = $"{member.Mention} is not blacklisted or is not in the blacklists.json file.",
                    };
                    embed.WithThumbnail(member.GetAvatarUrl(ImageFormat.Auto));
                    await ctx.DeferAsync(ephemeral: true);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [SlashCommand("mutecounter", "Check how many times you have been muted for saying ts = this")]
        public async Task mutecounter(InteractionContext ctx)
        {
            await ctx.DeferAsync(ephemeral: true);

            int muteCount = 0;

            try
            {
                var jsonText = File.ReadAllText("mutecounter.json");
                var json = JObject.Parse(jsonText);
                var mutes = (JArray)json["mutes"];

                if (mutes != null)
                {
                    string userId = ctx.User.Id.ToString();

                    foreach (JObject entry in mutes)
                    {
                        if (entry.ContainsKey(userId))
                        {
                            muteCount = entry[userId]["mutes"].Value<int>();
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("mutecounter.json missing 'mutes' array.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading mutecounter.json: {ex}");
            }

            var embed = new DiscordEmbedBuilder
            {
                Title = "Mute Counter",
                Color = DiscordColor.Purple,
                Description = $"You have been muted {muteCount} time{(muteCount == 1 ? "" : "s")}."
            };

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

        [SlashCommand("debug", "Toggle debug mode")]
        public async Task debugMode(InteractionContext ctx)
        {
            if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.All))
            {
                var errorEmbed = new DiscordEmbedBuilder
                {
                    Title = "404 - No Permissions",
                    Description = "You do not have the necessary permissions to use this command.",
                    Color = DiscordColor.Red
                };
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(errorEmbed).AsEphemeral());
                return;
            }

            if (!vars.debugMode)
            {
                vars.debugMode = true;
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Enabled debug mode").AsEphemeral());
                return;
            } else
            {
                vars.debugMode = false;
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Disabled debug mode").AsEphemeral());
                return;
            }
        }
    }
}
