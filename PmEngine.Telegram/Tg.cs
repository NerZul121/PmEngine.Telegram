using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace PmEngine.Telegram
{
    public static class Tg
    {
        public static string? TextWithEntities(string? text, MessageEntity[]? entities)
        {
            if (entities is null || text is null)
                return text;

            var result = text ?? "";
            int shift = 0;

            foreach (var entity in entities)
            {
                var tag = "";

                switch (entity.Type)
                {
                    case MessageEntityType.Bold: tag = "b"; break;
                    case MessageEntityType.Italic: tag = "i"; break;
                    case MessageEntityType.Underline: tag = "u"; break;
                    case MessageEntityType.Strikethrough: tag = "s"; break;
                    case MessageEntityType.Code: tag = "code"; break;
                    case MessageEntityType.Pre: tag = "pre"; break;
                    case MessageEntityType.Spoiler: tag = "span class=\"tg-spoiler\""; break;
                    case MessageEntityType.CustomEmoji: tag = $"tg-emoji emoji-id=\"{entity.CustomEmojiId}\""; break;
                    case MessageEntityType.TextLink: tag = $"a href=\"{entity.Url}\""; break;
                }

                if (string.IsNullOrEmpty(tag))
                    continue;

                var startTag = $"<{tag}>";
                var endTag = $"</{tag.Split(' ').First()}>";

                result = result.Insert(entity.Offset + shift, startTag);
                shift += startTag.Length;
                result = result.Insert(entity.Length + entity.Offset + shift, endTag);
                shift += endTag.Length;
            }

            return result;
        }
    }
}