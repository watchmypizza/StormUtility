using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stormtility
{
    public class JobPayout
    {
        public static async Task RunDailyJobAsyncPayout()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromHours(24));

                try
                {
                    jobPayout();

                    Console.WriteLine("Job payout executed");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public static void jobPayout()
        {
            var eJ = File.ReadAllText("economy.json");
            var eJP = JObject.Parse(eJ);
            var eE = (JArray)eJP["economy"];

            var jJ = File.ReadAllText("jobs.json");
            var jJP = JObject.Parse(jJ);
            var jobsDict = (JObject)jJP["jobs"];

            foreach (JObject userWrapper in eE)
            {
                foreach (var userProp in userWrapper.Properties())
                {
                    var userId = userProp.Name;
                    var userData = (JObject)userProp.Value;

                    if (!userData.ContainsKey("job"))
                        continue;

                    var jobName = userData["job"]?.ToString()?.ToLower();

                    if (string.IsNullOrEmpty(jobName))
                        continue;

                    if (!jobsDict.ContainsKey(jobName))
                        continue;

                    var jobData = (JObject)jobsDict[jobName];
                    int salary = jobData["salary"]?.Value<int>() ?? 0;

                    int currentMoney = userData["money"]?.Value<int>() ?? 0;
                    userData["money"] = currentMoney + salary;
                }
            }
            File.WriteAllText("economy.json", eJP.ToString());
        }
    }
}
