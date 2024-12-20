using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class CVarManager
{
    // Список CVar и команд
    private static readonly Dictionary<string, CVar> CVars = new Dictionary<string, CVar>();
    private static readonly Dictionary<string, System.Action<string[]>> Commands = new Dictionary<string, System.Action<string[]>>();

    public static void Init()
    {
        // Пример регистрации CVar и команды
        RegisterCVar("player_speed", 5f, "Player movement speed");
        RegisterCommand("echo", args =>
        {
            if (args.Length == 0)
            {
                Debug.LogWarning("Usage: echo <message>");
                return;
            }
            string message = string.Join(" ", args);
            Debug.Log(message);
        });
        RegisterCommand("kill", (args) => { Game.Instance.allPlayers.Where(p => args[0] == p.Name).First().Kill(); });
    }

    public static void RegisterCVar(string name, object defaultValue, string description = "")
    {
        name = name.ToLower();
        if (CVars.ContainsKey(name))
        {
            Debug.LogWarning($"CVar {name} is already registered.");
            return;
        }
        CVars[name] = new CVar(name, defaultValue, description);
    }

    public static void RegisterCommand(string name, System.Action<string[]> action)
    {
        name = name.ToLower();
        if (Commands.ContainsKey(name))
        {
            Debug.LogWarning($"Command {name} is already registered.");
            return;
        }
        Commands[name] = action;
    }

    public static void ExecuteCommand(string input)
    {
        var parts = input.Split(' ');
        var commandName = parts[0].ToLower();
        var args = parts.Length > 1 ? parts[1..] : new string[0];

        if (Commands.TryGetValue(commandName, out var action))
        {
            action.Invoke(args);
        }
        else
        {
            Debug.LogWarning($"Command {commandName} not found.");
        }
    }
}
