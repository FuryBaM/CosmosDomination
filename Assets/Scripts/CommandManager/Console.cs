using UnityEngine;
using System.Collections.Generic;

public class Console : MonoBehaviour
{
    private string input = ""; // Текущий ввод
    private bool isConsoleOpen = false; // Состояние консоли
    private List<string> logMessages = new List<string>(); // Лог сообщений
    private Vector2 scrollPosition = Vector2.zero; // Позиция скролла для вывода сообщений
    private List<string> commandHistory = new List<string>(); // История команд
    private int historyIndex = -1; // Индекс текущей команды в истории

    private Rect consoleRect; // Размер консоли на экране
    private bool isTextFieldFocused = false; // Фокус на поле ввода

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // Консоль сохраняется между сценами
    }

    private void Start()
    {
        CVarManager.Init();
        LogMessage("Console initialized.");
        consoleRect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
    }

    private void Update()
    {
        // Открыть/закрыть консоль
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            isConsoleOpen = !isConsoleOpen;

            // Сброс фокуса и истории при открытии
            if (isConsoleOpen)
            {
                input = "";
                historyIndex = -1;
                isTextFieldFocused = false;
            }
        }

        if (isConsoleOpen)
        {
            // Заблокировать управление игроком
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Выполнить команду
            if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                ExecuteInput();
                isTextFieldFocused = false; // Убираем фокус после выполнения команды
            }

            // Закрыть консоль
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isConsoleOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // История команд (стрелки вверх/вниз)
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                BrowseHistory(-1);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                BrowseHistory(1);
            }
        }
    }

    private void OnGUI()
    {
        if (!isConsoleOpen) return;

        GUI.skin.textField.alignment = TextAnchor.MiddleLeft;

        // Основная область консоли
        GUILayout.BeginArea(consoleRect, GUI.skin.box);
        GUILayout.Label("Console:");

        // Секция для вывода логов
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Screen.height / 2));
        foreach (var message in logMessages)
        {
            GUILayout.Label(message);
        }
        GUILayout.EndScrollView();

        // Поле ввода
        GUI.SetNextControlName("ConsoleInput");
        input = GUILayout.TextField(input);

        // Устанавливаем и отслеживаем фокус
        if (!isTextFieldFocused)
        {
            GUI.FocusControl("ConsoleInput");
            isTextFieldFocused = true;
        }

        GUILayout.EndArea();
    }

    private void ExecuteInput()
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        LogMessage($"> {input}"); // Добавляем команду в лог
        CVarManager.ExecuteCommand(input); // Выполняем команду через CVarManager
        commandHistory.Add(input); // Сохраняем команду в историю
        historyIndex = -1; // Сбрасываем индекс истории
        input = ""; // Очищаем поле ввода
    }

    private void BrowseHistory(int direction)
    {
        if (commandHistory.Count == 0) return;

        historyIndex = Mathf.Clamp(historyIndex + direction, 0, commandHistory.Count - 1);
        input = commandHistory[historyIndex];
    }

    public void LogMessage(string message)
    {
        logMessages.Add(message);
        scrollPosition.y = float.MaxValue; // Прокрутка вниз при добавлении новых сообщений
    }
}
