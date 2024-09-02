using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MySql.Data.MySqlClient;

public class Host
{
    private string _botToken;
    private TelegramBotClient _bot;
    private string _connectionString;

    // Словник для ТИМЧАСОВОГО зберігання даних користувачів
    private Dictionary<long, UserData> users = new Dictionary<long, UserData>();

    // Структура даних користувачів
    private struct UserData
    {
        public long UserId { get; set; }
        public string Role { get; set; }
        public string City { get; set; }
        public string UserName { get; set; }
        public string TelegramUsername { get; set; }        
        public string Age { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string InstUrl { get; set; }
        public string ServicePrice { get; set; }
        public string Expected { get; set; }
        public bool RegistrationStatus { get; set; } 
    }

    // Конструктор з токеном боту
    public Host(string token)
    {
        _bot = new TelegramBotClient(token);
        _botToken = token; // Зберігаємо токен
        _connectionString = "Server=localhost;Port=3306;Database=mydatabase;User=root;Password=root_password;";
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
            // Отримання ID чату, тексту повідомлення та нікнейму
            long chatId = update.Message.Chat.Id; 
            string messageText = update.Message.Text ?? "";
            string telegramUsername = update.Message.From.Username ?? "";

            if (users.ContainsKey(chatId))
            {
                var user = users[chatId];

                if (user.Expected == "city")
                {
                    user.City = messageText;
                    users[chatId] = user;
                    user.Expected = null;

                    await _bot.SendTextMessageAsync(chatId, "Твоє місто - " + user.City);
                    await SaveUserData(user);
                    await UserRegistration(chatId);
                    return;
                }
                else if (user.Expected == "name")
                {
                    user.UserName = messageText;
                    users[chatId] = user;
                    user.Expected = null;

                    await _bot.SendTextMessageAsync(chatId, "Твоє ім'я - " + user.UserName);
                    await SaveUserData(user);
                    await UserRegistration(chatId);
                    return;
                }
                else if (user.Expected == "age")
                {
                    var selectedAge = messageText.Trim();
                    bool isNumeric = int.TryParse(selectedAge, out int age);

                    if (isNumeric && age >= 16 && age <= 100)
                    {
                        user.Age = selectedAge;
                        users[chatId] = user;
                        user.Expected = null;

                        await _bot.SendTextMessageAsync(chatId, "Твій вік - " + user.Age);
                        await SaveUserData(user);
                        await UserRegistration(chatId);
                        return;
                    }
                    else
                    {
                        await _bot.SendTextMessageAsync(chatId, "Будь ласка, введіть коректний вік.");
                        return;
                    }
                }
                else if (user.Expected == "description")
                {
                    user.Description = messageText;
                    users[chatId] = user;
                    user.Expected = null;

                    await _bot.SendTextMessageAsync(chatId, $"Твій опис анкети:\n{user.Description}");
                    await SaveUserData(user);
                    await UserRegistration(chatId);
                    return;
                }
                else if(user.Expected == "photo")
                {
                    if (update.Message.Photo != null)
                    {
                        // Отримання найбільшого розміру фото
                        string fileId = update.Message.Photo.Last().FileId;
                        
                        // Оновлення даних користувача
                        user.PhotoUrl = fileId;
                        users[chatId] = user;
                        user.Expected = null;

                        await _bot.SendTextMessageAsync(chatId, "Ваше фото збережено!");
                        await SaveUserData(user);
                        await UserRegistration(chatId);
                        return;
                    }
                }
                else if(user.Expected == "inst")
                {
                    user.InstUrl = messageText;
                    users[chatId] = user;
                    user.Expected = null;

                    await _bot.SendTextMessageAsync(chatId, $"Твоє посилання на інстаграм:\n{user.InstUrl}");
                    await SaveUserData(user);
                    await UserRegistration(chatId);
                    return;
                }
                else if (user.Expected == "price")
                {
                    user.ServicePrice = messageText;
                    users[chatId] = user;
                    user.Expected = null;

                    await _bot.SendTextMessageAsync(chatId, $"Встановлений прайс:\n{user.ServicePrice}");
                    await SaveUserData(user);
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
                                UserId = chatId,
                                TelegramUsername = telegramUsername
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

                            // Створення пустої клавіатури
                            var emptyKeyboard = new ReplyKeyboardRemove();

                            await _bot.SendTextMessageAsync(chatId, "Ви обрали роль: " + newUserData.Role, replyMarkup: emptyKeyboard);
                            await SaveUserData(newUserData);
                            await UserRegistration(chatId);
                        }
                    break;

                    case "1":
                    case "1. наново заповнити анкету":
                        await RestartRegistration(chatId, telegramUsername);
                    break;
                    case "2":
                    case "2. дивитись анкети інших":
                        await _bot.SendTextMessageAsync(chatId, "Функція ще в розробці.");
                    break;
                    case "3":
                    case "3. дивитись мою анкету":
                        await _bot.SendTextMessageAsync(chatId, "Ось як виглядає твоя анкета: ");
                        await SendUserProfile(chatId);
                    break;
                    case "4":
                    case "4. вимкнути мою анкету":
                        await _bot.SendTextMessageAsync(chatId, "Функція ще в розробці.");
                    break;
                    default:
                        await _bot.SendTextMessageAsync(chatId, "Будь ласка, введіть коректну команду.");
                        break;
                }
        }
    }

