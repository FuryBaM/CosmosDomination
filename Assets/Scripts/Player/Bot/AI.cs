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
    private float getTargetEvent = 4f;
    private float getTargetMax = 7f;
    private float shootSpd;
    private float standNormal;
    private float standTarget;
    private float standFlag;
    private float shotChance;
    private float aimSpeed;
    private uint difficultyReverse;
    public uint difficulty = 5;

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
        aimX = playerMovement.transform.position.x + playerMovement.FacingDirection;
        aimY = playerMovement.transform.position.y;
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
        difficultyReverse = 10 - difficulty;
        standNormal = 0.01f * (difficultyReverse * 0.3f);
        standTarget = 0.03f * (difficultyReverse * 0.3f);
        standFlag = 0.005f * (difficultyReverse * 0.3f);
        shotChance = difficulty * 0.29f + 0.1f;
        if (difficulty == 10)
        {
            shotChance = 1000;
        }
        aimSpeed = 0.3f * (difficulty * 0.1f + 0.1f);
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
            return;
        }
        if (next != null)
        {
            curWp = nextWp;
            nextWp = next;
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
        // Получаем путь с помощью метода поиска
        List<NodeWaypoint> path = pathfinding.FindPath(curWp, targetWaypoint);

        // Проверка: путь не должен быть пустым
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Pathfinding returned an empty path.");
            return new List<NodeWaypoint>();
        }

        // Проверка: ограничиваем путь длиной maxLength
        if (path.Count > maxLength)
        {
            Debug.LogWarning("Path exceeds maximum length. Trimming the path.");
            path = path.Take((int)maxLength).ToList();
        }

        return path;
    }

    private void Update()
    {
        if (getTargetTimer >= getTargetMax)
        {
            getTargetTimer = 0;
        }
        if (difficulty > 0)
        {
            getTargetTimer += Time.deltaTime;
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
        if (getTargetTimer >= getTargetEvent && player.CurrentWeapon != null)
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

            List<(float dist, Player player, float rot)> potentialTargets = new List<(float dist, Player player, float rot)>();
            for (int i = 0; i < playerTargets.Count; i++)
            {
                var target = playerTargets[i];

                // Определяем позицию начала луча и направление
                Vector3 startPosition = player.CurrentWeapon.AttackPoint.position;
                Vector3 targetPosition = target.player.transform.position;
                Vector3 direction = (targetPosition - startPosition).normalized;

                // Выполняем Raycast с учетом LayerMask
                RaycastHit2D hit = Physics2D.Raycast(startPosition, direction, target.dist, player.CurrentWeapon.GetHitLayerMask());

                // Проверяем, что луч достигает игрока и не блокируется стеной
                if (hit.collider != null && hit.collider.CompareTag("Player") && hit.collider.gameObject == target.player.gameObject)
                {
                    potentialTargets.Add(target);
                }
            }

            // Если есть потенциальные цели, выбираем ближайшую
            if (potentialTargets.Count != 0)
            {
                potentialTargets.Sort((a, b) => a.dist.CompareTo(b.dist));
                target = potentialTargets[0].player;
            }
            else
            {
                target = null;
            }
        }
        if (target != null)
        {
            focusX = target.transform.position.x;
            focusY = target.transform.position.y;
            aimX += (focusX - aimX) * aimSpeed;
            aimY += (focusY - aimY) * aimSpeed;
            Vector2 aimPosition = new Vector2(aimX, aimY);
            Vector2 direction = aimPosition - (Vector2)player.CurrentWeapon.AttackPoint.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            playerMovement.RotateHandTowards(angle);
        }
        else
        {
            if (playerMovement.xVel == 0 && playerMovement.yVel == 0)
            {
                focusX = playerMovement.transform.position.x + playerMovement.FacingDirection;
                focusY = playerMovement.transform.position.y;
            }
            else
            {
                focusX = playerMovement.transform.position.x + playerMovement.xVel;
                focusY = playerMovement.transform.position.y + playerMovement.yVel;
            }
            aimX += (focusX - aimX) * 0.4f;
            aimY += (focusY - aimY) * 0.3f;
            Vector2 aimPosition = new Vector2(aimX, aimY);
            Vector2 direction = aimPosition - (Vector2)player.CurrentWeapon.AttackPoint.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            playerMovement.RotateHandTowards(angle);
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
            stand = Random.Range(2f, 6f) * (difficultyReverse * 0.1f);
            duck = Random.value < 0.5f;
            nostand = stand + Random.Range(2f, 6f) * (difficulty * 0.1f);
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
        if (difficulty != 0f)
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
        if (target != null && difficulty != 0)
        {
            shootSpd = 0.05f + (1f - (player.CurrentWeapon.FireDelay > 0.9f ? 0.9f : player.CurrentWeapon.FireDelay)) * 0.2f;
            shootSpd *= shotChance;
            if (Random.value < shootSpd && player.CurrentWeapon.CurrentAmmo > 0)
            {
                player.CurrentWeapon.UsePrimaryAttack();
            }
            else if (player.CurrentWeapon.Clip <= 0)
            {
                SwitchWeapon();
            }
        }
    }

    private void SwitchWeapon()
    {
        foreach (var kvpWeapon in player.KvpWeapons)
        {
            if (kvpWeapon.Value.CurrentAmmo <= 0 && kvpWeapon.Value.Clip <= 0)
            {
                continue;
            }
            player.SelectWeapon(kvpWeapon.Value);
        }
    }
#if UNITY_EDITOR
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
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(aimX, aimY, 0f), 0.3f);
        }
    }
#endif
}
