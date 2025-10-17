using PmEngine.Core.BaseMarkups;
using PmEngine.Telegram.Encoders;
using PmEngine.Telegram.Models;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace PmEngine.Telegram.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private string _botToken = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? "your_token";
        private long _chatId = 436644242;

        [TestMethod]
        public async Task SendTestShare()
        {
            var output = new TelegramOutput(null, new TelegramBotClient(_botToken), null, new TelegramOutputConfigure(), null);

            var knopka = new SingleMarkup() { InLine = true };
            knopka.Add(new InlineButtonActionWrapper(InlineKeyboardButton.WithUrl("Поделиться с друзьями", $"https://t.me/share/url?url={Uri.EscapeDataString("https://google.com")}&text={Uri.EscapeDataString("Попробуй этого бота для создания персональных стикеров!")}")));
            //knopka.Add(new InlineButtonActionWrapper(InlineKeyboardButton.WithSwitchInlineQueryChosenChat("Поделиться с друзьями 2", new())));

            await output.ShowContent("poooopa", knopka, null, null, _chatId);
        }

        [TestMethod]
        public void TestEncoder()
        {
            var encstr = "Мистер какер 🥹🥹🥹";

            var options = new JsonSerializerOptions()
            {
                Encoder = new EmojiEncoder()
            };

            Console.WriteLine($"{JsonSerializer.Serialize(encstr, options)}");
        }

        [TestMethod]
        public async Task TestSendMediaUrl()
        {
            var output = new TelegramOutput(null, new TelegramBotClient(_botToken), null, new TelegramOutputConfigure(), null);

            await output.ShowContent("Привет, это тест!", null, ["https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcR-3VPUH-VgejJMCyycXfRfGicjAESNkuhX7g&s"], null, _chatId);
        }
    }
}