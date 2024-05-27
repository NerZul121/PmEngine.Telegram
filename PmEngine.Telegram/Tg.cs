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

            List<TagPos> tags = new();

            var result = text ?? "";

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

                var tagPos = new TagPos() { StartPos = entity.Offset, StartLength = startTag.Length, EndPos = entity.Length, EndLength = endTag.Length };

                result = result.Insert(tags.Where(t => t.StartPos <= entity.Offset).Sum(t => t.StartLength) + entity.Offset, startTag);
                result = result.Insert(tags.Where(t => t.StartPos <= entity.Offset).Sum(t => t.StartLength) + tags.Where(t => t.EndPos <= entity.Length).Sum(t => t.EndLength) + entity.Length + entity.Offset + startTag.Length, endTag);
                tags.Add(tagPos);
            }

            return result;
        }
    }

    class TagPos
    {
        public int StartPos { get; set; }
        public int StartLength { get; set; }
        public int EndPos { get; set; }
        public int EndLength { get; set; }
    }
}