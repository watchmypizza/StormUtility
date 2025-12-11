using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility.commands.EconomyCommands
{
    public class EconomyCommands : ApplicationCommandModule
    {
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
                            ["lastclaimed"] = now,
                            ["job"] = "",
                            ["lastChanged"] = 0
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
                var entries = (JArray)json["economy"];

                JObject? foundEntry = null;
                JObject? activeUser = null;

                foreach (JObject entry in entries)
                {
                    if (entry.ContainsKey(targetUserId))
                    {
                        foundEntry = entry;
                        activeUser = entry[targetUserId] as JObject;
                        break;
                    }
                }

                if (foundEntry != null)
                {
                    if (targetUserId != ctx.User.Id.ToString())
                    {
                        var embedOther = new DiscordEmbedBuilder
                        {
                            Title = "Economy - Balance",
                            Color = DiscordColor.Green,
                        };
                        embedOther.AddField("Money", $"{activeUser["money"]} Stormy Coins");
                        embedOther.AddField("Streak", $"{activeUser["streak"]} day(s)");

                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embedOther).AsEphemeral());
                        return;
                    }

                    if (activeUser["job"] == null) activeUser["job"] = "";
                    if (activeUser["lastChanged"] == null) activeUser["lastChanged"] = 0;
                    if (activeUser["lastclaimed"] == null) activeUser["lastclaimed"] = DateTime.UtcNow;

                    File.WriteAllText("economy.json", json.ToString());

                    var embedSelf = new DiscordEmbedBuilder
                    {
                        Title = "Economy - Balance",
                        Color = DiscordColor.Green,
                    };
                    embedSelf.AddField("Money", $"{activeUser["money"]} Stormy Coins");
                    embedSelf.AddField("Streak", $"{activeUser["streak"]} day(s)");

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embedSelf).AsEphemeral());
                    return;
                }

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
                        ["lastclaimed"] = DateTime.UtcNow,
                        ["job"] = "",
                        ["lastChanged"] = 0
                    }
                };

                entries.Add(newUser);
                activeUser = newUser[targetUserId] as JObject;

                File.WriteAllText("economy.json", json.ToString());

                var embedNew = new DiscordEmbedBuilder
                {
                    Title = "Economy - Balance",
                    Color = DiscordColor.Green,
                };
                embedNew.AddField("Money", $"{activeUser["money"]} Stormy Coins");
                embedNew.AddField("Streak", $"{activeUser["streak"]} day(s)");

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embedNew).AsEphemeral());
            }

            [SlashCommand("setmoney", "Set money, admin only command")]
            public async Task setmoney(InteractionContext ctx, [Option("Amount", "The amount to set it to")] double amt, [Option("User", "The user to modify the amount of money")] DiscordUser? user = null)
            {
                if (!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator))
                {
                    var PermissionErrorEmbed = new DiscordEmbedBuilder
                    {
                        Title = "404 - No Permissions",
                        Description = "You do not have the necessary permissions to use this command.",
                        Color = DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(PermissionErrorEmbed).AsEphemeral());

                    return;
                }

                var targetUserId = user?.Id.ToString() ?? ctx.User.Id.ToString();
                var targetMember = (DiscordMember)user ?? ctx.Member;

                if (amt > 9007199254740991.00)
                {
                    var overflowEmbed = new DiscordEmbedBuilder
                    {
                        Title = "500 - Overflow",
                        Description = "You put too much in a water kettle, now it overflowed. Please try again with a smaller number",
                        Color = DiscordColor.Orange
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(overflowEmbed).AsEphemeral());

                    return;
                }

                var jsonText = File.ReadAllText("economy.json");
                var json = JObject.Parse(jsonText);
                var entries = (JArray)json!["economy"];

                bool found = false;

                JObject? activeUserData = null;

                foreach (JObject entry in entries)
                {
                    if (entry.ContainsKey(targetUserId))
                    {
                        activeUserData = entry[targetUserId] as JObject;
                        found = true;
                        break;
                    }
                }

                double amountRounded = Math.Round(amt);
                int amount = (int)amountRounded;

                if (found)
                {
                    int money = activeUserData!["money"].Value<int>();

                    money = amount;

                    activeUserData["money"] = money;
                }
                else
                {
                    JObject newUser = new JObject
                    {
                        [targetUserId] = new JObject
                        {
                            ["money"] = amount,
                            ["streak"] = 0,
                            ["lastclaimed"] = 0,
                            ["job"] = "",
                            ["lastChanged"] = 0
                        }
                    };

                    entries.Add(newUser);
                }

                DiscordEmbed success = new DiscordEmbedBuilder
                {
                    Title = "Economy - Admin Commands",
                    Description = $"Successfully set {targetMember.Mention}'s money to {amountRounded} Stormy Coin(s)",
                    Color = DiscordColor.Green
                };

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(success).AsEphemeral());

                File.WriteAllText("economy.json", json.ToString());
            }

            [SlashCommand("work", "Get a job.")]
            public async Task work(InteractionContext ctx, [Option("Job", "Yea, I'm aware that j*b is a very bad word.. and I couldn't care less.", autocomplete: true)][Autocomplete(typeof(JobAutoCompleteProvider))] string job)
            {
                try
                {
                    if (vars.nitroOnlyTest)
                    {
                        bool booster = ctx.Member.Roles.Any(r => r.Id == 1373145844303527937);
                        if (!booster && !ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator))
                        {
                            DiscordEmbed permissionsEmbed = new DiscordEmbedBuilder
                            {
                                Title = "404 - No Permissions",
                                Description = "You do not have the necessary permissions to use this command.",
                                Color = DiscordColor.Red
                            };

                            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(permissionsEmbed).AsEphemeral());
                            return;
                        }
                    }

                    var jsonText = File.ReadAllText("jobs.json");
                    var jobRoot = JObject.Parse(jsonText);
                    var jobsDict = (JObject)jobRoot["jobs"];

                    job = job.ToLower();

                    if (!jobsDict.ContainsKey(job))
                    {
                        DiscordEmbed jobNotExist = new DiscordEmbedBuilder
                        {
                            Title = "500 - Job doesn't exist",
                            Description = "The job you selected does not exist.",
                            Color = DiscordColor.Red
                        };

                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                            .AddEmbed(jobNotExist)
                            .AsEphemeral());
                        return;
                    }

                    var userJson = File.ReadAllText("economy.json");
                    var userRoot = JObject.Parse(userJson);
                    var userDict = (JArray)userRoot!["economy"];

                    JObject? userWrapper = userDict
                        .OfType<JObject>()
                        .FirstOrDefault(o => o.ContainsKey(ctx.User.Id.ToString()));

                    if (userWrapper == null)
                    {
                        DiscordEmbed userNotFound = new DiscordEmbedBuilder
                        {
                            Title = "500 - Please create a userprofile first.",
                            Description = "In order to do that please execute /economy daily or /economy balance first.",
                            Color = DiscordColor.Red
                        };

                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(userNotFound).AsEphemeral());
                        return;
                    }
                    else
                    {
                        // Implementing saving logic here later when I am back home.
                        // VERY IMPORTANT!!!: Do not forget to add lastChanged so the user can't spam job changes.
                        var usrIdStr = ctx.User.Id.ToString();
                        var userData = (JObject)userWrapper[usrIdStr];
                        DateTime lastChanged;

                        if (userData["lastChanged"] == null || userData["lastChanged"].Type != JTokenType.Date)
                        {
                            lastChanged = DateTime.MinValue;
                        }
                        else
                        {
                            lastChanged = userData["lastChanged"].ToObject<DateTime>();
                        }

                        TimeSpan cooldown = TimeSpan.FromHours(24);
                        bool changeJobInvalid = DateTime.UtcNow - lastChanged < cooldown;

                        var now = lastChanged.CompareTo(DateTime.UtcNow);
                        if (changeJobInvalid)
                        {
                            DiscordEmbed changeTooEarly = new DiscordEmbedBuilder
                            {
                                Title = "404 - You change jobs too quickly!",
                                Description = "Please calm down a bit and try again later.",
                                Color = DiscordColor.Red
                            };

                            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(changeTooEarly).AsEphemeral());
                            return;
                        }

                        userData["job"] = job;
                        userData["lastChanged"] = DateTime.UtcNow;

                        DiscordEmbed successEmbed = new DiscordEmbedBuilder
                        {
                            Title = "200 - Success",
                            Description = $"Successfully changed your job to `{job}`.",
                            Color = DiscordColor.Green
                        };

                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(successEmbed).AsEphemeral());
                    }
                    File.WriteAllText("economy.json", userRoot.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }
}
