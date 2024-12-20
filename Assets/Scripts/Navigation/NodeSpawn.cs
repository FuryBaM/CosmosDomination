using UnityEngine;

public class NodeSpawn : MonoBehaviour
{
    public Team teamSpawn;  // Название команды (например, "Red" или "Blue")
    public NodeWaypoint waypoint;

    // Визуализация спавн-узла для каждой команды
    protected void OnDrawGizmos()
    {
        switch (teamSpawn)
        { 
            case Team.None:
                Gizmos.color = Color.gray; break;
            case Team.Blue:
                Gizmos.color = Color.blue; break;
            case Team.Red:
                Gizmos.color = Color.red; break;
            case Team.Green:
                Gizmos.color = Color.green; break;
            case Team.Yellow:
                Gizmos.color = Color.yellow; break;
        }
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}
