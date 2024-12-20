using UnityEngine;

public abstract class BasePickup : MonoBehaviour
{
    public Sprite pickupSprite;
    public abstract void Pickup(Player player);
}
