using UnityEngine;
public class Armor : BaseBoost
{
    public int armorAmount; //  оличество восстанавливаемого здоровь€
    public override void ApplyBoost(Player player)
    {
        Debug.Log($"Applying {boostName} to {player.Name}");
        player.AddArmor(null, armorAmount);
        Destroy(this);
    }
}
