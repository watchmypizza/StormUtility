using DiscordRPC;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using gamebot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stormtility.commands
{
    public class gamecommands : ApplicationCommandModule
    {
        public static async Task<bool> getAdmin(InteractionContext ctx)
        {
            if(!ctx.Member.PermissionsIn(ctx.Channel).HasPermission(Permissions.Administrator))
            {
                await ctx.DeferAsync(true);
                return false;
            }
            return true;
        }

        public class BanLogEntry
        {
            public string UserId { get; set; } = "";
            public string DisplayReason { get; set; } = "";
            public DateTime CreateTime { get; set; }
            public bool Active { get; set; }
        }

        public static async Task<bool> sendRequest(string requestType, long userId, string? reason = "")
        {
            if (requestType.ToLower() == "ban")
            {
                var API_KEY = Program.apiKey;
                var url = $"https://apis.roblox.com/cloud/v2/universes/9439461291/places/72498860452823/user-restrictions/{userId}";
                var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                var payload = new
                {
                    gameJoinRestriction = new
                    {
                        active = true,
                        privateReason = "Moderator action",
                        displayReason = $"{reason}",
                        excludeAltAccounts = true
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PatchAsync(url, content);

                return response.IsSuccessStatusCode;
            } else if (requestType.ToLower() == "unban")
            {
                var API_KEY = Program.apiKey;
                var url = $"https://apis.roblox.com/cloud/v2/universes/9439461291/places/72498860452823/user-restrictions/{userId}";
                var httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                var payload = new
                {
                    gameJoinRestriction = new
                    {
                        active = false
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await httpClient.PatchAsync(url, content);

                return response.IsSuccessStatusCode;
            }
            return false;
        }

        public static async Task<string> GetUserRestrictionLogsAsync(string? pageToken = null)
        {
            var apiKey = Program.apiKey;
            var url = $"https://apis.roblox.com/cloud/v2/universes/9439461291/user-restrictions:listLogs";

            if (!string.IsNullOrEmpty(pageToken))
                url += $"&pageToken={pageToken}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            return await response.Content.ReadAsStringAsync();
        }

        public static List<BanLogEntry> ParseBanLogs(string json)
        {
            var logsList = new List<BanLogEntry>();

            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("logs", out var logs))
                return logsList;

            foreach (var logEntry in logs.EnumerateArray())
            {
                bool active = logEntry.GetProperty("active").GetBoolean();

                if (!active)
                    continue;

                var userPath = logEntry.GetProperty("user").GetString() ?? "";
                var userId = userPath.Split('/').Length > 1 ? userPath.Split('/')[1] : "Unknown";

                var displayReason = logEntry.GetProperty("displayReason").GetString() ?? "No reason provided";

                var createTime = logEntry.GetProperty("createTime").GetDateTime();

                logsList.Add(new BanLogEntry
                {
                    UserId = userId,
                    DisplayReason = displayReason,
                    CreateTime = createTime,
                    Active = active
                });
            }

            return logsList;
        }

        public static async Task<bool> logAction(InteractionContext ctx, DiscordMember user, long userId, DiscordColor color, string action, string? reason = null, string ? duration = null)
        {
            ulong channel = Program.logChannel;
            var channels = await ctx.Interaction.Guild.GetChannelsAsync();

            if(!channels.Any(ch => ch.Id == channel)) {
                return false;
            }

            DiscordEmbed logEmbed;
            var logChannel = ctx.Interaction.Guild.GetChannel(channel);
            if (duration != null) {
                logEmbed = new DiscordEmbedBuilder
                {
                    Title = action,
                    Description = $"{user.Mention} {action} {userId} for `{reason}` and for a timeframe of `{duration}`",
                    Color=color
                };
            } else
            {
                logEmbed = new DiscordEmbedBuilder
                {
                    Title = action,
                    Description = $"{user.Mention} {action} {userId} for `{reason}`",
                    Color = color
                };
            }
            if(reason == null)
            {
                logEmbed = new DiscordEmbedBuilder
                {
                    Title = action,
                    Description = $"{user.Mention} {action} {userId}",
                    Color = color
                };
            }

            try
            {
                var message = await logChannel.SendMessageAsync(logEmbed);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            return true;
        } 

        [SlashCommandGroup("game", "Moderation commands related to the game")]
        public class gameCommands : ApplicationCommandModule
        {
            [SlashCommand("test", "Test")]
            public async Task test(InteractionContext ctx)
            {
                bool hasAdmin = await getAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{ctx.Client.GatewayVersion}").AsEphemeral());
            }

            [SlashCommand("ban", "Ban an user")]
            public async Task ban(InteractionContext ctx, [Option("userId", "User to ban")] long user, [Option("reason", "Reason for the kick")] string? reason = "No reason provided")
            {
                bool hasAdmin = await getAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }

                bool requestAnswer = await sendRequest("ban", user, reason);
                if (requestAnswer)
                {
                    var banEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Ban",
                        Description = $"{user} has been banned for `{reason}`",
                        Color = DiscordColor.Red
                    };
                    await ctx.CreateResponseAsync(banEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, user, DiscordColor.Red, "banned", reason);
                }
                else
                {
                    await ctx.CreateResponseAsync($"There was an error completing the request. {requestAnswer}", true);
                }
            }

            [SlashCommand("unban", "Unban an user")]
            public async Task unban(InteractionContext ctx, [Option("userId", "User to ban")] long user)
            {
                bool hasAdmin = await getAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }

                bool requestAnswer = await sendRequest("unban", user);
                if (requestAnswer)
                {
                    var unbanEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Ban",
                        Description = $"{user} has been unbanned.",
                        Color = DiscordColor.Green
                    };
                    await ctx.CreateResponseAsync(unbanEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, user, DiscordColor.Green, "unbanned");
                }
                else
                {
                    await ctx.CreateResponseAsync($"There was an error completing the request. {requestAnswer}", true);
                }
            }

            [SlashCommand("getbans", "Get a list of banned players")]
            public async Task getbans(InteractionContext ctx)
            {
                try
                {
                    bool hasAdmin = await getAdmin(ctx);
                    if (!hasAdmin)
                    {
                        await ctx.CreateResponseAsync("You do not have permission to use this command.", ephemeral: true);
                        return;
                    }

                    string banLog = await GetUserRestrictionLogsAsync();
                    var logs = ParseBanLogs(banLog);

                    if (logs.Count == 0)
                    {
                        await ctx.CreateResponseAsync("No active bans found.", ephemeral: true);
                        return;
                    }

                    var latestBans = logs.OrderByDescending(b => b.CreateTime).Take(10).ToList();

                    var embedBuilder = new DiscordEmbedBuilder()
                        .WithTitle("Ban logs")
                        .WithColor(DiscordColor.Red);

                    foreach (var ban in latestBans)
                    {
                        embedBuilder.AddField(
                            $"User ID: {ban.UserId}",
                            $"{ban.DisplayReason}\nBanned at: {ban.CreateTime.ToLocalTime():g}",
                            inline: false);
                    }

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embedBuilder).AsEphemeral());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            [SlashCommand("getuser", "Gets a specific user by their UserID")]
            public async Task getuser(InteractionContext ctx, [Option("userId", "User to get")] long user)
            {
                bool hasAdmin = await getAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }

                var API_KEY = Program.apiKey;
                var url = $"https://apis.roblox.com/cloud/v2/universes/9439461291/places/72498860452823/user-restrictions/{user}";
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var doc = JsonDocument.Parse(json);
                    bool isActive = doc.RootElement.GetProperty("gameJoinRestriction").GetProperty("active").GetBoolean();
                    string reason = doc.RootElement.GetProperty("gameJoinRestriction").GetProperty("displayReason").ToString();

                    if (isActive)
                    {
                        var activeEmbed = new DiscordEmbedBuilder
                        {
                            Description = $"{user} is banned for `{reason}`",
                            Color = DiscordColor.Red
                        };
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(activeEmbed).AsEphemeral());
                    }
                    else
                    {
                        var activeEmbed = new DiscordEmbedBuilder
                        {
                            Description = $"{user} is not banned.",
                            Color = DiscordColor.Green
                        };
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(activeEmbed).AsEphemeral());
                    }
                }
                else
                {
                    await ctx.CreateResponseAsync($"There was an error completing the request.", true);
                }
            }

            [SlashCommand("tempban", "Temporarily ban someone")]
            public async Task tempban(InteractionContext ctx, [Option("UserID", "The User ID for the user to ban.")] long userId, [Option("Reason", "The reason for the ban")] string reason, [Option("Duration", "The duration of the ban")] string duration)
            {
                bool isAdmin = await getAdmin(ctx);
                if(!isAdmin)
                {
                    return;
                }

                Regex regex = new Regex(@"(\d+)(y|mo|[dhms])");

                TimeSpan total = TimeSpan.Zero;
                int parsedTime = 0;

                foreach (Match match in regex.Matches(duration))
                {
                    int value = int.Parse(match.Groups[1].Value);
                    string unit = match.Groups[2].Value;

                    switch (unit)
                    {
                        case "y":
                            parsedTime = value * 365;
                            total += TimeSpan.FromDays(parsedTime);
                            break;
                        case "mo":
                            parsedTime = value * 30;
                            total += TimeSpan.FromDays(parsedTime);
                            break;
                        case "d":
                            total += TimeSpan.FromDays(value);
                            break;
                        case "h":
                            total += TimeSpan.FromHours(value);
                            break;
                        case "m":
                            total += TimeSpan.FromMinutes(value);
                            break;
                        case "s":
                            total += TimeSpan.FromSeconds(value);
                            break;
                        default:
                            await ctx.CreateResponseAsync("Invalid duration.", true);
                            return;
                    }
                }

                var apiRequest = $"https://apis.roblox.com/cloud/v2/universes/9439461291/user-restrictions/{userId}";
                var httpClient = new HttpClient();
                var payload = new
                {
                    path = $"universes/9439461291/user-restrictions/{userId}",
                    gameJoinRestriction = new
                    {
                        active = true,
                        duration = (int)total.TotalSeconds + "s",
                        privateReason = reason,
                        displayReason = reason,
                        excludeAltAccounts = false
                    }
                };
                var totalAmt = "";
                if (total.TotalSeconds >= 3.154e+7)
                {
                    totalAmt = $"~{Math.Round(total.TotalDays / 365, 2)} years";
                }
                else if (total.TotalSeconds >= 2628e+6)
                {
                    totalAmt = $"~{Math.Round(total.TotalDays / 30, 2)} months";
                }
                else if (total.TotalSeconds >= 86400)
                {
                    totalAmt = $"{Math.Round(total.TotalDays, 2)} days";
                }
                else if (total.TotalSeconds >= 3600)
                {
                    totalAmt = $"{Math.Round(total.TotalHours, 2)} hours";
                }
                else if (total.TotalSeconds >= 60)
                {
                    totalAmt = $"{Math.Round(total.TotalMinutes, 2)} minutes";
                }
                else
                {
                    totalAmt = $"{Math.Round(total.TotalSeconds, 2)} seconds";
                }
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                httpClient.DefaultRequestHeaders.Add("x-api-key", Program.apiKey);
                var request = await httpClient.PatchAsync(apiRequest, content);
                var responseContent = await request.Content.ReadAsStringAsync();
                Console.WriteLine(responseContent);
                if (request.IsSuccessStatusCode)
                {
                    var successEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Success!",
                        Description = $"{userId} has been banned for {totalAmt}",
                        Color=DiscordColor.Red
                    };
                    await ctx.CreateResponseAsync(successEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, userId, DiscordColor.Red, "temporarily banned", reason, totalAmt);
                    return;
                } else
                {
                    await ctx.CreateResponseAsync("An error has occured. Are we rate limited?", true);
                    return;
                }
            }

            [SlashCommand("kick", "Kick an user from the game")]
            public async Task kick(InteractionContext ctx, [Option("userId", "User to kick")] long user, [Option("reason", "Reason for the kick")] string? reason = "No reason provided")
            {
                bool hasAdmin = await getAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }
                var API_KEY = Program.apiKey;
                var url = $"https://apis.roblox.com/cloud/v2/universes/9439461291/user-restrictions/{user}";
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                var payload = new
                {
                    path = $"universes/9439461291/user-restrictions/{user}",
                    gameJoinRestriction = new
                    {
                        active = true,
                        privateReason = "Moderator action",
                        displayReason = $"{reason}",
                        excludeAltAccounts = false,
                        duration = "1s"
                    }
                };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await httpClient.PatchAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    var kickEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Kick",
                        Description = $"{user} has been kicked for `{reason}`",
                        Color = DiscordColor.Orange
                    };
                    await ctx.CreateResponseAsync(kickEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, user, DiscordColor.Orange, "kicked", reason);
                    return;
                }
                else
                {
                    await ctx.CreateResponseAsync($"There was an error completing the request. {response.StatusCode}", true);
                    return;
                }
            }
        }
    }
}
