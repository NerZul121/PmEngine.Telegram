# PMEngine.Telegram
Модуль для работы с Telegram

## Используемые переменные

В модуле используются следующие переменные среды:
```
BOT_TOKEN - Токен ТГ бота
HOST_URL - URL приложения, куда будут приходить запросы от Telegram
```

## Подключение

### Подключение модуля

Для подключения модуля необходимо просто добавить его в список сервисов

```
builder.Services.AddTelegramModule();
```

Так же при добавлении можно сконфигурировать модуль, например:

```
builder.Services.AddTelegramModule(tg => tg.DefaultInLineMessageAction = MessageActionType.Delete);
```


### Настройка веб-хука

Для настройки веб-хука необходимо выполнить следующее

```
builder.Services.AddHttpClient("tgwebhook").AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(envBotToken, httpClient));

...

app.UseEndpoints(ep =>
{
    ep.MapControllerRoute(name: "tgwebhook",
        pattern: $"TGBot/{envBotToken}",
        new { controller = "TGBot", action = "Post" });
    ep.MapControllers();
});
```

## Создание контроллера

Для приема запросов от Telegram необходимо добавить в приложение свой контроллер. Пример простого контроллера приведен ниже:

```
public class TGBotController : ControllerBase
{
    private readonly ILogger<TGBotController> _logger;
    private readonly ITelegramBotClient _client;
    private readonly IServiceProvider _serviceProvider;
	
	public TGBotController(IServiceProvider services, ILogger<TGBotController> logger, ITelegramBotClient botClient)
	{
		_logger = logger;
		_client = botClient;
		_serviceProvider = services;
	}
	
	[HttpPost]
	public async Task Post([FromBody] Update update)
	{
		var tgcontroller = new BaseTGController();
		await tgcontroller.Post(update, _client, _logger, _serviceProvider);
	}
}
```

Он использует класс ``BaseTGController`` для обработки сообщений. Если вам необходимо обернуть обработку по-особому, то вы можете изменить логику контроллера на свою, опирась на код этого класса.