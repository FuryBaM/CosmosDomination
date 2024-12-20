using UnityEngine;
public class Medkit : BaseBoost
{
    public int healAmount; // ���������� ������������������ ��������
    public override void ApplyBoost(Player player)
    {
        Debug.Log($"Applying {boostName} to {player.Name}");
        player.Heal(null, healAmount);
        Destroy(this);
    }
}
