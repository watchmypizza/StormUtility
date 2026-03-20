using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;

namespace Stormtility.events
{
    public class ModalSubmitted
    {
        public static async Task Client_ModalSubmittedEvent(DiscordClient sender, ModalSubmitEventArgs args)
        {
            string userCode = args.Values["verificationcode"];
            string code = Helpers.verificationCode.getCodeIfExists(args.Interaction.User);

            if(userCode == code)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "200 - Success",
                    Description="You have successfully verified, welcome!",
                    Color=DiscordColor.Green
                };
                await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                return;
            } else
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = "400 - Oops?",
                    Description="Please try verifying again, you might have entered the wrong code.",
                    Color=DiscordColor.Red
                };
                await args.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                return;
            }
        }
    }
}