    private async Task SendStartMessage(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId,
            "Ласкаво просимо до боту знайомств для фотографів та моделей!\n" +
            "Щоб розпочати, оберіть свою роль:\n" +
            "фотограф - якщо ви фотограф\n" +
            "модель - якщо ви модель");
    }

    private async Task SendOldMessage(long chatId)
    {
        await _bot.SendTextMessageAsync(chatId,
            "Привіт! Схоже, ви вже зареєстровані.");
    }

    private async Task UserRegistration(long chatId)
    {
        var currentUserData = users[chatId];
        var RoleKeyboard = new ReplyKeyboardMarkup(
                new KeyboardButton[] { "Фотограф", "Модель" }
        );

        int step = 0;

        if (currentUserData.Role == null) { step = 1; }
        else if (currentUserData.City == null) { step = 2; }
        else if (currentUserData.UserName == null) { step = 3; }
        else if (currentUserData.Age == null) { step = 4; }
        else if (currentUserData.Description == null) { step = 5; }
        else if (currentUserData.PhotoUrl == null) { step = 6; }
        else if(currentUserData.InstUrl == null) { step = 7; }
        else if (currentUserData.ServicePrice == null) { step = 8;}
        else { step = 9; }

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
            case 6:
                currentUserData.Expected = "photo";
                users[chatId] = currentUserData;
                await _bot.SendTextMessageAsync(chatId, "Тепер надішли фото: це мають бути референси, якщо ти фотограф, та селфі - якщо модель.");
                break;
            case 7:
                currentUserData.Expected = "inst";
                users[chatId] = currentUserData;
                await _bot.SendTextMessageAsync(chatId, "Відправ посилання на свій інстаграм чи портфоліо.");
                break;
            case 8:
                currentUserData.Expected = "price";
                users[chatId] = currentUserData;
                await _bot.SendTextMessageAsync(chatId, "Вкажи тип послуги: безкоштовно чи платно.");
                break;
            case 9:
                currentUserData.RegistrationStatus = true;
                currentUserData.Expected = null;
                users[chatId] = currentUserData;
                await _bot.SendTextMessageAsync(chatId, "Реєстрацію завершено! Ось як виглядає твоя анкета:");
                await SendUserProfile(chatId);
                break;
            default:
                break;
        }
    }

    private async Task DeleteUserData(long chatId){
        try
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "DELETE FROM users WHERE UserId = @UserId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", chatId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user data: {ex.Message}");
        }
    }

    private void DeleteUserFromDictionary(long chatId)
    {
        if (users.ContainsKey(chatId))
        {
            users.Remove(chatId);
        }
    }

    // Оновити анкету / повторна реєстр. користувача
    private async Task RestartRegistration(long chatId, string telegramUsername)
    {
        // Видалення попередніх даних користувача з бази даних
        await DeleteUserData(chatId);
        
        // Видалення попередніх даних користувача зі словника
        DeleteUserFromDictionary(chatId);
        
        // Створення нової структури даних користувача
        users[chatId] = new UserData()
        {
            UserId = chatId,
            TelegramUsername = telegramUsername
        };

        // Початок реєстрації заново
        await SendStartMessage(chatId);
        await UserRegistration(chatId);
    }

    // Зберегти дані користувача в БД.
    private async Task SaveUserData(UserData user)
    {
        try
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "INSERT INTO users (UserId, TelegramUsername, Role, City, UserName, Age, Description, PhotoUrl, InstUrl, ServicePrice, Expected, RegistrationStatus) " +
                               "VALUES (@UserId, @TelegramUsername, @Role, @City, @UserName, @Age, @Description, @PhotoUrl, @InstUrl, @ServicePrice, @Expected, @RegistrationStatus) " +
                               "ON DUPLICATE KEY UPDATE " +
                               "TelegramUsername = @TelegramUsername, Role = @Role, City = @City, UserName = @UserName, Age = @Age, Description = @Description, " +
                               "PhotoUrl = @PhotoUrl, InstUrl = @InstUrl, ServicePrice = @ServicePrice, Expected = @Expected, RegistrationStatus = @RegistrationStatus";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", user.UserId);
                    command.Parameters.AddWithValue("@TelegramUsername", user.TelegramUsername);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    command.Parameters.AddWithValue("@City", user.City);
                    command.Parameters.AddWithValue("@UserName", user.UserName);
                    command.Parameters.AddWithValue("@Age", user.Age);
                    command.Parameters.AddWithValue("@Description", user.Description);
                    command.Parameters.AddWithValue("@PhotoUrl", user.PhotoUrl);
                    command.Parameters.AddWithValue("@InstUrl", user.InstUrl);
                    command.Parameters.AddWithValue("@ServicePrice", user.ServicePrice);
                    command.Parameters.AddWithValue("@Expected", user.Expected);
                    command.Parameters.AddWithValue("@RegistrationStatus", user.RegistrationStatus);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving user data: {ex.Message}");
        }
    }

    // Надіслати користувачу його анкету 
    private async Task SendUserProfile(long chatId)
    {
        // Отримати дані користувача з бази даних
        UserData user = await GetUserDataFromDatabase(chatId);

        // Перевірка, чи є такі дані в базі даних
        if (user.UserId != 0)
        {
            if (!string.IsNullOrEmpty(user.PhotoUrl))
            {
                // Формування тексту з даними користувача
                var userInfo = $"{user.UserName}, " +
                    $"{user.City}, " +
                    $"{user.Age} - " +
                    $"{user.Description}\n\n" +
                    $"Ціна послуги: {user.ServicePrice}\n" +
                    $"Переглянути портфоліо: {user.InstUrl}";

                InputFile userPhotoAsFile = new InputFileId(user.PhotoUrl);
                await _bot.SendPhotoAsync(chatId, userPhotoAsFile, caption: userInfo);
            }
            else
            {
                await _bot.SendTextMessageAsync(chatId, "Ваша анкета ще не містить фото.");
            }

            await SendMainMenu(chatId);
        }
        else
        {
            await _bot.SendTextMessageAsync(chatId, "Анкета користувача не знайдена.");
        }
    }

    private async Task<UserData> GetUserDataFromDatabase(long chatId)
    {
        UserData user = new UserData();

        try
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM users WHERE UserId = @UserId LIMIT 1";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", chatId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            user.UserId = chatId;
                            user.TelegramUsername = reader["TelegramUsername"].ToString();
                            user.Role = reader["Role"].ToString();
                            user.City = reader["City"].ToString();
                            user.UserName = reader["UserName"].ToString();
                            user.Age = reader["Age"].ToString();
                            user.Description = reader["Description"].ToString();
                            user.PhotoUrl = reader["PhotoUrl"].ToString();
                            user.InstUrl = reader["InstUrl"].ToString();
                            user.ServicePrice = reader["ServicePrice"].ToString();
                            user.Expected = reader["Expected"].ToString();
                            user.RegistrationStatus = Convert.ToBoolean(reader["RegistrationStatus"]);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching user data: {ex.Message}");
        }

        return user;
    }

    private async Task SendMainMenu(long chatId){
        var menuKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton("1. Наново заповнити анкету"),
            new KeyboardButton("2. Дивитись анкети інших"),
            new KeyboardButton("3. Дивитись мою анкету"),
            new KeyboardButton("4. Вимкнути мою анкету")
        })
        {
            ResizeKeyboard = true
        };

        await _bot.SendTextMessageAsync(
            chatId, "1. Наново заповнити анкету"
            + "\n2. Дивитись анкети інших"
            + "\n3. Дивитись мою анкету"
            + "\n4. Вимкнути мою анкету", replyMarkup: menuKeyboard);
    }
}