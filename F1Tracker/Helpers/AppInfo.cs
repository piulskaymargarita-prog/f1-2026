namespace F1Tracker.Helpers;

/// <summary>Сведения о проекте и авторе (единое место правки).</summary>
public static class AppInfo
{
    public const string AuthorName = "Пиульская Маргарита Алексеевна";      
    public const string Group = "Группа 22о";                  
    public const string Institution = "Государственное учреждение образования \"Витебский технологический колледж\"";    
    public const string Version = "1.0";
    public const string RepoUrl = "https://github.com/piulskaymargarita-prog/f1-2026";  

    public const string Description =
        "Десктопное приложение для учёта сезона Формулы-1 2026: справочники команд, пилотов " +
        "и Гран-при, ввод результатов сессий с автоматическим расчётом очков, турнирная таблица " +
        "пилотов и кубок конструкторов, отчёты в Excel и PDF, резервное копирование базы данных, " +
        "разграничение прав (admin / user) и журнал действий.";

    public static readonly string[] Stack =
    {
        "C# · .NET 8", "WPF · XAML", "SQLite", "Microsoft.Data.Sqlite",
        "BCrypt.Net-Next", "ClosedXML", "QuestPDF", "Git · GitHub"
    };
}