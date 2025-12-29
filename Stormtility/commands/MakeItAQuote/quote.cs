using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp.Web.DependencyInjection;

namespace Stormtility.commands.MakeItAQuote
{
    public class quote
    {
        public static async Task<Image> turnQuote(InteractionContext ctx, DiscordMessage message)
        {
            try
            {
                using var httpClient = new HttpClient();
                var bytes = await httpClient.GetByteArrayAsync(message.Author.AvatarUrl);
                var avatar = Image.Load<Rgba32>(bytes);

                var username = message.Author.Username;

                avatar.Mutate(ctx => ctx.Resize(750, 1080));

                var gradientBrush = new LinearGradientBrush(
                    new PointF(avatar.Width * 0.6f, 0),
                    new PointF(avatar.Width, 0),
                    GradientRepetitionMode.None,
                    new ColorStop(0, Color.Transparent),
                    new ColorStop(1, Color.Black.WithAlpha(0.8f))
                );

                avatar.Mutate(ctx => ctx.Fill(gradientBrush));

                var fontCollection = new FontCollection();
                var fontFamily = fontCollection.Add("SystemFonts");
                var fontMessage = fontFamily.CreateFont(28, FontStyle.Regular);
                var fontUsername = fontFamily.CreateFont(22, FontStyle.Italic);

                var textOptions = new TextOptions(fontMessage)
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    WrappingLength = avatar.Width * 0.35f,
                };

                avatar.Mutate(ctx =>
                {
                    ctx.DrawText(message.Content, fontMessage, Color.White, new PointF(avatar.Width * 0.62f, 20));
                    ctx.DrawText(username, fontUsername, Color.LightGray, new PointF(avatar.Width * 0.62f, 130));
                });

                return avatar;
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}
