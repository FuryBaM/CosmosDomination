using System.Collections.Generic;
using UnityEngine;

public class NodeWaypointPath
{
    public float dist;

    public uint nodes;

    public List<NodeWaypoint> path;

    public NodeWaypointPath(List<NodeWaypoint> waypoints)
    {
        path = waypoints;
        nodes = (uint)path.Count;
        dist = 0;
        for (int i = 0; i < nodes - 1; i++)
        {
            dist += Vector2.Distance(path[i].transform.position, path[i + 1].transform.position);
        }
    }
}