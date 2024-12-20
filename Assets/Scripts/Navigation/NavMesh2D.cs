using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
public class NavMesh2D : MonoBehaviour
{
    [SerializeField] private NodeSpawn nodeSpawnPrefab;
    [SerializeField] private NodeFlag nodeFlagPrefab;
    [SerializeField] private NodePickup nodePickupPrefab;
    [SerializeField] private NodeWaypoint nodeWaypointPrefab;
    [SerializeField] private NodeHoldpoint nodeHoldpointPrefab;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float step = 0.1f;
    [SerializeField] private float maxSlopeAngle = 45f;

    // ����������� ���������
    [SerializeField] private float maxJumpHeight = 2f;
    [SerializeField] private float maxFallHeight = 4f;
    [SerializeField] private float maxHorizontalReach = 2f;

    private List<NodeWaypoint> nodes = new List<NodeWaypoint>();
    private List<NodeSpawn> spawns = new List<NodeSpawn>();
    private List<NodeFlag> flags = new List<NodeFlag>();
    private List<NodePickup> pickups = new List<NodePickup>();
    private List<NodeHoldpoint> holdpoints = new List<NodeHoldpoint>();

    private void Start()
    {
        nodes.Clear();
        spawns.Clear();
        flags.Clear();
        pickups.Clear();

        // ��������� ������� ��������� ����
        NodeWaypoint[] eNodes = FindObjectsByType<NodeWaypoint>(FindObjectsSortMode.InstanceID);
        nodes.AddRange(eNodes);
        NodeSpawn[] eSpawns = FindObjectsByType<NodeSpawn>(FindObjectsSortMode.InstanceID);
        spawns.AddRange(eSpawns);
        NodeFlag[] eFlags = FindObjectsByType<NodeFlag>(FindObjectsSortMode.InstanceID);
        flags.AddRange(eFlags);
        NodePickup[] ePickups = FindObjectsByType<NodePickup>(FindObjectsSortMode.InstanceID);
        pickups.AddRange(ePickups);
        NodeHoldpoint[] eHoldpoints = FindObjectsByType<NodeHoldpoint>(FindObjectsSortMode.InstanceID);
        pickups.AddRange(eHoldpoints);
    }

    public List<NodeWaypoint> GetNodes()
    {
        return nodes;
    }

    void AddNode(Vector2 point, GameObject ground, List<NodeAiAction> actions = null)
    {
        // ��������� �� ���������� � ������������� ������
        if (IsNodeTooClose(point))
            return;

        GameObject nodeObj = new GameObject("Node");
        nodeObj.transform.position = point;
        NodeWaypoint node = nodeObj.AddComponent<NodeWaypoint>();
        node.Ground = ground;
        nodeObj.transform.SetParent(transform);

        // ��������� ��������
        if (actions != null)
        {
            foreach (var action in actions)
            {
                node.NodeActions.Add(action);  // ��������� ����������� ������� NodeAiAction
            }
        }

        nodes.Add(node);
    }

    bool IsNodeTooClose(Vector2 point)
    {
        float minDistance = 0.1f;

        foreach (var node in nodes)
        {
            if (Vector2.Distance(point, node.transform.position) < minDistance)
            {
                return true;
            }
        }
        return false;
    }

