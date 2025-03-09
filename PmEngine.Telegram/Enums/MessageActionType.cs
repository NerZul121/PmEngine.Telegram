namespace PmEngine.Telegram.Enums
{
    /// <summary>
    /// Что сделать с сообщением после выполнения кнопки?
    /// </summary>
    public enum MessageActionType
    {
        /// <summary>
        /// Дефолтный
        /// </summary>
        Default = -1,
        /// <summary>
        /// Ничего не делать
        /// </summary>
        None = 0,
        /// <summary>
        /// Удалить
        /// </summary>
        Delete = 1,
        /// <summary>
        /// Добавить текст с кнопки в конец сообщения
        /// </summary>
        Additional = 2,
        /// <summary>
        /// Обновить, или если не вышло - удалить. НЕ работает с медиа.
        /// </summary>у
        UpdateOrDelete = 3
    }
}