using UnityEngine;

public class BoostPickup : BasePickup
{
    [SerializeField] private BaseBoost _boost; // Ссылка на конкретный буст (например, ArmorBoost, HealthBoost)

    public override void Pickup(Player player)
    {
        if (player != null && _boost != null)
        {
            // Применяем буст к игроку
            _boost.ApplyBoost(player);
        }
    }
}
