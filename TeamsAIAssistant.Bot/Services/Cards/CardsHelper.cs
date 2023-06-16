using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Builder;
using System.Threading.Tasks;
using System.Threading;
using TeamsAIAssistant.Bot.Interfaces.Cards;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using Microsoft.Bot.Schema;
using System.Collections.Generic;

namespace TeamsAIAssistant.Bot.Services.Cards
{
    public class CardsHelper : ICardsHelper
    {
        private readonly IConfiguration _configuration;
        public CardsHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendWelcomeCard(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var teamMember = await TeamsInfo.GetMemberAsync(stepContext.Context, stepContext.Context.Activity.From.Id).ConfigureAwait(false);
            var paths = new[] { ".", "Cards", "WelcomeCard.json" };

            object dataJson = new { Username = teamMember.Name, LogoUrl = $"{_configuration["Hostname"]}/images/logo.png" };

            var attachment = CreateAdaptiveCardWithData(paths, dataJson);
            var reply = MessageFactory.Attachment(attachment);
            await stepContext.Context.SendActivityAsync(reply);
        }

        public static Attachment CreateAdaptiveCardWithData(string[] path, object dataJson)
        {
            string templateJson = System.IO.File.ReadAllText(Path.Combine(path), Encoding.UTF8);
            var template = new AdaptiveCards.Templating.AdaptiveCardTemplate(templateJson);
            var card = template.Expand(dataJson);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(card),
            };

            return adaptiveCardAttachment;
        }

        public static Attachment CreateAdaptiveCard(string[] path)
        {
            string templateJson = System.IO.File.ReadAllText(Path.Combine(path), Encoding.UTF8);

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(templateJson),
            };

            return adaptiveCardAttachment;
        }

        public static HeroCard GetHeroCard(string title, string subtitle, string text, string imageURL, string buttonText, string buttonURL)
        {
            var heroCard = new HeroCard();

            heroCard.Title = title ?? string.Empty;
            heroCard.Subtitle = subtitle ?? string.Empty;
            heroCard.Text = text ?? string.Empty;
            if (!string.IsNullOrEmpty(imageURL))
            {
                heroCard.Images = new List<CardImage> { new CardImage(imageURL) };
            }
            if (!string.IsNullOrEmpty(buttonURL))
            {
                heroCard.Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, buttonText ?? string.Empty, value: buttonURL) };
            }

            return heroCard;
        }
    }
}
