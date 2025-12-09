using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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
                Description = $"{member.Mention} has been blacklisted for `{reason}`"
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
                Description = $"{member.Mention} has been unblacklisted."
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

        [SlashCommandGroup("economy", "Economy commands")]
        public class economy : ApplicationCommandModule
        {
            [SlashCommand("daily", "Daily reward")]
            public async Task daily(InteractionContext ctx)
            {
                bool boosterRole = ctx.Member.Roles.Any(r => r.Id == 1373145844303527937);

                if (vars.nitroOnlyTest)
                {
                    if (!boosterRole && !ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator))
                    {
                        Console.WriteLine("Not a booster");
                        var Boosterembed = new DiscordEmbedBuilder
                        {
                            Title = "Booster only",
                            Description = "This function is currently only available for <@&1373145844303527937> and will become available for everyone else later on!"
                        };
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(Boosterembed).AsEphemeral());
                        return;
                    }
                }

                await ctx.DeferAsync(ephemeral: true);

                string userId = ctx.User.Id.ToString();
                string economyPath = "./economy.json";

                var jsonText = File.ReadAllText(economyPath);
                var json = JObject.Parse(jsonText);
                var entries = (JArray)json["economy"];

                JObject? foundEntry = null;
                JObject? activeUserData = null;

                foreach (JObject entry in entries)
                {
                    if (entry.ContainsKey(userId))
                    {
                        foundEntry = entry;
                        activeUserData = entry[userId] as JObject;
                        break;
                    }
                }

                DateTime now = DateTime.UtcNow;

                if (foundEntry != null)
                {
                    DateTime lastClaimed = foundEntry[userId]?["lastclaimed"]?.Value<DateTime>()
                                           ?? DateTime.MinValue;

                    if (now < lastClaimed.AddDays(1))
                    {
                        var remaining = lastClaimed.AddDays(1) - now;

                        var lastClaimedEmbed = new DiscordEmbedBuilder
                        {
                            Title = "Uh oh..",
                            Description = $"You must wait {remaining.Hours}h {remaining.Minutes}m before claiming again.",
                            Color = DiscordColor.Red
                        };

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(lastClaimedEmbed));
                        return;
                    }

                    if (now > lastClaimed.AddDays(2))
                    {
                        int streakbreak = foundEntry[userId]!["streak"]!.Value<int>();
                        streakbreak = 0;
                        foundEntry[userId]!["streak"] = streakbreak;

                        Console.WriteLine("Removed user's streak.");
                    }

                    int money = foundEntry[userId]!["money"]!.Value<int>();
                    int streak = foundEntry[userId]!["streak"]!.Value<int>();

                    streak += 1;
                    money += 125;

                    foundEntry[userId]!["money"] = money;
                    foundEntry[userId]!["streak"] = streak;
                    foundEntry[userId]!["lastclaimed"] = now;

                    Console.WriteLine($"{money}, {streak}, {ctx.User.Username}");
                }
                else
                {
                    var newEntry = new JObject
                    {
                        [userId] = new JObject
                        {
                            ["money"] = 125,
                            ["streak"] = 1,
                            ["lastclaimed"] = now
                        }
                    };

                    entries.Add(newEntry);
                    activeUserData = newEntry[userId] as JObject;
                }

                File.WriteAllText(economyPath, json.ToString(Formatting.Indented));

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Daily Bonus",
                    Description = $"You have successfully claimed your daily bonus of 125 Stormy Coins! Your new balance is {activeUserData!["money"]} with a streak of {activeUserData!["streak"]} day(s)!",
                    Color = DiscordColor.Green
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }

            [SlashCommand("balance", "Check your current balance!")]
            public async Task balance(InteractionContext ctx, [Option("User", "View the balance of a user")] DiscordUser? user = null)
            {
                if (vars.nitroOnlyTest)
                {
                    bool boosterRole = ctx.Member.Roles.Any(r => r.Id == 1373145844303527937);

                    if (!boosterRole && !ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator))
                    {
                        Console.WriteLine("Not a booster");
                        var Boosterembed = new DiscordEmbedBuilder
                        {
                            Title = "Booster only",
                            Description = "This function is currently only available for <@&1373145844303527937> and will become available for everyone else later on!"
                        };
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(Boosterembed).AsEphemeral());
                        return;
                    }
                }

                var targetUserId = user?.Id.ToString() ?? ctx.User.Id.ToString();

                var jsonText = File.ReadAllText("economy.json");
                var json = JObject.Parse(jsonText);
                var entries = (JArray)json!["economy"];

                JObject? foundEntry = null;
                JObject? activeUser = null;

                foreach(JObject entry in entries)
                {
                    if(entry.ContainsKey(targetUserId))
                    {
                        foundEntry = entry;
                        activeUser = entry[targetUserId] as JObject;
                        break;
                    }
                }

                if (foundEntry != null)
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Economy - Balance",
                        Color = DiscordColor.Green,
                    };
                    embed.AddField("Money", $"{activeUser!["money"]} Stormy Coins", inline: false);
                    embed.AddField("Streak", $"{activeUser!["streak"]} day(s)", inline: false);

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                    return;
                }
                else
                {
                    if (targetUserId != ctx.User.Id.ToString())
                    {
                        var ERRORembed = new DiscordEmbedBuilder
                        {
                            Title = "404 - Nothing here",
                            Description = "This user does not have any economy data.",
                            Color = DiscordColor.Red
                        };

                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(ERRORembed).AsEphemeral());

                        return;
                    }

                    var newUser = new JObject
                    {
                        [targetUserId] = new JObject
                        {
                            ["money"] = 0,
                            ["streak"] = 0,
                        }
                    };

                    entries.Add(newUser);
                    activeUser = newUser[targetUserId] as JObject;

                    File.WriteAllText("economy.json", json.ToString());

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Economy - Balance",
                        Color = DiscordColor.Green,
                    };
                    embed.AddField("Money", $"{activeUser!["money"]} Stormy Coins", inline: false);
                    embed.AddField("Streak", $"{activeUser!["streak"]} day(s)", inline: false);

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                    return;
                }
            }
        }
    }
}
