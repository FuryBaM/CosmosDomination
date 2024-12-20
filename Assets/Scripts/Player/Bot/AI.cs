using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using static UT;
using UnityEditor;

public class AI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform handTransform;
    private Pathfinding2D pathfinding;
    private NodeWaypoint nextWp;
    private NodeWaypoint curWp;
    private List<NodeWaypoint> path;
    private float wpTimer = 0;
    private bool defendingFlag;
    private float focusX;
    private float focusY;
    private float stand;
    private float nostand;
    public bool duck;
    private float getTargetTimer = 0;
    private float getTargetEvent = 4;
    private float getTargetMax = 4;
    private float shootSpd;
    private float standNormal;
    private float standTarget;
    private float standFlag;
    private float shotChance;
    private float aimSpeed;
    private uint diffRev;
    public uint diff = 5; // difficulty

    private Player target = null;

    private float aimX = 0f;
    private float aimY = 0f;

    private void Start()
    {
        if (player == null) player = GetComponent<Player>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (handTransform == null) handTransform = player.Hand;
        player.OnDead += HandleDeath;

        pathfinding = FindObjectOfType<Pathfinding2D>();
        path = new List<NodeWaypoint>();
        aimX = playerMovement.transform.position.x + 100f;
        aimY = playerMovement.transform.position.y - 50f;
        getTargetEvent = 4f;
        getTargetTimer = 0;
        target = null;
        List<NodeSpawn> spawns = new List<NodeSpawn>(Game.Instance.spawnNodes);
        spawns.Sort((a, b) => Vector2.Distance(transform.position, a.transform.position).CompareTo(Vector2.Distance(transform.position, b.transform.position)));
        GetNextWaypoint(spawns[0].waypoint, true);
        SetDiffStats();
    }

    public void SetDiffStats()
    {
        diffRev = 10 - diff;
        standNormal = 0.01f * (diffRev * 0.3f);
        standTarget = 0.03f * (diffRev * 0.3f);
        standFlag = 0.005f * (diffRev * 0.3f);
        shotChance = diff * 0.29f + 0.1f;
        if (diff == 10)
        {
            shotChance = 1000;
        }
        aimSpeed = 0.3f * (diff * 0.1f + 0.1f);
    }

    private void HandleDeath(Player victim, Player killer)
    {
        path.Clear();
    }

    private void GetNextWaypoint(NodeWaypoint next = null, bool shouldJump = false)
    {
        wpTimer = 0;
        if (!shouldJump && playerMovement.Jumping && Mathf.Abs(transform.position.y - nextWp.transform.position.y) > 2f)
        {
            Debug.Log("Condition is not pass");
            return;
        }
        if (next != null)
        {
            curWp = nextWp;
            nextWp = next;
            Debug.Log("Move to spawn node");
        }
        else
        {
            curWp = nextWp;
            if (Game.Instance.Mode == Game.GameMode.FlagCapture && (path != null && path.Count == 0))
            {
                MoveToObjective();
            }
            if (Mathf.Abs(transform.position.y - nextWp.transform.position.y) <= 2f)
            {
                if (Mathf.Abs(transform.position.y - nextWp.transform.position.y) < 2f)
                {
                    if (path != null && path.Count == 0)
                    {
                        nextWp = curWp.Connections[Random.Range(0, curWp.Connections.Count)];
                    }
                    else
                    {
                        nextWp = path[0];
                        path.RemoveAt(0);
                    }
                }
            }
            else
            {
                GetClosestWp();
            }
        }
        //if (curWp != null && Game.Instance.Extra != null && Game.Instance.Extra.Speak != null && Game.Instance.Extra.Speak == curWp.id)
        //{
        //    stand = 999999;
        //    // Add logic for speaking if needed
        //}
    }

    private void MoveToObjective()
    {
        if (!player.HasFlag())
        {
            if (Random.value < 0.3f && !Game.Instance.flags[player.PlayerTeam == Team.Blue ? Team.Red : Team.Blue].IsCaptured)
            {
                path = PathFind(FindClosestNode(Game.Instance.flags[player.PlayerTeam == Team.Blue ? Team.Red : Team.Blue].transform.position), 5);
            }
        }
        else
        {
            path = PathFind(FindClosestNode(Game.Instance.flags[player.PlayerTeam].transform.position), 5);
        }
        Debug.Log($"Move to Objective {path.Count}");
    }

    private NodeWaypoint FindClosestNode(Vector2 position)
    {
        NodeWaypoint[] nodes = FindObjectsByType<NodeWaypoint>(FindObjectsSortMode.None);
        NodeWaypoint closestNode = null;
        float closestDistance = Mathf.Infinity;

        foreach (var node in nodes)
        {
            float distance = Vector2.Distance(position, node.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = node;
            }
        }

        return closestNode;
    }

    private void GetClosestWp()
    {
        wpTimer = 0;
        path.Clear();
        NodeWaypoint[] nodes = FindObjectsByType<NodeWaypoint>(FindObjectsSortMode.None);
        NodeWaypoint closestNode = null;
        float closestDistance = Mathf.Infinity;

        foreach (var node in nodes)
        {
            float distanceY = Mathf.Abs(transform.position.y - node.transform.position.y);
            if (distanceY < 2f)
            {
                float distanceX = Mathf.Abs(transform.position.x - node.transform.position.x);
                if (distanceX < closestDistance)
                {
                    closestDistance = distanceX;
                    closestNode = node;
                }
            }
        }
        if (closestNode != null)
        {
            nextWp = closestNode;
        }
        else
        {
            nextWp = curWp.Connections[Random.Range(0, curWp.Connections.Count)];
        }
    }

    public void Wait(uint val)
    {
        stand = nostand = val;
        duck = false;
    }

    public List<NodeWaypoint> PathFind(NodeWaypoint targetWaypoint, uint maxLength)
    {
        List<NodeWaypointPath> possiblePaths = new List<NodeWaypointPath>();

        // Проходим через все соединения текущего узла
        for (int i = 0; i < curWp.Connections.Count; i++)
        {
            SearchNode(curWp.Connections[i], targetWaypoint, new List<NodeWaypoint>(), possiblePaths);
        }

        // Сортируем найденные пути по дистанции
        possiblePaths.Sort((a, b) => a.dist.CompareTo(b.dist));

        // Ограничиваем максимальной длиной пути
        if (possiblePaths.Count > maxLength)
        {
            possiblePaths = possiblePaths.Take((int)maxLength).ToList();
        }

        // Возвращаем путь, который мы нашли
        return possiblePaths.Count > 0 ? possiblePaths[0].path : new List<NodeWaypoint>();
    }

    private void SearchNode(NodeWaypoint currentNode, NodeWaypoint target, List<NodeWaypoint> currentPath, List<NodeWaypointPath> paths)
    {
        // Добавляем текущий узел в путь
        currentPath.Add(currentNode);

        // Если достигли целевого узла
        if (currentNode == target)
        {
            // Создаём новый путь и добавляем его в список возможных путей
            paths.Add(new NodeWaypointPath(new List<NodeWaypoint>(currentPath)));
            return;
        }

        // Рекурсивно ищем путь от текущего узла к целевому среди его соединений
        foreach (var connectedNode in currentNode.Connections)
        {
            // Проверяем, не был ли узел уже посещён
            if (!currentPath.Contains(connectedNode))
            {
                SearchNode(connectedNode, target, currentPath, paths);
            }
        }
    }

    private void Update()
    {
        if (getTargetTimer >= getTargetMax)
        {
            getTargetTimer = 0;
        }
        if (diff > 0)
        {
            ++getTargetTimer;
        }
        if (!playerMovement.Jumping && stand <= 0)
        {
            wpTimer += Time.deltaTime;
        }
        if (wpTimer > 4f)
        {
            GetClosestWp();
        }
        if (player.IsDead)
        {
            playerMovement.keys = 0;
            return;
        }
        if (getTargetTimer == getTargetEvent && player.CurrentWeapon != null)
        {
            List<(float dist, Player player, float rot)> playerTargets = new List<(float dist, Player player, float rot)>();
            Player[] targets = FindObjectsByType<Player>(FindObjectsSortMode.None);
            for (int i = 0; i < targets.Length; ++i)
            {
                if (targets[i] != player && !targets[i].IsDead && targets[i].PlayerTeam != player.PlayerTeam)
                {
                    float distance = Vector2.Distance(transform.position, targets[i].transform.position);
                    if (distance < Mathf.Min(player.CurrentWeapon.Range * 10, 450))
                    {
                        playerTargets.Add((distance, targets[i], GetRotation(transform.position, targets[i].transform.position)));
                    }
                }
            }
            for (int i = 0; i < playerTargets.Count; i++)
            {
                var target = playerTargets[i];

                // Определяем позицию начала луча и направление
                Vector3 startPosition = new Vector3(transform.position.x, transform.position.y, 0); // Исходная позиция
                Vector3 targetPosition = new Vector3(target.player.transform.position.x, target.player.transform.position.y, 0); // Позиция цели

                Vector3 direction = (target.player.transform.position - transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, target.dist);
                if (hit.collider != null && !hit.collider.CompareTag("Player"))
                {
                    playerTargets.RemoveAt(i);
                }
            }
            if (playerTargets.Count != 0)
            {
                playerTargets.Sort((a, b) => a.dist.CompareTo(b.dist));
                target = playerTargets[0].player;
            }
            else
            {
                target = null;
            }
        }
        if (target != null)
        {
            focusX = target.IsDead ? target.transform.position.x : target.transform.position.x;
            focusY = target.IsDead ? target.transform.position.y : target.transform.position.y;
            aimX += (focusX - aimX) * aimSpeed;
            aimY += (focusY - aimY) * aimSpeed;
            playerMovement.RotateHandTowards(Mathf.Atan2(aimY, aimX) * Mathf.Rad2Deg);
        }
        else
        {
            focusX = transform.position.x + playerMovement.xVel;
            focusY = transform.position.y + playerMovement.yVel;
            aimX += (focusX - aimX) * 0.4f;
            aimY += (focusY - aimY) * 0.3f;
            playerMovement.RotateHandTowards(Mathf.Atan2(aimY, aimX) * Mathf.Rad2Deg);
        }
        if (Game.Instance.Mode == Game.GameMode.FlagCapture)
        {
            NodeFlag flag = Game.Instance.flags[player.PlayerTeam];
            if (!flag.IsCaptured && InBox(transform.position.x, transform.position.y, flag.transform.position.x - 10f, flag.transform.position.y - 4f, 20f, 10f))
            {
                if (stand <= 0 && nostand <= 0 && !player.HasFlag())
                {
                    defendingFlag = true;
                    stand = Random.Range(2f, 5f);
                    duck = Random.value < 0.5f;
                    nostand = stand + Random.Range(0f, 2f);
                }
            }
            if (stand <= 0 && nostand <= 0)
            {
                defendingFlag = false;
            }
        }
        if (stand <= 0 && nostand <= 0 && !playerMovement.Jumping && Random.value < (player.HasFlag() ? standFlag : (target == null ? standNormal : standTarget)))
        {
            stand = Random.Range(2f, 6f) * (diffRev * 0.1f);
            duck = Random.value < 0.5f;
            nostand = stand + Random.Range(2f, 6f) * (diff * 0.1f);
        }
        if (stand > 0)
        {
            stand -= Time.deltaTime;
            if (duck)
            {
                playerMovement.Duck(true);
            }
            else
            {
                playerMovement.Duck(false);
            }
        }
        if (nostand > 0)
        {
            nostand -= Time.deltaTime;
        }
        if (diff != 0f)
        {
            if (!(nextWp.transform.position.x > transform.position.x - 0.1f && nextWp.transform.position.x < transform.position.x + 0.1f))
            {
                if (stand <= 0 && nextWp.transform.position.x > transform.position.x)
                {
                    playerMovement.keys |= (int)MovementFlags.RIGHT;
                    playerMovement.keys &= ~(int)MovementFlags.LEFT;
                }
                else if (stand <= 0 && nextWp.transform.position.x < transform.position.x)
                {
                    playerMovement.keys |= (int)MovementFlags.LEFT;
                    playerMovement.keys &= ~(int)MovementFlags.RIGHT;
                }
                else
                {
                    playerMovement.keys &= ~(int)MovementFlags.LEFT;
                    playerMovement.keys &= ~(int)MovementFlags.RIGHT;
                }
            }
            else
            {
                GetNextWaypoint();
            }
        }
        if (stand <= 0)
        {
            for (int i = 0; i < nextWp.NodeActions.Count; i++)
            {
                NodeAiAction action = nextWp.NodeActions[i];
                if (!GetComponent<Collider2D>().IsTouching(action.GetComponent<Collider2D>())) continue;
                switch (nextWp.NodeActions[i].ActionName)
                {
                    case "Jump":
                        if (!playerMovement.Jumping)
                        {
                            playerMovement.Jump();
                        }
                        break;
                    case "DoubleJump":
                        playerMovement.Jump();
                        break;
                }
            }
        }
        if (target != null && diff != 0)
        {
            shootSpd = 0.05f + (1f - (player.CurrentWeapon.FireDelay > 0.9f ? 0.9f : player.CurrentWeapon.FireDelay)) * 0.2f;
            shootSpd *= shotChance;
            if (Random.value < shootSpd && player.CurrentWeapon.CurrentAmmo > 0)
            {
                player.CurrentWeapon.UsePrimaryAttack();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green; // Выбираем нужный цвет, например, красный
        style.fontSize = 7;
        var content = new GUIContent($"wp:{wpTimer.ToString("F3")} st:{stand.ToString("F3")} nst:{nostand.ToString("F3")} p:{path.Count} cwp:{(curWp != null ? curWp.transform.GetInstanceID() : "null")} nwp:{(nextWp != null ? nextWp.transform.GetInstanceID() : "null")}");
        Handles.Label(transform.position + Vector3.left * 0.1f, content, style);

        if (path != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count; i++)
            {
                if (i < path.Count - 1)
                {
                    Gizmos.DrawLine(path[i].transform.position, path[i + 1].transform.position);
                }
                else
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawSphere(path[i].transform.position, 0.2f);
            }
        }
        if (curWp)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, curWp.transform.position);
        }
        if (nextWp)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, nextWp.transform.position);
        }
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
            Gizmos.DrawSphere(target.transform.position, 0.3f);
        }
    }
}
