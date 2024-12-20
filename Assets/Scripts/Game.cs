using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Player;
using System.IO;

public class Game : MonoBehaviour
{
    public enum GameMode
    {
        TeamDeathmatch,
        Deathmatch,
        FlagCapture,
        Domination
    }
    public static Game Instance;
    public GameMode Mode = GameMode.FlagCapture;

    public GameObject playerPrefab;
    public GameObject botPrefab;

    // Список всех спавн-узлов на сцене
    public List<NodeSpawn> spawnNodes;

    // Словарь для флагов каждой команды
    public Dictionary<Team, NodeFlag> flags { get; private set; } = new Dictionary<Team, NodeFlag>();

    public int maxPlayersPerTeam = 5;
    public float respawnTime = 3f; // Время до возрождения

    public List<Player> redTeamPlayers = new List<Player>();
    public List<Player> blueTeamPlayers = new List<Player>();
    public List<Player> greenTeamPlayers = new List<Player>();
    public List<Player> yellowTeamPlayers = new List<Player>();

    public List<Player> allPlayers = new List<Player>();

    public bool noStand = false;

    public Dictionary<Team, int> teamScores = new Dictionary<Team, int>();

    private void Awake()
    {
        // Синглтон для GameManager
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Находим все спавн-узлы на сцене и группируем их по командам
        spawnNodes = FindObjectsOfType<NodeSpawn>().ToList();

        // Находим все флаги на сцене и добавляем их в словарь
        NodeFlag[] allFlags = FindObjectsOfType<NodeFlag>();
        foreach (var flag in allFlags)
        {
            Team flagTeam = GetFlagTeam(flag);  // Получаем команду, которой принадлежит флаг
            if (!flags.ContainsKey(flagTeam))
            {
                flags.Add(flagTeam, flag );
            }
            if (Mode != GameMode.FlagCapture)
            {
                flag.gameObject.SetActive(false);
            }
        }
        teamScores.Add(Team.Blue, 0);
        teamScores.Add(Team.Red, 0);
        teamScores.Add(Team.Yellow, 0);
        teamScores.Add(Team.Green, 0);
    }

    private void Start()
    {
        // Пример: создаём игру с 10 игроками, где 1 из них — это реальный игрок.
        SpawnPlayersForTeams(10);
        foreach (var player in allPlayers)
        {
            player.OnDead += HandlePlayerDeath;
        }
        foreach (var kvpFlag in flags)
        {
            kvpFlag.Value.OnDelivered += OnFlagDelivered;
        }
    }
    private void OnFlagDelivered(NodeFlag flag, Player deliverer) 
    {
        teamScores[deliverer.PlayerTeam]++;
    }
    private void OnDisable()
    {
        foreach (var player in allPlayers)
        {
            player.OnDead -= HandlePlayerDeath;
        }
    }
    private void HandlePlayerDeath(Player player, Player killer)
    {
        Debug.Log($"{player.Name} died. Respawning in {respawnTime} seconds.");
        if (Mode == GameMode.TeamDeathmatch)
        {
            teamScores[killer.PlayerTeam]++;
        }
        // Отправляем уведомление клиенту
        NotifyClientOfDeath(player, killer, respawnTime);

        // Начинаем процесс возрождения
        StartCoroutine(RespawnCoroutine(player));
    }
    private void NotifyClientOfDeath(Player player, Player killer, float respawnTime)
    {
        // Вызов RPC, отправка сообщения или события для клиента
        Debug.Log($"Notifying client: {player.Name} died. Respawn in {respawnTime} seconds.");
    }

    // Уведомление клиента о возрождении
    private void NotifyClientOfRespawn(Player player)
    {
        // Вызов RPC, отправка сообщения или события для клиента
        Debug.Log($"Notifying client: {player.Name} has respawned.");
    }
    private void SpawnPlayersForTeams(int totalPlayers)
    {
        // Определяем доступные команды на основе существующих спавн-узлов
        var teamsToSpawn = new List<Team>();

        if (spawnNodes.Exists(sn => sn.teamSpawn == Team.Red)) teamsToSpawn.Add(Team.Red);
        if (spawnNodes.Exists(sn => sn.teamSpawn == Team.Blue)) teamsToSpawn.Add(Team.Blue);
        if (spawnNodes.Exists(sn => sn.teamSpawn == Team.Green)) teamsToSpawn.Add(Team.Green);
        if (spawnNodes.Exists(sn => sn.teamSpawn == Team.Yellow)) teamsToSpawn.Add(Team.Yellow);

        if (teamsToSpawn.Count == 0)
        {
            Debug.LogError("Нет доступных спавн-узлов для команд.");
            return;
        }

        int playersPerTeam = Mathf.CeilToInt((float)totalPlayers / teamsToSpawn.Count);
        int spawnedPlayers = 0;

        foreach (var team in teamsToSpawn)
        {
            for (int i = 0; i < playersPerTeam; i++)
            {
                if (spawnedPlayers >= totalPlayers)
                    break;

                // Первый игрок — реальный игрок, остальные — боты
                bool isRealPlayer = (spawnedPlayers == 0);
                GameObject prefabToSpawn = isRealPlayer ? playerPrefab : botPrefab;

                // Создаём игрока
                SpawnPlayer(team, prefabToSpawn);
                spawnedPlayers++;
            }
        }
    }

