using DiscordRPC;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using gamebot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stormtility.commands
{
    public class Gamecommands : ApplicationCommandModule
    {
        public static async Task<bool> GetAdmin(InteractionContext ctx)
        {
            bool isStaff = ctx.Member.Roles.Any(r => r.Id == Program.staffId);
            bool isOwner = ctx.User.Id == 806333974795386923;

            if (isStaff || isOwner)
            {
                return true;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Unauthorized.").AsEphemeral(true));
            return false;
        }

        public class BanLogEntry
        {
            public string UserId { get; set; } = "";
            public string DisplayReason { get; set; } = "";
            public DateTime CreateTime { get; set; }
            public bool Active { get; set; }
            public static ulong placeId { get; } = 119475035626185;
            public static ulong universeId { get; set; } = 9439461291;
            public string User { get; internal set; }
        }

        private static readonly HttpClientHandler handler = new HttpClientHandler { UseCookies = true };
        private static readonly HttpClient httpClient = new HttpClient(handler);

        public static async Task<long> GetUser(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || username.Contains("\""))
                {
                    return 0;
                }
                var url = $"https://users.roblox.com/v1/usernames/users";
                var body = new
                {
                    usernames = new Collection<string>
                    {
                      username
                    },
                    excludeBannedUsers = true
                };

                var payload = JsonSerializer.Serialize(body);
                var ctn = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(url, ctn);

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Error: {response.StatusCode}");
                    Console.WriteLine($"CRITICAL: {content.ToString()}");
                    return 0;
                }

                var dataArray = doc.RootElement.GetProperty("data");

                if (dataArray.GetArrayLength() > 0)
                {
                    return dataArray[0].GetProperty("id").GetInt64();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                Console.WriteLine($"Error: {e}");
                return 0;
            }
            return 0;
        }

        public static async Task<bool> SendRequest(string requestType, string username, string? reason = "")
        {
            CancellationToken cancellationToken = new();
            long.TryParse(username, out long userId);
            if(userId == 0) {
                userId = await GetUser(username).WaitAsync(cancellationToken);
            }

            if (requestType.ToLower() == "ban")
            {
                try {
                    var API_KEY = Program.apiKey;
                    var url = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}/user-restrictions/{userId}";
                    var httpClient = new HttpClient();

                    httpClient.DefaultRequestHeaders.Add("x-api-key", API_KEY);
                    var payload = new
                    {
                        gameJoinRestriction = new
                        {
                            active = true,
                            privateReason = "Moderator action",
                            displayReason = $"{reason}",
                            excludeAltAccounts = false,
                            ApplyDeviceBlock = true
                        }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PatchAsync(url, content);

                    await refBan.writeToFile(userId);

                    return response.IsSuccessStatusCode;
                } catch (Exception e)
                {
                    return false;
                }
            } else if (requestType.ToLower() == "unban")
            {
                try {
                    var API_KEY = Program.apiKey;
                    var url = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}/user-restrictions/{userId}";
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

                    if(response.IsSuccessStatusCode)
                    {
                        await refBan.removeFromFile(userId);
                    }

                    return response.IsSuccessStatusCode;
                } catch (Exception e)
                {
                    return false;
                }
            }
            return false;
        }

        public static async Task<string> GetUserRestrictionLogsAsync(string? pageToken = null)
        {
            var apiKey = Program.apiKey;
            var url = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}/user-restrictions";

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

            if (!root.TryGetProperty("userRestrictions", out var logs))
                return logsList;

            foreach (var logEntry in logs.EnumerateArray())
            {
                var restriction = logEntry.GetProperty("gameJoinRestriction");

                bool active = restriction.GetProperty("active").GetBoolean();

                if (!active)
                    continue;

                var userPath = logEntry.GetProperty("user").GetString() ?? "";
                var userId = userPath.Split('/').Length > 1 ? userPath.Split('/')[1] : "Unknown";

                var displayReason = restriction.GetProperty("displayReason").GetString() ?? "No reason provided";

                var createTime = restriction.GetProperty("startTime").GetDateTime();

                logsList.Add(new BanLogEntry
                {
                    User = userPath,
                    UserId = userId,
                    DisplayReason = displayReason,
                    CreateTime = createTime,
                    Active = active
                });
            }

            return logsList;
        }

        public static async Task<bool> logAction(InteractionContext ctx, DiscordMember user, string username, DiscordColor color, string action, string? reason = null, string ? duration = null)
        {
            ulong channel = Program.logChannel;
            var channels = await ctx.Interaction.Guild.GetChannelsAsync();
            CancellationToken cancellationToken = new CancellationToken();
            long.TryParse(username, out long userId);
            if(userId == 0) {
                long userNew = await GetUser($"{username}").WaitAsync(cancellationToken);
                username = $"{userNew}";
            }

            if(!channels.Any(ch => ch.Id == channel)) {
                return false;
            }

            try
            {
                var json = File.ReadAllText("games.json");
                var doc = JsonDocument.Parse(json);
                var caseNumber = doc.RootElement.GetProperty("games").GetInt32() + 1;
                var newGameCase = $"Game #{caseNumber}";

                DiscordEmbed logEmbed;
                var logChannel = ctx.Interaction.Guild.GetChannel(channel);
                if (duration != null)
                {
                    logEmbed = new DiscordEmbedBuilder
                    {
                        Title = newGameCase,
                        Description = $"{user.Mention} {action} {username} for `{reason}` and for a timeframe of `{duration}`",
                        Color = color
                    };
                }
                else
                {
                    logEmbed = new DiscordEmbedBuilder
                    {
                        Title = newGameCase,
                        Description = $"{user.Mention} {action} {username} for `{reason}`",
                        Color = color
                    };
                }
                if (reason == null)
                {
                    logEmbed = new DiscordEmbedBuilder
                    {
                        Title = newGameCase,
                        Description = $"{user.Mention} {action} {username}",
                        Color = color
                    };
                }

                var updatedJson = JsonSerializer.Serialize(new { games = caseNumber });
                File.WriteAllText("games.json", updatedJson);

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
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public static async Task<bool> sendMessagingServiceRequest(string topicString, string messageString)
        {
            try {
                var url = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}:publishMessage";

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", Program.apiKey);

                var body = new
                {
                    topic = topicString,
                    message = messageString
                };

                var json = JsonSerializer.Serialize(body);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync(url, content).Result;
            } catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("--- STACK BEGIN ---");
                Console.WriteLine(e);
                Console.WriteLine("--- STACK END ---");
                Console.ResetColor();
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
                bool hasAdmin = await GetAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{ctx.Client.GatewayVersion}").AsEphemeral());
            }

            [SlashCommand("ban", "Ban an user")]
            public async Task ban(InteractionContext ctx, [Option("Username", "User to ban")] string username, [Option("reason", "Reason for the kick")] string? reason = "No reason provided")
            {
                bool hasAdmin = await GetAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }

                bool requestAnswer = await SendRequest("ban", username, reason);
                if (requestAnswer)
                {
                    bool messagingServiceAnswer = await sendMessagingServiceRequest("userBanned", $"{username}");
                    if (!messagingServiceAnswer)
                    {
                        await ctx.CreateResponseAsync($"There was an error removing user from leaderboards the request. {messagingServiceAnswer}", true);
                    }
                    var banEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Ban",
                        Description = $"{username} has been banned for `{reason}`",
                        Color = DiscordColor.Red
                    };
                    await ctx.CreateResponseAsync(banEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, username, DiscordColor.Red, "banned", reason);
                }
                else
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title="API Limit - 429",
                        Description="API Rate limit hit. Please retry in a few seconds.",
                        Color=DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
                    return;
                }
            }

            [SlashCommand("unban", "Unban an user")]
            public async Task unban(InteractionContext ctx, [Option("username", "User to ban")] string user)
            {
                bool hasAdmin = await GetAdmin(ctx);
                if (!hasAdmin)
                {
                    return;
                }

                bool requestAnswer = await SendRequest("unban", user);
                if (requestAnswer)
                {
                    bool messagingServiceAnswer = await sendMessagingServiceRequest("userUnbanned", $"{user}");
                    if (!messagingServiceAnswer)
                    {
                        await ctx.CreateResponseAsync($"There was an error removing user from leaderboards the request. {messagingServiceAnswer}", true);
                    }
                    var unbanEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Unban",
                        Description = $"{user} has been unbanned.",
                        Color = DiscordColor.Green
                    };
                    await ctx.CreateResponseAsync(unbanEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, user, DiscordColor.Green, "unbanned");
                }
                else
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title="API Limit - 429",
                        Description="API Rate limit hit. Please retry in a few seconds.",
                        Color=DiscordColor.Red
                    };

                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral(true));
                    return;
                }
            }

            [SlashCommand("testcommand", "Testing command for the nut")]
            public async Task testcommand(InteractionContext ctx, [Option("username", "GETS USERID FOR USER")] string username = "0")
            {
                long userId = await GetUser(username);
                Console.WriteLine(userId);
            }

            public async Task<string> getUsername(long userId)
            {
                string url = $"https://users.roblox.com/v1/users/{userId}";

                using HttpClient httpClient = new();

                var request = httpClient.GetAsync(url).Result;

                if(!request.IsSuccessStatusCode)
                {
                    return $"ERR {request.StatusCode}";
                }

                var content = await request.Content.ReadAsStringAsync();
                var jsonContent = JsonDocument.Parse(content.ToString());
                var name = jsonContent.RootElement.GetProperty("name").ToString();
                return name;
            }

            [SlashCommand("getbans", "Get a list of banned players")]
            public async Task getbans(InteractionContext ctx)
            {
                try
                {
                    await ctx.DeferAsync(true);
                    bool hasAdmin = await GetAdmin(ctx);
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
                        long.TryParse(ban.UserId, out long userId);
                        var username = await getUsername(userId);
                        embedBuilder.AddField(
                            $"Username: {username} | User ID: {ban.UserId}",
                            $"{ban.DisplayReason}\nBanned at: {ban.CreateTime.ToLocalTime():g}",
                            inline: false);
                    }

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embedBuilder));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            [SlashCommand("getuser", "Gets a specific user by their username")]
            public async Task getuser(InteractionContext ctx, [Option("username", "User to get information about")] string user)
            {
                try {
                    bool hasAdmin = await GetAdmin(ctx);
                    if (!hasAdmin)
                    {
                        return;
                    }

                    await ctx.DeferAsync(true);

                    long.TryParse(user, out long userId);
                    if(userId == 0)
                    {
                        userId = await GetUser(user);
                    }
                    string detailedInfoLink = $"https://users.roblox.com/v1/users/{userId}";
                    string followers = $"https://friends.roblox.com/v1/users/{userId}/followers/count";
                    string avatarURL = $"https://thumbnails.roblox.com/v1/users/avatar?userIds={userId}&size=250x250&format=Png&isCircular=false";
                    string getBanURL = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}/user-restrictions/{userId}";
                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("x-api-key", Program.apiKey);

                    var isBannedResp = await httpClient.GetAsync(getBanURL);
                    var GBContent = await isBannedResp.Content.ReadAsStringAsync();
                    var GBSuccess = isBannedResp.IsSuccessStatusCode;

                    var response = await httpClient.GetAsync(detailedInfoLink);
                    var content = await response.Content.ReadAsStringAsync();
                    var isSuccess = response.IsSuccessStatusCode;

                    var avatarResp = await httpClient.GetAsync(avatarURL);
                    var AContent = await avatarResp.Content.ReadAsStringAsync();
                    var ASUCCESS = response.IsSuccessStatusCode;

                    var avatarJson = JsonDocument.Parse(AContent).RootElement;
                    var dataElement = avatarJson.GetProperty("data");
                    var imageUrl = "";
                    try {
                        imageUrl = dataElement[0].GetProperty("imageUrl").GetString();
                    } catch
                    {
                        imageUrl = "https://devforum-uploads.s3.dualstack.us-east-2.amazonaws.com/uploads/original/4X/7/c/3/7c34eccaf268571661f00616fa85b67c21836175.png";
                    }

                    if(!isSuccess)
                    {
                        var embed = new DiscordEmbedBuilder()
                        {
                            Title = $"{response.StatusCode} - Oops?",
                            Description = $"Something has gone terribly wrong.",
                            Color = DiscordColor.Red
                        };
                        embed.AddField("Error Message", response.ReasonPhrase ?? "UNKNOWN // SERVER DID NOT SEND ANY.", true);

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                        return;
                    }

                    if(!ASUCCESS)
                    {
                        var avatarEmbed = new DiscordEmbedBuilder()
                        {
                            Title = $"{response.StatusCode} - Oops?",
                            Description = $"Something has gone terribly wrong.",
                            Color = DiscordColor.Red
                        };
                        avatarEmbed.AddField("Error Message", response.ReasonPhrase ?? "UNKNOWN // SERVER DID NOT SEND ANY.", true);

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(avatarEmbed));
                        return;
                    }

                    if(!GBSuccess)
                    {
                        var GBEmbed = new DiscordEmbedBuilder()
                        {
                            Title = $"{response.StatusCode} - Oops?",
                            Description = $"Something has gone terribly wrong.",
                            Color = DiscordColor.Red
                        };
                        GBEmbed.AddField("Error Message", response.ReasonPhrase ?? "UNKNOWN // SERVER DID NOT SEND ANY.", true);

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(GBEmbed));
                        return;
                    }

                    var json = JsonDocument.Parse(content);
                    var GBJson = JsonDocument.Parse(GBContent);
                    var description = json.RootElement.GetProperty("description");
                    var creationDate = json.RootElement.GetProperty("created");
                    var isBanned = GBJson.RootElement.GetProperty("gameJoinRestriction").GetProperty("active").GetBoolean() ? "Yes" : "No";
                    var externalAppDisplayName = json.RootElement.GetProperty("externalAppDisplayName");
                    var hasVerifiedBadge = json.RootElement.GetProperty("hasVerifiedBadge");
                    var name = json.RootElement.GetProperty("name");
                    var displayName = json.RootElement.GetProperty("displayName");
                    response = await httpClient.GetAsync(followers);

                    var contentFol = await response.Content.ReadAsStringAsync();
                    var isSuccessFol = response.IsSuccessStatusCode;

                    if(!isSuccessFol)
                    {
                        var folembed = new DiscordEmbedBuilder()
                        {
                            Title = $"{response.StatusCode} - Oops?",
                            Description = $"Something has gone terribly wrong.",
                            Color = DiscordColor.Red
                        };
                        folembed.AddField("Error Message", response.ReasonPhrase ?? "UNKNOWN // SERVER DID NOT SEND ANY.", true);

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(folembed));
                        return;
                    }

                    var followerCount = JsonDocument.Parse(contentFol).RootElement.GetProperty("count");

                    var Sembed = new DiscordEmbedBuilder()
                    {
                        Title = name.ToString(),
                        Color = DiscordColor.Green
                    };

                    var friendCountURL = $"https://friends.roblox.com/v1/users/{userId}/friends/count";
                    response = await httpClient.GetAsync(friendCountURL);
                    var contentFriends = await response.Content.ReadAsStringAsync();
                    var isSuccessFriends = response.IsSuccessStatusCode;

                    if(!isSuccessFriends)
                    {
                        var frembed = new DiscordEmbedBuilder()
                        {
                            Title = $"{response.StatusCode} - Oops?",
                            Description = $"Something has gone terribly wrong.",
                            Color = DiscordColor.Red
                        };
                        frembed.AddField("Error Message", response.ReasonPhrase ?? "UNKNOWN // SERVER DID NOT SEND ANY.", true);

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(frembed));
                        return;
                    }

                    var friendCount = JsonDocument.Parse(contentFriends).RootElement.GetProperty("count");

                    var socialsURL = $"https://accountinformation.roblox.com/v1/users/{userId}/promotion-channels";
                    response = await httpClient.GetAsync(socialsURL);
                    var contentSocials = await response.Content.ReadAsStringAsync();
                    var isSuccessSocials = response.IsSuccessStatusCode;

                    if(!isSuccessSocials)
                    {
                        var soembed = new DiscordEmbedBuilder()
                        {
                            Title = $"{response.StatusCode} - Oops?",
                            Description = $"Something has gone terribly wrong.",
                            Color = DiscordColor.Red
                        };
                        soembed.AddField("Error Message", response.ReasonPhrase ?? "UNKNOWN // SERVER DID NOT SEND ANY.", true);

                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(soembed));
                        return;
                    }

                    var socialsJson = JsonDocument.Parse(contentSocials).RootElement;
                    var facebook = socialsJson.GetProperty("facebook");
                    var twitter = socialsJson.GetProperty("twitter");
                    var youtube = socialsJson.GetProperty("youtube");
                    var twitch = socialsJson.GetProperty("twitch");

                    string Validate(string input) => string.IsNullOrWhiteSpace(input) ? "N/A" : input;

                    DateTime.TryParse(creationDate.ToString(), out DateTime creation);
                    Sembed.AddField("Description", Validate(description.ToString()), true);
                    Sembed.AddField("Creation Date", creation.ToString("yyyy/MM/dd"), true);
                    Sembed.AddField("Banned", isBanned.ToString(), true);
                    Sembed.AddField("External App Display Name", Validate(externalAppDisplayName.ToString()), true);
                    Sembed.AddField("Verified", hasVerifiedBadge.ToString(), true);
                    Sembed.AddField("User Id", userId.ToString(), true);
                    Sembed.AddField("Display Name", displayName.ToString(), true);
                    Sembed.AddField("Followers:", followerCount.ToString(), true);
                    Sembed.AddField("Friends:", friendCount.ToString(), true);
                    Sembed.AddField("Socials:", $"FB: {Validate(facebook.ToString())} X: {Validate(twitter.ToString())} YT: {Validate(youtube.ToString())} TWITCH: {Validate(twitch.ToString())}");
                    if(imageUrl != null || imageUrl != string.Empty) Sembed.WithThumbnail(imageUrl.ToString());

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Sembed));
                    return;
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("--- STACK BEGIN ---");
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("---  STACK END  ---");
                    Console.ResetColor();
                }
            }

            [SlashCommand("tempban", "Temporarily ban someone")]
            public async Task tempban(InteractionContext ctx, [Option("Username", "The username for the user to ban.")] string username, [Option("Reason", "The reason for the ban")] string reason, [Option("Duration", "The duration of the ban")] string duration)
            {
                bool isAdmin = await GetAdmin(ctx);
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

                CancellationToken cancellationToken = new();
                long.TryParse(username, out long userId);
                if(userId == 0) {
                    userId = await GetUser(username).WaitAsync(cancellationToken);
                }
                var apiRequest = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}/user-restrictions/{userId}";
                var httpClient = new HttpClient();
                var payload = new
                {
                    path = $"universes/{BanLogEntry.universeId}/user-restrictions/{userId}",
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
                    _ = await logAction(ctx, ctx.Member, username, DiscordColor.Red, "temporarily banned", reason, totalAmt);
                    return;
                } else
                {
                    await ctx.CreateResponseAsync("An error has occured. Are we rate limited?", true);
                    return;
                }
            }

            [SlashCommand("kick", "Kick an user from the game")]
            public async Task kick(
            InteractionContext ctx, [Option("Username", "User to kick")] string user, [Option("reason", "Reason for the kick")] string? reason = "No reason provided")
            {
                try {
                    bool hasAdmin = await GetAdmin(ctx);
                    if (!hasAdmin)
                        return;

                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Add("x-api-key", Program.apiKey);

                    CancellationToken cancellationToken = new();
                    long.TryParse(user, out long userId);
                    if(userId == 0) {
                        userId = await GetUser(user).WaitAsync(cancellationToken);
                    }

                    var url = $"https://apis.roblox.com/cloud/v2/universes/{BanLogEntry.universeId}/user-restrictions/{userId}";

                    var payload = new
                    {
                        path = $"universes/{BanLogEntry.universeId}/user-restrictions/{userId}",
                        gameJoinRestriction = new
                        {
                            active = true,
                            privateReason = "Moderator action",
                            displayReason = reason,
                            excludeAltAccounts = false,
                            duration = "1s"
                        }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var request = new HttpRequestMessage(HttpMethod.Patch, url)
                    {
                        Content = content
                    };

                    HttpResponseMessage response;

                    response = await httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode) {
                        return;
                    }

                    var kickEmbed = new DiscordEmbedBuilder
                    {
                        Title = "Kick",
                        Description = $"{user} has been kicked for `{reason}`",
                        Color = DiscordColor.Orange
                    };

                    await ctx.CreateResponseAsync(kickEmbed.Build(), true);
                    _ = await logAction(ctx, ctx.Member, user, DiscordColor.Orange, "kicked", reason);
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("--- STACK BEGIN ---");
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("--- STACK SPLIT ---");
                    Console.WriteLine(e);
                    Console.WriteLine("--- STACK END ---");
                    Console.ResetColor();
                }
            }

            [SlashCommand("announce", "Make a global announcement in all game servers. THE MESSAGE WILL BE FILTERED BY ROBLOX!")]
            public async Task announce(InteractionContext ctx, [Option("message", "The message to announce")] string message)
            {
                bool hasAdmin = await GetAdmin(ctx);
                if (!hasAdmin)
                    return;

                bool requestAnswer = await sendMessagingServiceRequest("announcement", message);
                if (requestAnswer)
                {
                    var announceEmbed = new DiscordEmbedBuilder()
                    {
                        Title = "Success!",
                        Description = $"Your announcement has been sent successfully.",
                        Color = DiscordColor.Green
                    };
                    announceEmbed.AddField("Content", message);
                    await ctx.CreateResponseAsync(announceEmbed.Build(), true);
                }
                else
                {
                    await ctx.CreateResponseAsync($"There was an error completing the request. {requestAnswer}", true);
                }
            }

            [SlashCommand("refreshbans", "Manually refresh bans")]
            public async Task refreshbans(InteractionContext ctx)
            {
                bool refreshed = await refBan.refreshBans(true);
                if(refreshed)
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Refreshed successfully!").AsEphemeral());
                    return;
                } else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Error refreshing, check logs!").AsEphemeral());
                    return;
                }
            }
        }
    }
}
