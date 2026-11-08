namespace Stormtility.Helpers
{
    public class LinkHelper
    {
        private static readonly FileHelper fileHelper = new();
        private static List<string> Links = [];

        public static async Task StartCacheRefresh()
        {
            PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

            do
            {
                try
                {
                    Links = await fileHelper.Read("linkblacklist.txt");
                }
                catch (Exception e)
                {
                    await Logger.LOGGER(e);
                }
            } while (await timer.WaitForNextTickAsync());
        }

        public static bool IsBlacklisted(string msgContent)
        {
            foreach (string link in Links)
            {
                if (msgContent.Contains(link))
                {
                    return true;
                }
                return false;
            }
            return false;
        }

        public static async Task RefreshCache() {
            try {
                Links = await fileHelper.Read("linkblacklist.txt");

                return;
            } catch(Exception e) {
                await Logger.LOGGER(e);
                return;
            }
        }
    }
}
