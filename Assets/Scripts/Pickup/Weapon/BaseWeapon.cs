#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using static Player;
public enum WeaponTypeEnum
{
    Pistol,
    Plasma,
    Shotgun,
    Assault,
    Sniper,
    Heavy,
    Rocket
}
public abstract class BaseWeapon : MonoBehaviour
{
    // Поля класса
    protected string m_Name;
    public Player Player { get; protected set; }

    // Свойства оружия
    public int Damage { get; protected set; }
    public float FireDelay { get; protected set; }
    public float FireLastTime { get; protected set; }
    public float ReloadTime { get; protected set; } = 0;
    public float ReloadLastTime { get; protected set; }

    // Параметры патронов
    public int MaxAmmo { get; protected set; } = 30;  // Вместимость магазина
    public int CurrentAmmo { get; protected set; } = 0; // Текущее кол-во патронов в магазине
    public int Clip { get; protected set; } = 30; // Сумма патронов в доп магазинах
    public int ClipSize { get; protected set; } = 90; // Максимальное число патронов в доп магазинах
    public int Range { get; protected set; } = 0;

    // Направление атаки
    [SerializeField] private Transform m_attackPoint;
    public Transform AttackPoint
    {
        get { return m_attackPoint; }
        protected set { m_attackPoint = value; }
    }

    // Статус оружия
    public bool IsEquipped { get; protected set; } = false;
    public WeaponTypeEnum WeaponType { get; protected set; }

    // Маска для определения попаданий
    [SerializeField] protected LayerMask hitMask;

    // События
    public abstract event Action OnAmmoChange;
    public abstract event Action OnFire;
    public abstract event Action OnReload;
    public virtual event Action OnWeaponDestroy;

    public void SetOwner(Player player)
    {
        Player = player;
    }
    // Primary attack
    public abstract void UsePrimaryAttack();
    public abstract void StartPrimaryAttack();

    public abstract void StopPrimaryAttack();

    // Secondary attack
    public abstract void UseSecondaryAttack();
    public abstract void StartSecondaryAttack();
    public abstract void StopSecondaryAttack();

    public abstract bool CanHoldFire();

    public abstract void ApplyAttachment();

    public virtual void EquipWeapon(Player player)
    {
        IsEquipped = true;
        transform.SetParent(player.Hand, false);

        transform.localPosition = new Vector3(0, 0, 0);
        transform.localRotation = Quaternion.identity;
        Player = player;
    }

    public virtual void UnEquipWeapon(Player player)
    {
        IsEquipped = false;
        transform.SetParent(null);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForce(player.transform.forward * 10f, ForceMode.Impulse);
        }
        Player = null;
    }
    public abstract bool AddAmmoToClip(int ammo);
    public abstract void Reload();
    public virtual bool IsReloading()
    {
        return Time.time - ReloadLastTime < ReloadTime;
    }
    public LayerMask GetHitLayerMask() => hitMask;
    private void OnDestroy()
    {
        OnWeaponDestroy?.Invoke();
    }
#if UNITY_EDITOR
    public virtual void OnDrawGizmos()
    {
        GUIContent content = new GUIContent();
        content.text = $"{CurrentAmmo}/{Clip}";
        Handles.Label(transform.position + Vector3.left, content);
    }
#endif
}
