using System.ComponentModel;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Trees.Metadata;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Stormtility.config;
using TskLib;

namespace Stormtility.commands.UtilityCommands
{
    [SlashCommandGroup("utility", "Utility commands")]
    [Command("utility")]
    [AllowedProcessors(typeof(SlashCommandProcessor))]
    [Description("Utility commands")]
    public class Utilitycommands
    {
        [SlashCommand("rank", "Rank every server member the member role")]
        [Command("rank")]
        [Description("Rank every server member with the member role")]
        public async Task Rank(SlashCommandContext ctx)
        {
            DiscordMember? member = ctx.Member!;
            DiscordInteraction interaction = ctx.Interaction!;
            if (!member.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
            {
                await ctx.RespondAsync("You do not have permission to use this command.");
                return;
            }

            await ctx.DeferResponseAsync(true);

            ulong memberRoleID = 1370383562515284090;
            var memberRole = await interaction.Guild!.GetRoleAsync(memberRoleID);

            int rolesGranted = 0;

            foreach(var mem in interaction.Guild!.Members)
            {
                if(mem.Value.IsBot) { return; }
                if (!mem.Value.Roles.Contains(memberRole))
                {
                    rolesGranted++;
                    await mem.Value.GrantRoleAsync(memberRole);
                }
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Granted the Member role to {rolesGranted} members."));
        }

        [SlashCommand("togglemorse", "Toggle morse code translation on or off")]
        [Command("togglemorse")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        [Description("Toggle morse code translation on or off.")]
        public async Task ToggleMorseCommand(SlashCommandContext ctx)
        {
            if (!ctx.Member!.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
            {
                await ctx.RespondAsync("You do not have permission to use this command.");
                return;
            }
            Vars.MorseTranslateEnabled = !Vars.MorseTranslateEnabled;
            string status = Vars.MorseTranslateEnabled ? "enabled" : "disabled";
            await ctx.RespondAsync($"Morse code translation has been {status}.", true);
        }

        [SlashCommand("toggleAI", "Toggle AI and what channel it should be in")]
        [Command("toggleAI")]
        [Description("Toggle AI in your current channel.")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task ToggleAI(SlashCommandContext ctx)
        {
            if(ctx.User.Id != 1116315001330880602)
            {
                return;
            }
            if(!Vars.AiToggle)
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("AI enabled."));
                Vars.AiToggle = true;
                Vars.AIChannel = ctx.Interaction.Channel.Id;
            } else
            {
                Vars.AiToggle = false;
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("AI disabled"));
            }
        }

        [SlashCommand("changePrompt", "Change the prompt to a funny prompt")]
        [Command("changePrompt")]
        [Description("Change the prompt the AI should utilize")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task ChangePrompt(SlashCommandContext ctx, [Parameter("prompt")][Description("The prompt")] string prompt)
        {
            if(ctx.User.Id != 1116315001330880602)
            {
                return;
            }

            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"Changed promt to: {prompt}").AsEphemeral());
            Vars.AIPrompt = prompt;
        }

        [SlashCommand("whitelist", "Add someone to AI whitelist")]
        [Command("whitelist")]
        [Description("Add someone to the whitelist for the AI")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task Whitelist(SlashCommandContext ctx, [Parameter("action")][Description("Action to perform.")] string action, [Parameter("user")][Description("User to execute action on")] DiscordUser user)
        {
            if (!ctx.Member!.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.ModerateMembers))
            {
                await ctx.RespondAsync("You do not have permission to use this command.", true);
                return;
            }

            if(action.ToLower() == "remove")
            {
                Vars.UserIdsAI.Remove(user.Id);
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"Removed {user.Username} from whitelist").AsEphemeral());
            } else if (action.ToLower() == "add")
            {
                Vars.UserIdsAI.Add(user.Id);
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"Added {user.Username} to whitelist").AsEphemeral());
            } else {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Invalid action, use 'add' or 'remove'").AsEphemeral());
            }
        }

        [SlashCommand("clearhistory", "clear chat history (wipe ai brain)")]
        [Command("clearhistory")]
        [Description("wipe ai brain")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task Clearhistory(SlashCommandContext ctx)
        {
            if (ctx.User.Id != 1116315001330880602)
            {
                return;
            }

            Vars.chatHistory = [];
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Cleared history").AsEphemeral());
        }

        [Command("converttranscripts")]
        [Description("Convert all the transcripts into .tsk files")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task Converttranscripts(SlashCommandContext ctx) {
            if (ctx.User.Id != 1116315001330880602)
            {
                return;
            }

            await ctx.DeferResponseAsync(true);
            await ctx.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent("Please standby..."));
            Tsk.Convert("transcripts.json", "transcripts");
        }

        [SlashCommand("fixbrokentsk", "Scan and repair broken .tsk transcript files")]
        [Command("fixbrokentsk")]
        [Description("Scan the transcripts folder and repair any .tsk files that fail to parse")]
        [AllowedProcessors(typeof(SlashCommandProcessor))]
        public async Task Fixbrokentsk(SlashCommandContext ctx)
        {
            if (!ctx.Member!.PermissionsIn(ctx.Channel).HasPermission(DiscordPermission.Administrator))
            {
                await ctx.RespondAsync("You do not have permission to use this command.", true);
                return;
            }

            await ctx.DeferResponseAsync(true);

            const string transcriptsFolder = "transcripts";

            if (!Directory.Exists(transcriptsFolder))
            {
                await ctx.Interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("No transcripts folder found.")
                );
                return;
            }

            var repaired = new List<TskRepairResult>();
            var failed = new List<string>();

            foreach (string file in Directory.EnumerateFiles(transcriptsFolder, "*.tsk"))
            {
                try
                {
                    TskRepairResult result = await TskRepair.RepairAsync(file);

                    if (result.WasBroken)
                        repaired.Add(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FixBrokenTsk] Failed on {file}: {ex}");
                    failed.Add(Path.GetFileName(file));
                }
            }

            if (repaired.Count == 0 && failed.Count == 0)
            {
                await ctx.Interaction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder().WithContent("Scanned all .tsk files, none were broken.")
                );
                return;
            }

            var summaryLines = repaired.Select(r =>
                $"`{Path.GetFileName(r.FilePath)}` — recovered {r.MessagesRecovered} message(s), backup: `{Path.GetFileName(r.BackupPath!)}`"
            ).ToList();

            if (failed.Count > 0)
                summaryLines.Add($"Failed to process: {string.Join(", ", failed.Select(f => $"`{f}`"))}");

            string summary = string.Join("\n", summaryLines);

            if (summary.Length > 1900)
                summary = summary[..1900] + "\n... (truncated)";

            await ctx.Interaction.EditOriginalResponseAsync(
                new DiscordWebhookBuilder().WithContent($"Repaired {repaired.Count} broken file(s):\n{summary}")
            );
        }
    }
}