    // ���������� ������
    public void SaveNodes()
    {
        List<NodeWaypoint> nodeList = new List<NodeWaypoint>(nodes);
        List<NodeWaypointData> nodeDataList = new List<NodeWaypointData>();

        for (int i = 0; i < nodeList.Count; i++)
        {
            NodeWaypoint node = nodeList[i];
            NodeWaypointData data = new NodeWaypointData
            {
                position = node.transform.position,
                connectionIndices = new List<int>(),
                actionSet = new List<NodeWaypointActionData>() // ���������� ������ ��� ������������
            };

            // ����������� �������� �� ����������� ������������� � NodeActionData
            foreach (var action in node.NodeActions)
            {
                NodeWaypointActionData actionData = new NodeWaypointActionData
                {
                    action = action.ActionName // ����������� � ������ ��� ����������
                };
                data.actionSet.Add(actionData);
            }

            foreach (var connectedNode in node.Connections)
            {
                int index = nodeList.IndexOf(connectedNode);
                if (index >= 0)
                {
                    data.connectionIndices.Add(index);
                }
            }
            nodeDataList.Add(data);
        }

        List<NodeSpawn> spawnList = new List<NodeSpawn>(spawns);
        List<NodeSpawnData> spawnDataList = new List<NodeSpawnData>();

        for (int i = 0; i < spawnList.Count; i++)
        {
            var spawnData = new NodeSpawnData();
            spawnData.position = spawnList[i].transform.position;
            spawnData.team = (int)spawnList[i].teamSpawn;
            spawnData.connectionIndex = nodeList.IndexOf(spawnList[i].waypoint);
            spawnDataList.Add(spawnData);
        }

        List<NodePickup> pickupList = new List<NodePickup>(pickups);
        List<NodePickupData> pickupDataList = new List<NodePickupData>();

        for (int i = 0; i < pickupList.Count; i++)
        {
            var pickupData = new NodePickupData();
            pickupData.position = pickupList[i].transform.position;
            pickupData.pickupType = (int)pickupList[i].pickupType;

            pickupDataList.Add(pickupData);
        }

        List<NodeFlag> flagList = new List<NodeFlag>(flags);
        List<NodeFlagData> flagDataList = new List<NodeFlagData>();

        for (int i = 0; i < flagList.Count; i++)
        {
            var flagData = new NodeFlagData();
            flagData.position = flagList[i].transform.position;
            flagData.team = (int)flagList[i].team;

            flagDataList.Add(flagData);
        }

        List<NodeHoldpoint> holdpointList = new List<NodeHoldpoint>(holdpoints);
        List<NodeHoldpointData> holdpointDataList = new List<NodeHoldpointData>();

        for (int i = 0; i < holdpointList.Count; i++)
        {
            var holdpointData = new NodeHoldpointData();
            holdpointData.position = holdpointList[i].transform.position;

            holdpointDataList.Add(holdpointData);
        }

        // ����������� nodeDataList � JSON
        string json = JsonUtility.ToJson(new NodeDataListWrapper { 
            waypoints = nodeDataList, 
            spawns = spawnDataList, 
            pickups = pickupDataList, 
            flags = flagDataList 
        }, true);
        System.IO.File.WriteAllText(Application.persistentDataPath + $"/nodes_{SceneManager.GetActiveScene().name}.json", json);
    }

    // �������� ������
    public void LoadNodes()
    {
        string filePath = Application.persistentDataPath + $"/nodes_{SceneManager.GetActiveScene().name}.json";
        if (!System.IO.File.Exists(filePath))
        {
            Debug.LogError("���� � ������� ����� �� ������ �� ���� " + filePath);
            return;
        }

        string json = System.IO.File.ReadAllText(filePath);
        NodeDataListWrapper wrapper = JsonUtility.FromJson<NodeDataListWrapper>(json);

        // ������� ������������ ����
        foreach (var node in nodes)
        {
            if (node != null)
            {
                Destroy(node.gameObject);
            }
        }
        nodes.Clear();

        List<NodeWaypoint> nodeList = new List<NodeWaypoint>();

        // ������� ����
        foreach (var data in wrapper.waypoints)
        {
            GameObject nodeObj = new GameObject("Node");
            nodeObj.transform.parent = transform;
            nodeObj.transform.position = data.position;
            NodeWaypoint node = nodeObj.AddComponent<NodeWaypoint>();

            // ��������������� �������� �� ��������������� ������
            foreach (var actionData in data.actionSet)
            {
                // ����������� NodeActionData ������� � ��� �������� (��������, ��� Action)
                NodeAiAction action = new NodeAiAction();
                action.SetAction(Enum.Parse<NodeAiAction.ActionType>(actionData.action)); // ��������������� ��������
                node.NodeActions.Add(action);
            }

            nodeList.Add(node);
        }

        // ������������� ����������
        for (int i = 0; i < nodeList.Count; i++)
        {
            NodeWaypoint node = nodeList[i];
            NodeWaypointData data = wrapper.waypoints[i];

            foreach (var index in data.connectionIndices)
            {
                if (index >= 0 && index < nodeList.Count)
                {
                    NodeWaypoint connectedNode = nodeList[index];
                    node.Connections.Add(connectedNode);
                }
                else
                {
                    Debug.LogWarning("���������������� ������ ���������� " + index);
                }
            }
        }
        nodes.AddRange(nodeList);
    }
}

[System.Serializable]
public class NodeWaypointData
{
    public Vector2 position;
    public List<int> connectionIndices;
    public List<NodeWaypointActionData> actionSet; // ��� ���������� ������ ��������
}
[System.Serializable]
public class NodeSpawnData
{
    public Vector2 position;
    public int connectionIndex;
    public int team;
}
[System.Serializable]
public class NodePickupData
{
    public Vector2 position;
    public int pickupType;
}
[System.Serializable]
public class NodeFlagData
{
    public Vector2 position;
    public int team;
}
[System.Serializable]
public class NodeHoldpointData
{
    public Vector2 position;
}
[System.Serializable]
public class NodeWaypointActionData
{
    public string action = "Jump"; // ��������� ��� ������
}

[System.Serializable]
public class NodeDataListWrapper
{
    public List<NodeWaypointData> waypoints;
    public List<NodeSpawnData> spawns;
    public List<NodeFlagData> flags;
    public List<NodePickupData> pickups;
    public List<NodeHoldpointData> holdpoints;
}
