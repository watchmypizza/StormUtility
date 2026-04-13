using Stormtility.config;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using DSharpPlus.Exceptions;
using System.Text.Json;
using Stormtility.commands;

namespace Stormtility.events
{
    public class MemberUpdated
    {
        private static async Task<bool> verifyDisplayName(DiscordUser user)
        {
            Regex regex = new Regex("[^A-Za-z0-9()!?,.\"$':#+:;_\\s]+");
            DiscordMember discordMember = (DiscordMember)user;
            var nickname = discordMember.DisplayName;
            var match = regex.Match(nickname);

            if(match.Success)
            {
                string currentNick = nickname;
                var replacedNick = currentNick.Replace(match.Value, "");
                try {
                    await discordMember.ModifyAsync(a => a.Nickname = replacedNick ?? user.Username);
                } catch (UnauthorizedException e)
                {
                    Console.WriteLine($"Not authorized to perform action on {discordMember.Mention} ({discordMember.Username})");
                    return false;
                } catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("--- STACK BEGIN ---");
                    Console.WriteLine(e.StackTrace);
                    Console.WriteLine("--- STACK SPLIT ---");
                    Console.WriteLine(e.Message);
                    Console.WriteLine("---  STACK END  ---");

                    try 
                    {
                        using HttpClient httpClient = new();
                        var url = Program.webhookURL;

                        var payload = new
                        {
                            content = $"**[ERROR]** {DateTime.Now}\n```\n{e.Message}\n{e.StackTrace?[..Math.Min(e.StackTrace.Length, 1500)]}\n```"
                        };

                        string json = JsonSerializer.Serialize(payload);

                        using StringContent stringContent = new(json, System.Text.Encoding.UTF8, "application/json");

                        var response = await httpClient.PostAsync(url, stringContent);

                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[Webhook Error] Discord returned: {response.StatusCode}");
                        }
                    }
                    catch (Exception logEx)
                    {
                        Console.WriteLine($"FAILED TO SEND WEBHOOK: {logEx.Message}");
                    }

                    return false;
                }
            }

            return true;
        }

        public static async Task<Task> Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            if(vars.commandTriggered)
            {
                return Task.CompletedTask;
            }

            _ = await verifyDisplayName(args.Member);

            var blacklist = args.Guild.GetRole(1397404869702320260);

            bool hadBefore = args.RolesBefore.Any(r => r.Id == blacklist.Id);
            bool hasAfter = args.RolesAfter.Any(r => r.Id == blacklist.Id);

            slashcommands blacklists = new slashcommands();

            if (hadBefore && !hasAfter)
            {
                string isBlacklisted = await blacklists.getBlacklist(args.Member);
                if(isBlacklisted == string.Empty) { return Task.CompletedTask; }
                bool removed = await blacklists.removeBlacklist(args.Member);
            }

            if (!hadBefore && hasAfter)
            {
                string isBlacklisted = await blacklists.getBlacklist(args.Member);
                if(isBlacklisted != string.Empty) { return Task.CompletedTask; }
                bool added = await blacklists.insertBlacklist(args.Member, "Role has been given manually.");

                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }
    }
}