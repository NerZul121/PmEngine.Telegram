using PmEngine.Telegram.Encoders;
using System.Text.Json;

namespace PmEngine.Telegram.Tests
{
    [TestClass]
    public class UnitTest1
    {
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
    }
}