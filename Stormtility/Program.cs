using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.commands;
using Stormtility.config;
using System.Diagnostics.Tracing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Stormtility
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension CommandBuilder { get; set; }
        private static SlashCommandsExtension Slash { get; set; }
        static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            Client = new DiscordClient(discordConfig);
            Client.Ready += Client_Ready;
            Client.GuildMemberUpdated += Client_GuildMemberUpdated;
            Client.MessageCreated += Client_MessageCreated;

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };
            CommandBuilder = Client.UseCommandsNext(commandsConfig);
            CommandBuilder.RegisterCommands<TestCommands>();

            Slash = Client.UseSlashCommands();
            Slash.RegisterCommands<slashcommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static async Task<bool> checkMessage(DiscordMessage message)
        {
            List<string> blacklistedWords = new List<string> {"18", "e-girl", "condo", "csam", "dropbox", "porn", "rape", "erp", "erotic", "hanime", "hentai"};

            string content = message.Content;

            Regex inviteRegex = new Regex(@"https://discord\.gg/([a-zA-Z0-9]+)");

            Match match = inviteRegex.Match(content);
            if (!match.Success)
                return false;

            string inviteCode = match.Groups[1].Value;

            string url = $"https://discord.com/api/v10/invites/{inviteCode}?with_counts=true";

            using var http = new HttpClient();
            var response = await http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return false;

            string jsonText = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(jsonText);

            string? guildName = json["guild"]?["name"]?.ToString();

            if (guildName == null)
                return false;

            foreach (string word in blacklistedWords)
            {
                if (guildName.Contains(word, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static async Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            bool result = await checkMessage(message: args.Message);
            var user = (DiscordUser)args.Author;
            if (args.Author.IsBot) return;
            var member = (DiscordMember)user;

            if (result)
            {
                await args.Message.DeleteAsync();
                foreach (var role in member.Roles)
                {
                    if (role.Id == args.Guild.GetRole(vars.blacklistRole).Id)
                    {
                        return;
                    }
                    else
                    {
                        await member.GrantRoleAsync(args.Guild.GetRole(vars.blacklistRole));
                        break;
                    }
                }
                var embed = new DiscordEmbedBuilder
                {
                    Title = "Inappropiate invite",
                    Description = $"{member.Mention} has been blacklisted for sending following invite: {args.Message.Content}",
                    Color = DiscordColor.Red
                };
                var logChannel = args.Guild.GetChannel(vars.modLogs);
                await logChannel.SendMessageAsync(embed);
            }

            if (args.Author.IsBot)
            {
                return;
            }
            var content = args.Message.Content;

            var badPhrases = new[]
            {
                "ts = this",
                "this = ts"
            };

            var exceptions = new[]
            {
                "ts = this shit",
                "this shit = ts"
            };

            bool matchesBad = badPhrases.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));
            bool matchesException = exceptions.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase));

            if (matchesBad && !matchesException)
            {
                if (args.Guild == null) return;
                if (member.IsBot) return;
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.ModerateMembers))
                    return;

                var jsonText = File.ReadAllText("mutecounter.json");
                var json = JObject.Parse(jsonText);
                var mutes = (JArray)json["mutes"];

                string userId = args.Author.Id.ToString();
                JObject? entry = null;

                foreach (JObject e in mutes)
                {
                    if (e.ContainsKey(userId))
                    {
                        entry = e;
                        break;
                    }
                }

                if (entry == null)
                {
                    entry = new JObject
                    {
                        [userId] = new JObject
                        {
                            ["mutes"] = 1
                        }
                    };

                    mutes.Add(entry);
                }
                else
                {
                    int current = entry[userId]["mutes"].Value<int>();
                    entry[userId]["mutes"] = current + 1;
                }

                File.WriteAllText("mutecounter.json", json.ToString());

                await member.TimeoutAsync(DateTimeOffset.UtcNow.AddMinutes(1), "wrong use of ts");
            }

            return;
        }

        private static Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            if(vars.commandTriggered)
            {
                return Task.CompletedTask;
            }

            var blacklist = args.Guild.GetRole(1397404869702320260);
            var blacklists = "blacklists.json";

            bool hadBefore = args.RolesBefore.Any(r => r.Id == blacklist.Id);
            bool hasAfter = args.RolesAfter.Any(r => r.Id == blacklist.Id);

            var jsonText = File.ReadAllText(blacklists);
            var json = JObject.Parse(jsonText);
            var blacklisttable = (JArray)json["blacklist"];
            JObject toRemove = null;

            if (hadBefore && !hasAfter)
            {
                foreach (JObject entries in blacklisttable)
                {
                    if (entries.ContainsKey(args.Member.Id.ToString()))
                    {
                        toRemove = entries;
                        break;
                    }
                }
            }

            if (toRemove != null)
            {
                blacklisttable.Remove(toRemove);
                File.WriteAllText(blacklists, json.ToString());
                return Task.CompletedTask;
            }

            if (!hadBefore && hasAfter)
            {
                var entry = new JObject
                {
                    [args.Member.Id.ToString()] = new JObject
                    {
                        ["reason"] = "Role has been given manually."
                    }
                };

                ((JArray)json["blacklist"]).Add(entry);

                File.WriteAllText(blacklists, json.ToString());
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }


        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