    public void SpawnPlayer(Team team, GameObject prefabToSpawn)
    {
        // Находим спавн-узел для команды
        NodeSpawn spawnNode = GetRandomSpawnNodeForTeam(team);

        // Спавним игрока
        Player newPlayer = InstantiatePlayer(spawnNode.transform.position, prefabToSpawn, team);
        newPlayer.SetName($"Player_{allPlayers.Count}");

        // Добавляем в соответствующую команду
        AddPlayerToTeam(newPlayer, team);

        allPlayers.Add(newPlayer);
    }

    private Player InstantiatePlayer(Vector3 spawnPosition, GameObject prefab, Team team)
    {
        GameObject playerObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
        Player player = playerObject.GetComponent<Player>();
        player.SetTeam(team);
        GameObject glockPrefab = Resources.Load<GameObject>("Pickups/Glock");
        GameObject glock = Instantiate(glockPrefab, transform.position, Quaternion.identity);
        player.PickUpWeapon(glock.GetComponent<BaseWeapon>());
        return player;
    }

    private void AddPlayerToTeam(Player player, Team team)
    {
        switch (team)
        {
            case Team.Red:
                redTeamPlayers.Add(player);
                player.GetComponent<SpriteRenderer>().color = Color.red;
                break;
            case Team.Blue:
                blueTeamPlayers.Add(player);
                player.GetComponent<SpriteRenderer>().color = Color.blue;
                break;
            case Team.Green:
                greenTeamPlayers.Add(player);
                player.GetComponent<SpriteRenderer>().color = Color.green;
                break;
            case Team.Yellow:
                yellowTeamPlayers.Add(player);
                player.GetComponent<SpriteRenderer>().color = Color.yellow;
                break;
            default:
                break;
        }
    }

    private NodeSpawn GetSpawnNodeForTeam(Team team)
    {
        return spawnNodes.FirstOrDefault(sn => sn.teamSpawn == team);
    }

    private NodeSpawn GetRandomSpawnNodeForTeam(Team team)
    {
        // Получаем все узлы для заданной команды
        var teamSpawnNodes = spawnNodes.Where(sn => sn.teamSpawn == team).ToList();

        // Если нет узлов для этой команды, возвращаем null
        if (teamSpawnNodes.Count == 0)
        {
            return null;
        }

        // Выбираем случайный узел
        int randomIndex = UnityEngine.Random.Range(0, teamSpawnNodes.Count);
        return teamSpawnNodes[randomIndex];
    }


    public void RespawnPlayer(Player player)
    {
        // Спавним игрока в его команде
        NodeSpawn spawnNode = GetRandomSpawnNodeForTeam(player.PlayerTeam);
        player.transform.position = spawnNode.transform.position;
        player.Revive();
    }

    public void HandleTeamBalance()
    {
        // Автобаланс команд, если количество игроков в одной из команд сильно отличается
        if (redTeamPlayers.Count > blueTeamPlayers.Count + 1)
        {
            Player playerToMove = redTeamPlayers.Last();
            redTeamPlayers.Remove(playerToMove);
            blueTeamPlayers.Add(playerToMove);
            playerToMove.SetTeam(Team.Blue);
            RespawnPlayer(playerToMove);
        }
        else if (blueTeamPlayers.Count > redTeamPlayers.Count + 1)
        {
            Player playerToMove = blueTeamPlayers.Last();
            blueTeamPlayers.Remove(playerToMove);
            redTeamPlayers.Add(playerToMove);
            playerToMove.SetTeam(Team.Red);
            RespawnPlayer(playerToMove);
        }
        // Повторить для других команд, если требуется
    }

    public NodeFlag GetCapturedFlag(Team team)
    {
        if (flags.ContainsKey(team)) return flags[team]; else return null;
    }
    public NodeFlag GetRandomFlagExceptOwn(Team ownTeam)
    {
        // Фильтруем флаги, исключая свою команду
        var otherFlags = flags.Where(kvp => kvp.Key != ownTeam).Select(kvp => kvp.Value).ToList();

        // Если нет других флагов, возвращаем null
        if (otherFlags.Count == 0)
            return null;

        // Выбираем случайный флаг из оставшихся
        int randomIndex = UnityEngine.Random.Range(0, otherFlags.Count);
        return otherFlags[randomIndex];
    }
    // Определяет команду по флагу
    private Team GetFlagTeam(NodeFlag flag)
    {
        // В зависимости от того, как устроено ваше представление флагов,
        // вы можете реализовать логику, которая будет определять команду флага
        if (flag != null)
        {
            return flag.team;
            // Аналогично для других команд
        }
        return Team.None;
    }

    // Вызывается, когда игрок умирает (пример)
    public void PlayerDied(Player player)
    {
        StartCoroutine(RespawnCoroutine(player));
    }

    private IEnumerator RespawnCoroutine(Player player)
    {
        // Возрождение после небольшой задержки
        yield return new WaitForSeconds(5f);
        RespawnPlayer(player);
    }
    private void OnGUI()
    {
        // Настройки для шрифта и размера текста
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.white;

        // Позиция для начала вывода текста
        float startX = 10f;
        float startY = 10f;
        float spacing = 30f;

        // Отображение очков команд
        int index = 0;
        foreach (var teamScore in teamScores)
        {
            GUI.Label(
                new Rect(startX, startY + index * spacing, 200f, 30f),
                $"{teamScore.Key.ToString()}: {teamScore.Value}",
                style
            );
            index++;
        }
    }
}
