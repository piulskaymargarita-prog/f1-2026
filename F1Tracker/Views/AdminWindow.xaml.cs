using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
using F1Tracker.Data;
using F1Tracker.Helpers;

namespace F1Tracker.Views;

public partial class AdminWindow : Window
{
    private static readonly Regex LoginRegex = new(@"^[A-Za-z0-9_.\-]{3,32}$", RegexOptions.Compiled);
    private bool _logReady;

    public AdminWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            // Вторая линия обороны: даже если окно открыли в обход Navigator
            if (!Session.IsAdmin)
            {
                Navigator.GoTo(AppPage.Main, this);
                return;
            }
            SwitchTab("users");
        };
    }

    private void Grid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e) =>
        GridHelper.Configure(e);

    // ===================== Вкладки =====================
    private void Tab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { CommandParameter: string tab }) SwitchTab(tab);
    }

    private void SwitchTab(string tab)
    {
        foreach (var b in TabsPanel.Children.OfType<Button>())
            b.Tag = (string)b.CommandParameter == tab ? "active" : null;

        bool users = tab == "users";
        UsersPanel.Visibility = users ? Visibility.Visible : Visibility.Collapsed;
        LogPanel.Visibility = users ? Visibility.Collapsed : Visibility.Visible;
        Crumbs.Trail = "Главная/Администрирование/" + (users ? "Пользователи" : "Журнал действий");

        if (users) LoadUsers();
        else LoadLog();
    }

    // ===================== Пользователи =====================
    private void LoadUsers()
    {
        try
        {
            UsersGrid.ItemsSource = Database.ExecuteQuery(
                "SELECT user_id AS id, username AS login, role, created_at FROM users ORDER BY user_id;").DefaultView;
            ResetSelection();
        }
        catch (Exception ex)
        {
            Fail("Не удалось загрузить пользователей: " + ex.Message);
        }
    }

    /// <summary>Блок «Выбранный пользователь» доступен, только когда в таблице выбрана строка.</summary>
    private void ResetSelection()
    {
        SelectedPanel.IsEnabled = false;
        SelectedText.Text = "Выберите строку в таблице слева";
        SelPassBox.Clear();
    }

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not DataRowView r)
        {
            ResetSelection();
            return;
        }

        string login = (string)r["login"];
        string role = (string)r["role"];
        bool isMe = Convert.ToInt32(r["id"]) == Session.UserId;

        SelectedPanel.IsEnabled = true;
        SelectedText.Text = $"{login} · сейчас: {role}" + (isMe ? " · это вы" : "");
        SelRoleCombo.SelectedValue = role;
        SelPassBox.Clear();
        ErrorText.Text = "";
        InfoText.Text = "";
    }

    private bool TryGetSelected(out int id, out string login, out string role)
    {
        if (UsersGrid.SelectedItem is DataRowView r)
        {
            id = Convert.ToInt32(r["id"]);
            login = (string)r["login"];
            role = (string)r["role"];
            return true;
        }
        id = 0; login = ""; role = "";
        Fail("Сначала выберите пользователя в таблице.");
        return false;
    }

    private void Fail(string message)
    {
        ErrorText.Text = message;
        InfoText.Text = "";
    }

    private void Ok(string message)
    {
        ErrorText.Text = "";
        InfoText.Text = "✔ " + message;
    }

    // ---------- Блок 1: добавить ----------
    private void CreateUser_Click(object sender, RoutedEventArgs e)
    {
        string login = NewLoginBox.Text.Trim();
        string password = NewPassBox.Password;

        if (!LoginRegex.IsMatch(login))
        {
            Fail("Логин: 3–32 символа, только латиница, цифры и . _ -");
            return;
        }
        if (password.Length < 6)
        {
            Fail("Пароль — минимум 6 символов.");
            return;
        }
        if (NewRoleCombo.SelectedValue is not string role)
        {
            Fail("Выберите роль.");
            return;
        }

        try
        {
            string hash = BCrypt.Net.BCrypt.HashPassword(password);   // в БД — только хэш
            long id = Database.ExecuteInsert(
                "INSERT INTO users (username, password_hash, role) VALUES (@u, @h, @r);",
                Database.P("@u", login), Database.P("@h", hash), Database.P("@r", role));

            ActionLogger.Log("CREATE_USER", $"{login} ({role}), id {id}");
            NewLoginBox.Clear();
            NewPassBox.Clear();
            NewRoleCombo.SelectedIndex = 0;
            LoadUsers();
            Ok($"Пользователь «{login}» добавлен.");
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            Fail("Пользователь с таким логином уже существует.");
        }
        catch (Exception ex)
        {
            Fail("Ошибка БД: " + ex.Message);
        }
    }

    // ---------- Блок 2: выбранный пользователь ----------
    private void ApplyRole_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out int id, out string login, out string oldRole)) return;

        if (id == Session.UserId)
        {
            Fail("Нельзя менять роль собственной учётной записи.");
            return;
        }
        if (SelRoleCombo.SelectedValue is not string newRole)
        {
            Fail("Выберите новую роль.");
            return;
        }
        if (newRole == oldRole)
        {
            Fail("У пользователя уже эта роль.");
            return;
        }

        try
        {
            Database.ExecuteNonQuery("UPDATE users SET role = @r WHERE user_id = @id;",
                                     Database.P("@r", newRole), Database.P("@id", id));
            ActionLogger.Log("CHANGE_ROLE", $"{login}: {oldRole} → {newRole}");
            LoadUsers();
            Ok($"Роль «{login}» изменена: {oldRole} → {newRole}.");
        }
        catch (Exception ex)
        {
            Fail("Ошибка БД: " + ex.Message);
        }
    }

    private void SetPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out int id, out string login, out _)) return;

        string password = SelPassBox.Password;
        if (password.Length < 6)
        {
            Fail("Введите новый пароль: минимум 6 символов.");
            return;
        }

        try
        {
            Database.ExecuteNonQuery("UPDATE users SET password_hash = @h WHERE user_id = @id;",
                                     Database.P("@h", BCrypt.Net.BCrypt.HashPassword(password)),
                                     Database.P("@id", id));
            ActionLogger.Log("CHANGE_PASSWORD", login);   // сам пароль в журнал не пишем
            SelPassBox.Clear();
            Ok($"Новый пароль для «{login}» задан.");
        }
        catch (Exception ex)
        {
            Fail("Ошибка БД: " + ex.Message);
        }
    }

    private void DeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetSelected(out int id, out string login, out _)) return;

        if (id == Session.UserId)
        {
            Fail("Нельзя удалить собственную учётную запись.");
            return;
        }

        var answer = MessageBox.Show(
            $"Удалить пользователя «{login}»?\nЗаписи журнала с его действиями сохранятся.",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        try
        {
            Database.ExecuteNonQuery("DELETE FROM users WHERE user_id = @id;", Database.P("@id", id));
            ActionLogger.Log("DELETE_USER", $"{login} (id {id})");
            LoadUsers();
            Ok($"Пользователь «{login}» удалён.");
        }
        catch (Exception ex)
        {
            Fail("Ошибка БД: " + ex.Message);
        }
    }

    // ===================== Журнал =====================
    private void LoadLog()
    {
        try
        {
            LogGrid.ItemsSource = Database.ExecuteQuery(
                """
                SELECT l.log_id AS id, l.timestamp AS ts, COALESCE(u.username, '—') AS username,
                       l.action AS action, COALESCE(l.details, '') AS details
                FROM action_log l
                LEFT JOIN users u ON u.user_id = l.user_id
                ORDER BY l.log_id DESC
                LIMIT 500;
                """).DefaultView;
            _logReady = true;
            ApplyLogFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось загрузить журнал:\n" + ex.Message, "F1 Tracker",
                            MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ApplyLogFilter()
    {
        if (!_logReady || LogGrid.ItemsSource is not DataView view) return;
        GridHelper.ApplyTextFilter(view, LogFilterBox.Text, "username", "action", "details", "ts");
        LogCountText.Text = $"ПОКАЗАНО: {view.Count} (последние 500)";
    }

    private void LogFilterBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyLogFilter();
    private void RefreshLog_Click(object sender, RoutedEventArgs e) => LoadLog();
}