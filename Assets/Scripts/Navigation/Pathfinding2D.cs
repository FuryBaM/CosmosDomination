using System.Collections.Generic;
using UnityEngine;

public class Pathfinding2D : MonoBehaviour
{
    public List<NodeWaypoint> FindPath(NodeWaypoint startNode, NodeWaypoint endNode)
    {
        var openList = new List<NodeWaypoint>();
        var closedList = new HashSet<NodeWaypoint>();

        Dictionary<NodeWaypoint, NodeWaypoint> cameFrom = new Dictionary<NodeWaypoint, NodeWaypoint>();
        Dictionary<NodeWaypoint, float> gScore = new Dictionary<NodeWaypoint, float>();
        Dictionary<NodeWaypoint, float> fScore = new Dictionary<NodeWaypoint, float>();

        foreach (var node in FindObjectOfType<NavMesh2D>().GetNodes())
        {
            gScore[node] = float.MaxValue;
            fScore[node] = float.MaxValue;
        }

        gScore[startNode] = 0;
        fScore[startNode] = Vector2.Distance(startNode.transform.position, endNode.transform.position);

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            NodeWaypoint current = GetNodeWithLowestFScore(openList, fScore);

            if (current == endNode)
                return ReconstructPath(cameFrom, current);

            openList.Remove(current);
            closedList.Add(current);

            foreach (NodeWaypoint neighbor in current.Connections)
            {
                if (closedList.Contains(neighbor))
                    continue;

                float tentativeGScore = gScore[current] + Vector2.Distance(current.transform.position, neighbor.transform.position);

                if (!openList.Contains(neighbor))
                    openList.Add(neighbor);

                if (tentativeGScore >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Vector2.Distance(neighbor.transform.position, endNode.transform.position);
            }
        }

        return null; // ѕуть не найден
    }

    private NodeWaypoint GetNodeWithLowestFScore(List<NodeWaypoint> nodes, Dictionary<NodeWaypoint, float> fScore)
    {
        NodeWaypoint lowestNode = nodes[0];
        float lowestScore = fScore[lowestNode];

        foreach (NodeWaypoint node in nodes)
        {
            if (fScore[node] < lowestScore)
            {
                lowestNode = node;
                lowestScore = fScore[node];
            }
        }

        return lowestNode;
    }

    private List<NodeWaypoint> ReconstructPath(Dictionary<NodeWaypoint, NodeWaypoint> cameFrom, NodeWaypoint current)
    {
        var path = new List<NodeWaypoint> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
