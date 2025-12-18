using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility.config
{
    public static class vars
    {
        public static bool commandTriggered { get; set; } = false;
        public static bool nitroOnlyTest { get; set; } = true;
        public static ulong modLogs { get; } = 1445401844611547356;
        public static ulong blacklistRole { get; } = 1397404869702320260;
        public static bool debugMode { get; set; } = false;
        public static Dictionary<string, (bool isBad, DateTime expiresAt)> inviteCache = [];
    }
}
