using System;
using UnityEngine;
[RequireComponent(typeof(TriggerFlagCapture))]
public class NodeFlag : MonoBehaviour
{
    public Team team = Team.None;
    public bool IsCaptured { get; private set; } = false;
    private SpriteRenderer spriteRenderer;
    public event Action<NodeFlag, Player> OnCaptured;
    public event Action<NodeFlag, Player> OnDelivered;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void CaptureFlag(Player player)
    {
        if (IsCaptured || player.PlayerTeam == team) return;
        IsCaptured = true;
        player.GetFlag(this);
        OnCaptured?.Invoke(this, player);
        Debug.Log($"{player.name}({player.PlayerTeam}) took flag {team}");
    }
    public void DeliverFlag(Player player)
    {
        if (IsCaptured || player.PlayerTeam != team) return;
        NodeFlag deliveredFlag = player.ResetFlag(false);
        deliveredFlag.ResetCapture();
        OnDelivered?.Invoke(deliveredFlag, player);
        Debug.Log($"{player.name}({player.PlayerTeam}) delivered flag {deliveredFlag.team}");
    }
    public void ResetCapture()
    {
        IsCaptured = false;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!IsCaptured)
        {
            Gizmos.color = PlayerUT.GetColorByTeam(team);
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
        else
        {
            Gizmos.color = Color.black;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
#endif
}
