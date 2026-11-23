using DSharpPlus.Entities;

namespace Stormtility.Helpers
{
    public class MessageEventsHelper
    {
        public static async Task<bool> CheckBigText(DiscordMessage message)
        {
            string content = message.Content;

            DiscordMember? member = (DiscordMember?)message?.Author;

            if (member is not null)
            {
                if (member?.PermissionsIn(message?.Channel!).HasPermission(DiscordPermission.Administrator) ?? false)
                {
                    return false;
                }
            }

            if (content.StartsWith("#"))
            {
                try
                {
                    await message!.DeleteAsync("Big text");
                }
                catch (Exception e)
                {
                    await Logger.LOGGER(e);
                    return true;
                }
            }

            List<char> chars = [];

            foreach (char letter in content)
            {
                chars.Add(letter);

                if (chars.Count < content.Length)
                {
                    continue;
                }

                for (int i = 0; i < chars.Count - 1; i++)
                {
                    if (chars[i - 1] == '\n' && chars[i] == '#' && chars[i + 1] == ' ')
                    {
                        try
                        {
                            await message!.DeleteAsync("Big text");
                            return true;
                        }
                        catch (Exception e)
                        {
                            await Logger.LOGGER(e);
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
