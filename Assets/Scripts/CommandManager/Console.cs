using UnityEngine;
using System.Collections.Generic;

public class Console : MonoBehaviour
{
    private string input = ""; // ������� ����
    private bool isConsoleOpen = false; // ��������� �������
    private List<string> logMessages = new List<string>(); // ��� ���������
    private Vector2 scrollPosition = Vector2.zero; // ������� ������� ��� ������ ���������
    private List<string> commandHistory = new List<string>(); // ������� ������
    private int historyIndex = -1; // ������ ������� ������� � �������

    private Rect consoleRect; // ������ ������� �� ������
    private bool isTextFieldFocused = false; // ����� �� ���� �����

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // ������� ����������� ����� �������
    }

    private void Start()
    {
        CVarManager.Init();
        LogMessage("Console initialized.");
        consoleRect = new Rect(10, 10, Screen.width - 20, Screen.height - 20);
    }

    private void Update()
    {
        // �������/������� �������
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            isConsoleOpen = !isConsoleOpen;

            // ����� ������ � ������� ��� ��������
            if (isConsoleOpen)
            {
                input = "";
                historyIndex = -1;
                isTextFieldFocused = false;
            }
        }

        if (isConsoleOpen)
        {
            // ������������� ���������� �������
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // ��������� �������
            if (Event.current != null && Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                ExecuteInput();
                isTextFieldFocused = false; // ������� ����� ����� ���������� �������
            }

            // ������� �������
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                isConsoleOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // ������� ������ (������� �����/����)
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

        // �������� ������� �������
        GUILayout.BeginArea(consoleRect, GUI.skin.box);
        GUILayout.Label("Console:");

        // ������ ��� ������ �����
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Screen.height / 2));
        foreach (var message in logMessages)
        {
            GUILayout.Label(message);
        }
        GUILayout.EndScrollView();

        // ���� �����
        GUI.SetNextControlName("ConsoleInput");
        input = GUILayout.TextField(input);

        // ������������� � ����������� �����
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

        LogMessage($"> {input}"); // ��������� ������� � ���
        CVarManager.ExecuteCommand(input); // ��������� ������� ����� CVarManager
        commandHistory.Add(input); // ��������� ������� � �������
        historyIndex = -1; // ���������� ������ �������
        input = ""; // ������� ���� �����
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
        scrollPosition.y = float.MaxValue; // ��������� ���� ��� ���������� ����� ���������
    }
}
