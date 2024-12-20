using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class BotBrain : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform handTransform;

    [Header("AI Settings")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private LayerMask targetLayer;
    [SerializeField] private float aimSpeed = 0.8f;
    [SerializeField] private float shotChance = 0.5f;
    [SerializeField] private float standNormal = 0.01f;
    [SerializeField] private float standTarget = 0.03f;
    [SerializeField] private float standFlag = 0.005f;

    private Transform target;
    private float targetAcquisitionTimer;
    private float targetAcquisitionInterval = 0.5f;
    private float standTimer;
    private float noStandTimer;
    private bool isDucking;
    private Vector2 aimPosition;
    private Pathfinding2D pathfinding;
    private NodeWaypoint nextWp;
    private NodeWaypoint curWp;
    private List<NodeWaypoint> path;
    private int currentNodeIndex;
    private float wpTimer = 0;
    private uint diffRev;
    public uint diff = 5; // difficulty

    private bool isDefendingFlag;

    private void Start()
    {
        if (player == null) player = GetComponent<Player>();
        if (playerMovement == null) playerMovement = GetComponent<PlayerMovement>();
        if (handTransform == null) handTransform = player.Hand;
        player.OnDead += HandleDeath;

        pathfinding = FindAnyObjectByType<Pathfinding2D>();
        aimPosition = transform.position + Vector3.right * 5f;
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


    private void HandleDeath(Player player, Player killer)
    {
        path.Clear();
        currentNodeIndex = 0;
    }

    private void Update()
    {
        if (player.IsDead)
        {
            playerMovement.keys = 0;
            return;
        }

        UpdateTimers();
        HandleTargetAcquisition();
        HandleAiming();
        HandleShooting();
        HandleMovement();
    }

    private void UpdateTimers()
    {
        if (!playerMovement.Jumping && standTimer <= 0f)
        {
            wpTimer += Time.deltaTime;
        }
        if (wpTimer >= 4f)
        {
            GetClosestWp();
        }
        if (!Game.Instance.noStand)
        {
            if (Game.Instance.Mode == Game.GameMode.FlagCapture)
            {
                if (!Game.Instance.flags[player.PlayerTeam].IsCaptured)
                {
                    if (standTimer <= 0f && noStandTimer <= 0f && !player.HasFlag())
                    {
                        isDefendingFlag = true;
                        standTimer = Random.Range(2f, 5f);
                        isDucking = Random.value < 0.5f;
                        noStandTimer = standTimer + Random.Range(0f, 2f);
                    }
                }
                if (standTimer <= 0f && noStandTimer <= 0f)
                {
                    isDefendingFlag = false;
                }
            }
            float chance = player.HasFlag() ? standFlag : (target != null ? standTarget : standNormal);
            if (standTimer <= 0f && noStandTimer <= 0f && !playerMovement.Jumping && Random.value < chance)
            {
                standTimer = Random.Range(2f, 6f) * (diffRev * 0.1f);
                noStandTimer = standTimer + Random.Range(2f, 6f) * (diffRev * 0.1f);
                isDucking = Random.value < 0.5f;
            }
        }
        if (standTimer > 0f)
        {
            standTimer -= Time.deltaTime;
            if (isDucking)
            {
                playerMovement.Duck(true);
            }
        }
        if (noStandTimer > 0f)
        {
            noStandTimer -= Time.deltaTime;
        }
    }

    private void HandleTargetAcquisition()
    {
        targetAcquisitionTimer += Time.deltaTime;
        if (targetAcquisitionTimer >= targetAcquisitionInterval)
        {
            targetAcquisitionTimer = 0f;
            AcquireTarget();
        }
    }

    private void AcquireTarget()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange, targetLayer);
        float closestDistance = Mathf.Infinity;
        Transform closestTarget = null;

        foreach (var hit in hits)
        {
            Player potentialTarget = hit.GetComponent<Player>();
            if (potentialTarget != null && !potentialTarget.IsDead && potentialTarget != player)
            {
                if (player.PlayerTeam == potentialTarget.PlayerTeam) continue;

                float distance = Vector2.Distance(transform.position, potentialTarget.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = potentialTarget.transform;
                }
            }
        }

        target = closestTarget;
    }

    private void HandleAiming()
    {
        if (target != null)
        {
            // Цель — позиция с небольшим случайным разбросом для реалистичности
            Vector2 targetPosition = target.position + new Vector3(
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.2f, 0.2f)
            );

            // Рассчитываем разницу между текущим направлением и желаемым
            Vector2 desiredDirection = targetPosition - aimPosition;
            float distanceToTarget = desiredDirection.magnitude;

            // Реалистичная скорость наведения, зависящая от расстояния
            float speedModifier = Mathf.Clamp01(distanceToTarget / 5f);
            aimPosition += desiredDirection.normalized * aimSpeed * speedModifier * Time.deltaTime;
        }
        else
        {
            // Если нет цели, просто смотрим вперед с небольшими случайными отклонениями
            Vector2 aheadPosition = (Vector2)transform.position + Vector2.right * playerMovement.FacingDirection * 5f;
            aheadPosition += new Vector2(
                Random.Range(-0.1f, 0.1f),
                Random.Range(-0.1f, 0.1f)
            );

            Vector2 desiredDirection = aheadPosition - aimPosition;
            aimPosition += desiredDirection.normalized * aimSpeed * Time.deltaTime;
        }

        // Расчёт угла и вращение руки
        Vector2 direction = aimPosition - (Vector2)handTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Добавляем небольшую дрожь, чтобы сделать прицеливание "живым"
        angle += Random.Range(-1f, 1f);

        playerMovement.RotateHandTowards(angle);
    }
    private void HandleShooting()
    {
        if (target == null) return;
        if (player.CurrentWeapon == null) return;
        if (player.CurrentWeapon.CurrentAmmo <= 0) player.CurrentWeapon.Reload();
        float shootProbability = shotChance * Time.deltaTime;
        if (Random.value < shootProbability)
        {
            player.CurrentWeapon?.UsePrimaryAttack();
        }
    }

    private void HandleMovement()
    {
        if (diff == 0) return;
        if (standTimer > 0f)
        {
            if (isDucking)
            {
                playerMovement.Duck(true);
            }
            return;
        }
        else if (isDucking)
        {
            playerMovement.Duck(false);
            isDucking = false;
        }

        if (path != null && currentNodeIndex < path.Count)
        {
            MoveTowardsWaypoint();
        }
        else
        {
            return;
        }
    }
    //private void FindNewPath()
    //{
    //    wpTimer = 0;
    //    Vector2 destination;

    //    if (player.HasFlag())
    //    {
    //        destination = GetFlagHomePosition();
    //    }
    //    else if (ShouldDefendFlag())
    //    {
    //        destination = GetFlagPosition(player.PlayerTeam);
    //    }
    //    else
    //    {
    //        destination = GetRandomObjective();
    //    }

    //    NodeWaypoint startNode = FindClosestNode(transform.position);
    //    NodeWaypoint endNode = FindClosestNode(destination);

    //    if (startNode != null && endNode != null)
    //    {
    //        path = pathfinding.FindPath(startNode, endNode);
    //        currentNodeIndex = 0;
    //    }
    //}

    private void MoveTowardsWaypoint()
    {
        if (!(nextWp.transform.position.x > player.transform.position.x - 0.1f && nextWp.transform.position.x < player.transform.position.x + 0.1f))
        {
            if (standTimer <= 0 && nextWp.transform.position.x > player.transform.position.x)
            {
                playerMovement.keys |= (int)MovementFlags.LEFT;
                playerMovement.keys &= ~(int)MovementFlags.RIGHT;
            }
            else if (standTimer <= 0 && nextWp.transform.position.x < player.transform.position.x)
            {
                playerMovement.keys |= (int)MovementFlags.RIGHT;
                playerMovement.keys &= ~(int)MovementFlags.LEFT;
            }
        }
        else
        {
            GetNextWaypoint();
        }
        //NodeWaypoint waypoint = path[currentNodeIndex];
        //Vector2 direction = waypoint.transform.position - transform.position;
        //float horizontal = direction.x > 0 ? 1 : -1;
        //float distanceX = Mathf.Abs(waypoint.transform.position.x - transform.position.x);
        //float distanceY = Mathf.Abs(waypoint.transform.position.y - transform.position.y);
        //HandleWaypointActions(waypoint);
        //playerMovement.Move(horizontal);
        //if ((distanceX < 0.1f))
        //{
        //    currentNodeIndex++;
        //}
    }

    private void HandleWaypointActions(NodeWaypoint waypoint)
    {
        if (standTimer <= 0f) return;
        foreach (var action in waypoint.NodeActions)
        {
            if (!GetComponent<Collider2D>().IsTouching(action.GetComponent<Collider2D>())) continue;
            if (action.ActionName == "Jump")
            {
                if (!playerMovement.Jumping)
                    playerMovement.Jump();
            }
            else if (action.ActionName == "DoubleJump")
            {
                playerMovement.Jump();
            }
        }
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
    private Vector2 GetFlagHomePosition()
    {
        Vector2 flagPosition = Vector2.zero;
        NodeFlag capturedFlag = Game.Instance.GetCapturedFlag(player.PlayerTeam);
        if (capturedFlag != null)
        {
            flagPosition = capturedFlag.transform.position;
        }
        return flagPosition;
    }

    private Vector2 GetFlagPosition(Team team)
    {
        Vector2 flagPosition = Vector2.zero;
        if (Game.Instance.flags[team] != null)
        {
            flagPosition = Game.Instance.flags[team].transform.position;
        }
        return flagPosition;
    }

    private bool ShouldDefendFlag()
    {
        return false;
    }

    private Vector2 GetRandomObjective()
    {
        return transform.position + new Vector3(Random.Range(-15f, 15f), 0, 0);
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
            if (distanceY < 50f)
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
    private void GetNextWaypoint(NodeWaypoint next = null, bool shouldJump = false)
    {
        wpTimer = 0;
        if (!shouldJump && playerMovement.Jumping && Mathf.Abs(transform.position.y - nextWp.transform.position.y) > 30f)
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
            if (Game.Instance.Mode == Game.GameMode.FlagCapture && (path == null || path.Count == 0))
            {
                MoveToObjective();
            }
            if (Mathf.Abs(transform.position.y - nextWp.transform.position.y) <= 50f)
            {
                if (Mathf.Abs(transform.position.y - nextWp.transform.position.y) < 50f)
                {
                    if (path == null || path.Count == 0)
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
            if (Random.Range(0f, 1f) < 0.3 && !Game.Instance.flags[player.PlayerTeam == Team.Blue ? Team.Red : Team.Blue].IsCaptured)
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
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (path != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < path.Count; i++)
            {
                if (i < path.Count - 1)
                {
                    Gizmos.DrawLine(path[i].transform.position, path[i + 1].transform.position);
                }
                if (i == currentNodeIndex)
                {
                    Gizmos.color = Color.cyan;
                }
                else
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawSphere(path[i].transform.position, 0.2f);
            }
        }
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawSphere(target.position, 0.3f);
        }
        if (nextWp != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, nextWp.transform.position);
        }

        Gizmos.color = Color.white;
        string intent = standTimer > 0f ? "Standing" : "Moving";
        intent = isDucking ? "Ducking" : intent;
        if (target != null)
        {
            intent += $", Target: {target.name}";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, intent);
        string timerString = standTimer.ToString() + " " + noStandTimer.ToString();
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.0f, timerString);
    }
#endif
}