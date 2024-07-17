using Telegram.Bot;
using Telegram.Bot.Types;
using MySql.Data.MySqlClient;

internal class Program
{
    private static void Main(string[] args)
    {
        // Отримуємо токен боту
        string token = "7225909476:AAH_qzaDDhC2mobZ6Oqyjnnc3r-W8tbp66k";

        // Створення екземпляра боту
        Host bot = new Host(token);

        // Запуск боту
        bot.Start();

        // Очікування команди користувача
        Console.ReadLine();
    }

}
