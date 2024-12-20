using UnityEngine;

public class BoostPickup : BasePickup
{
    [SerializeField] private BaseBoost _boost; // ������ �� ���������� ���� (��������, ArmorBoost, HealthBoost)

    public override void Pickup(Player player)
    {
        if (player != null && _boost != null)
        {
            // ��������� ���� � ������
            _boost.ApplyBoost(player);
        }
    }
}
