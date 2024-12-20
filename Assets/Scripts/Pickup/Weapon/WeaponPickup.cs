using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : BasePickup
{
    [SerializeField] private BaseWeapon _weapon; // Оружие, которое будет добавлено в инвентарь
    public override void Pickup(Player player)
    {
        if (_weapon.IsEquipped) return;
        if (player != null)
        {
            // Проверяем, не находится ли уже оружие в инвентаре игрока
            if (!player.KvpWeapons.ContainsKey(_weapon.WeaponType))
            {
                // Добавляем оружие в инвентарь
                player.PickUpWeapon(_weapon);
                transform.SetParent(player.Hand, false);
            }
            else
            {
                // Если оружие уже есть в инвентаре, можно показать сообщение или просто проигнорировать
                Debug.Log($"Оружие уже в инвентаре!");
            }
        }
    }
}
