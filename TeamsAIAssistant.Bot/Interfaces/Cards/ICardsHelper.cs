using Microsoft.Bot.Builder.Dialogs;
using System.Threading.Tasks;
using System.Threading;

namespace TeamsAIAssistant.Bot.Interfaces.Cards
{
    public interface ICardsHelper
    {
        Task SendWelcomeCard(WaterfallStepContext stepContext, CancellationToken cancellationToken);
    }
}
