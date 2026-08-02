using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Stormtility.events
{
    public static class VCJoined
    {
        // Persistent cache outside the Task so timestamps survive between separate event calls
        private static readonly ConcurrentDictionary<ulong, DateTime> _voiceSessionCache = new();
        private const ulong StaffRoleId = 1370422448763437207;

        public static async Task Client_HandleVCJoin(DiscordClient sender, VoiceStateUpdatedEventArgs args)
        {
            DiscordMember member = (DiscordMember) await args.GetUserAsync();
            if (member == null || member.IsBot) return;

            if (!member.Roles.Any(r => r.Id == StaffRoleId)) return;

            ulong userId = member.Id;

            if (args.Before?.GetChannelAsync() == null && args.After?.GetChannelAsync() != null)
            {
                _voiceSessionCache[userId] = DateTime.UtcNow;
            }

            else if (args.Before?.GetChannelAsync() != null && args.After?.GetChannelAsync() == null)
            {
                if (_voiceSessionCache.TryRemove(userId, out DateTime joinTime))
                {
                    TimeSpan duration = DateTime.UtcNow - joinTime;
                    int totalMinutes = (int)Math.Round(duration.TotalMinutes);

                    if (totalMinutes > 0)
                    {
                        var staffActivity = await Helpers.StaffActivityTracker.GetActivity(userId);
                        
                        int currentVC = staffActivity.TryGetValue("VCMinutes", out var vcObj) ? Convert.ToInt32(vcObj) : 0;
                        staffActivity["VCMinutes"] = currentVC + totalMinutes;
                        staffActivity["lastActive"] = DateTime.UtcNow.ToString("O");

                        await Helpers.StaffActivityTracker.UpsertActivity(userId, staffActivity);
                    }
                }
            }
            
            else if (args.Before?.GetChannelAsync() != null && args.After?.GetChannelAsync() != null && args.Before.GetChannelAsync().Result.Id != args.After.GetChannelAsync().Result.Id)
            {
            }
        }
    }
}