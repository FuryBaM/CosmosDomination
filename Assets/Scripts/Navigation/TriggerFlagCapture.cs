using UnityEngine;
[RequireComponent (typeof(BoxCollider2D))]
public class TriggerFlagCapture : MonoBehaviour
{
    private NodeFlag flagNode;
    private bool isPlayerInTrigger = false;
    private BoxCollider2D m_boxCollider2D;
    private void Awake()
    {
        flagNode = GetComponentInParent<NodeFlag>();
        m_boxCollider2D = GetComponent<BoxCollider2D>();
        m_boxCollider2D.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            if (!player.HasFlag())
            {
                flagNode.CaptureFlag(player); // Игрок берет флаг
            }
            else if (player.HasFlag() && flagNode.team == player.PlayerTeam)
            {
                flagNode.DeliverFlag(player); // Игрок доставляет флаг
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInTrigger = false;
        }
    }
}
