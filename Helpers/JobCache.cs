using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility
{
    public static class JobCache
    {
        public static Dictionary<string, JobData> Jobs = new();

        public static void Load()
        {
            var json = File.ReadAllText("config/jobs.json");
            var root = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, JobData>>>(json);
            Jobs = root!["jobs"]
                .ToDictionary(kvp => kvp.Key.ToLower(), kvp => kvp.Value);
        }
    }
}
