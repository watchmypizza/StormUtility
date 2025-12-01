using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using Stormtility.commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stormtility.config;
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

            // CommandsNext (prefix commands)
            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };
            CommandBuilder = Client.UseCommandsNext(commandsConfig);
            CommandBuilder.RegisterCommands<TestCommands>();

            // Slash commands (only ONE call!)
            Slash = Client.UseSlashCommands();
            Slash.RegisterCommands<slashcommands>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }

        private static Task Client_MessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            if (args.Author.IsBot)
            {
                return Task.CompletedTask;
            }
            var content = args.Message.Content;
            var user = (DiscordUser)args.Author;
            var member = (DiscordMember)user;

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
                if (args.Guild == null) return Task.CompletedTask;
                if (member.IsBot) return Task.CompletedTask;
                if (member.PermissionsIn(args.Channel).HasPermission(Permissions.ModerateMembers))
                    return Task.CompletedTask;

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

                member.TimeoutAsync(DateTimeOffset.UtcNow.AddMinutes(1), "wrong use of ts");
            }

            return Task.CompletedTask;
        }

        private static Task Client_GuildMemberUpdated(DiscordClient sender, GuildMemberUpdateEventArgs args)
        {
            if(vars.commandTriggered)
            {
                return Task.CompletedTask;
            }

            const ulong blacklist = 1397404869702320260;
            var blacklists = "blacklists.json";

            bool hadBefore = args.RolesBefore.Any(r => r.Id == blacklist);
            bool hasAfter = args.RolesAfter.Any(r => r.Id == blacklist);

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


        private static Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
