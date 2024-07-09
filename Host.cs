using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

public class Host
{
    // Подія для обробки нових повідомлень
    public Action<ITelegramBotClient, Update>? OnMessage;

    // Приватний екземпляр TelegramBotClient
    private TelegramBotClient _bot;

    // Словник для зберігання даних користувачів
    private Dictionary<long, UserData> users = new Dictionary<long, UserData>();

    // Структура зберігання даних користувачів
    private struct UserData
    {
        public long UserId { get; set; }
        public string Role { get; set; }
        public string City { get; set; }
        public string UserName { get; set; }
        public string Age { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string InstUrl { get; set; }
        public string ServicePrice { get; set; }
        public string Expected { get; set; }
    }

    // Конструктор з токеном боту
    public Host(string token)
    {
        _bot = new TelegramBotClient(token);
    }

    // Запуск боту
    public void Start()
    {
        // Реєстрація обробників оновлень
        _bot.StartReceiving(UpdateHandler, ErrorHandler);

        // Повідомлення для перевірки в терміналі 
        Console.WriteLine("Бот запущено!");
    }

    // Обробник помилок
    private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
    {
        Console.WriteLine($"Помилка: {exception.Message}");
        await Task.CompletedTask;
    }

    // Обробник оновлень
    private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
    {
        // Перевірка типу оновлення
        if (update.Message != null)
        {
            // Отримання ID чату та тексту повідомлення
            long chatId = update.Message.Chat.Id;
            string messageText = update.Message.Text ?? "";

            if (users.ContainsKey(chatId))
                {
                    var user = users[chatId];
                    if (user.Expected == "city")
                    {
                        var selectedCity = messageText;
                        user.City = selectedCity;
                        users[chatId] = user;
                        await _bot.SendTextMessageAsync(chatId, "Твоє місто - " + user.City);
                        user.Expected = null; 
                        await UserRegistration(chatId);
                        return;
                        // if user.input_expected == city! 
                        // if user.input_expected == name! 
                    }
                    else if (user.Expected == "name")
                    {
                        var selectedName = messageText;
                        user.UserName = selectedName;
                        users[chatId] = user;
                        await _bot.SendTextMessageAsync(chatId, "Твоє ім'я - " + user.UserName);
                        user.Expected = null;
                        await UserRegistration(chatId);
                        return;
                    }
                    else if (user.Expected == "age")
                    {
                        var selectedAge = messageText.Trim(); // Видалити пробіли з початку і кінця рядка
                        
                        // Перевірка, чи рядок містить лише цифри
                        bool isNumeric = int.TryParse(selectedAge, out int age);
                        if (isNumeric && age >= 16 && age <= 100)
                        {
                            user.Age = selectedAge;
                            users[chatId] = user;
                            await _bot.SendTextMessageAsync(chatId, "Твій вік - " + user.Age);
                            user.Expected = null;
                            await UserRegistration(chatId);
                        }
                        else
                        {
                            await _bot.SendTextMessageAsync(chatId, "Будь ласка, введіть коректний вік.");
                        }
                        return;
                    }
                    else if (user.Expected == "description")
                    {
                        var selectedDescription = messageText;
                        user.Description = selectedDescription;
                        users[chatId] = user;
                        await _bot.SendTextMessageAsync(chatId, $"Твій опис анкети:\n{user.Description}");                        
                        user.Expected = null; await UserRegistration(chatId); 
                        await UserRegistration(chatId);
                        return;
                    }
                }

            // Обробка команди користувача
            switch (messageText.ToLower())
            {
                case "/start":
                // Перевірка, чи є користувач новим 
                    if (users.ContainsKey(chatId))
                        {
                            await SendOldMessage(chatId);
                        }
                    else
                        {
                            // Створення нової структури даних користувача
                            users[chatId] = new UserData()
                            {
                                UserId = chatId
                            };

                            await SendStartMessage(chatId);
                            await UserRegistration(chatId);
                            
                        }
                break;
        
                case "фотограф":
                case "модель":
                    if (users.ContainsKey(chatId))
                        {
                            string selectedRole = messageText.ToLower();

                            /// Отримання об'єкта UserData
                            var newUserData = users[chatId];
                            // Оновлення властивості Role
                            newUserData.Role = selectedRole;
                            // Збереження змін
                            users[chatId] = newUserData;

                            await _bot.SendTextMessageAsync(chatId, "Ви обрали роль: " + selectedRole);
                            // Продовжити реєстрацію
                            await UserRegistration(chatId);
                        }
                break;


                default:
                    await _bot.SendTextMessageAsync(chatId, "Будь ласка, введіть коректну команду.");                    
                break;
            }
        }
    }

    // Надсилання вітального повідомлення новому користувачу 
    private async Task SendStartMessage(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId, // РОЗМІСТИТИ ПОВІОМЛЕННЯ-ПРИВІТАННЯ І ПОТІМ ПРАВИЛА
            "Ласкаво просимо до боту знайомств для фотографів та моделей!\n" +
            "Щоб розпочати, оберіть свою роль:\n" +
            "фотограф - якщо ви фотограф\n" +
            "модель - якщо ви модель");
    }

    // Надсилання вітального повідомлення олду
    private async Task SendOldMessage(long chatId)
    {
        // РОЗМІСТИТИ ОСТАННЮ АНКЕТУ КОРИСТУВАЧА І ДАТИ МЕНЮ КЕРУВАННЯ
        await _bot.SendTextMessageAsync(chatId,
            "привіт стара миша!!!!");
    }

    // Реєстрація ролі користувача 
    private async Task UserRegistration (long chatId)
    {
        var RoleKeyboard = new ReplyKeyboardMarkup (
            new KeyboardButton [] { "Фотограф", "Модель"}
        );

        // Отримання даних про користувача з словника
        var currentUserData = users[chatId];

        // Визначити поточний крок реєстрації
        int step = 0;
        if (currentUserData.Role == null)
        {
            step = 1; // Запит ролі
        }
        else if (currentUserData.City == null)
        {
            step = 2; // Запит міста
        }
        else if (currentUserData.UserName == null)
        {
            step = 3; // Запит імені 
        }
        else if (currentUserData.Age == null)
        {
            step = 4; // Запит віку
        }
        else if (currentUserData.Description == null)
        {
            step = 5; // Запит опису анкети
        }
        else if(currentUserData.PhotoUrl == null)
        {
            step = 6;
        }
        else
        {
            step = 7; // Реєстрація завершена. ВИВЕСТИ ПОВІДОМЛЕННЯ 
        }

        switch (step)
        {
            case 1:
            await _bot.SendTextMessageAsync(chatId, "Обери свою роль: ", replyMarkup: RoleKeyboard);
            break;

            case 2:
            currentUserData.Expected = "city";
            users[chatId] = currentUserData;
            await _bot.SendTextMessageAsync(chatId, "Введи назву міста!");
            break;

            case 3:
            currentUserData.Expected = "name";
            users[chatId] = currentUserData;
            await _bot.SendTextMessageAsync(chatId, "Введи своє ім'я!");
            break;

            case 4:
            currentUserData.Expected = "age";
            users[chatId] = currentUserData;
            await _bot.SendTextMessageAsync(chatId, "Введи свій вік!");
            break;

            case 5:
            currentUserData.Expected = "description";
            users[chatId] = currentUserData;
            await _bot.SendTextMessageAsync(chatId, "Введи опис анкети!");
            break;

            default:
            break;
        }
    }

}