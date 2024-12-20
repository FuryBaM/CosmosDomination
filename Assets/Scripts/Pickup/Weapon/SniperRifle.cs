using System;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SniperRifle : BaseWeapon
{
    public bool IsReloading { get; private set; }
    private float nextFireTime = 0f;

    public override event Action OnAmmoChange;
    public override event Action OnFire;
    public override event Action OnReload;
    public override event Action OnWeaponDestroy;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        m_Name = "Sniper Rifle";
        MaxAmmo = 5; // Магазин на 5 патронов
        CurrentAmmo = MaxAmmo;
        Clip = 15; // Запасные патроны
        ClipSize = 30;

        Range = 200; // Большая дальность
        Damage = 100; // Высокий урон
        FireDelay = 1.5f; // Медленный темп стрельбы (1 выстрел каждые 1.5 секунды)
        WeaponType = WeaponTypeEnum.PrimaryWeapon;
    }
    public void WasteAmmo(int ammo)
    {
        ammo = Mathf.Clamp(ammo, 0, MaxAmmo);
        CurrentAmmo -= ammo;
        OnAmmoChange?.Invoke();
    }
    public void FullUpAmmo()
    {
        CurrentAmmo = MaxAmmo;
        OnAmmoChange?.Invoke();
    }
    public void WasteAddAmmo(int ammo)
    {
        ammo = Mathf.Clamp(ammo, 0, Clip);
        Clip -= ammo;
        OnAmmoChange?.Invoke();
    }
    public override bool AddAmmoToMag(int ammo)
    {
        if (Clip == ClipSize) return false;
        Clip += ammo;
        Clip = Mathf.Min(ClipSize, Clip);
        OnAmmoChange?.Invoke();
        return true;
    }
    public override void UsePrimaryAttack()
    {
        Fire();
        if (CurrentAmmo <= 0)
        {
            Reload();
        }
    }

    public override void StartPrimaryAttack()
    {
        UsePrimaryAttack(); // Нет удержания для одиночного выстрела
    }

    public override void StopPrimaryAttack()
    {
        // Не требуется
    }

    public override void UseSecondaryAttack()
    {
        Debug.Log("Sniper Rifle does not have a secondary attack.");
    }

    public override void StartSecondaryAttack()
    {
        // Не требуется
    }

    public override void StopSecondaryAttack()
    {
        // Не требуется
    }

    public override bool CanHoldFire()
    {
        return false; // Нет автоматического огня
    }

    public override void ApplyAttachment()
    {
        Debug.Log("Sniper Rifle currently does not support attachments.");
    }

    public override void Reload()
    {
        if (Clip <= 0 || CurrentAmmo == MaxAmmo)
        {
            Debug.Log("Cannot reload. Either no reserve ammo or magazine is full.");
            return;
        }

        int ammoToReload = Mathf.Min(MaxAmmo - CurrentAmmo, Clip);
        FullUpAmmo();
        WasteAddAmmo(ammoToReload);
        OnReload?.Invoke();
        Debug.Log($"Reloaded. Current Ammo: {CurrentAmmo}, Reserve Ammo: {Clip}");
    }

    private void Fire()
    {
        if (Time.time < nextFireTime || IsReloading)
        {
            return;
        }

        // Устанавливаем время для следующего выстрела
        nextFireTime = Time.time + FireDelay;

        if (CurrentAmmo <= 0 || IsReloading)
        {
            Debug.Log("Out of ammo or reloading.");
            return;
        }

        WasteAmmo(1);
        Debug.Log("Sniper rifle: Single shot fired.");
        Vector2 hitPoint = AttackPoint.right * Player.GetComponent<PlayerMovement>().FacingDirection * Range;
        bool hit = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(AttackPoint.position, AttackPoint.right * Player.GetComponent<PlayerMovement>().FacingDirection, Range, GetHitLayerMask());
        foreach (var hit2D in hits)
        {
            // Проверяем, является ли объект владельцем, и игнорируем его
            var target = hit2D.collider.GetComponent<Player>();
            if (target != null && target != Player)  // Игнорируем владельца
            {
                if (target.PlayerTeam == Player.PlayerTeam) continue;
                Debug.Log($"Hit: {hit2D.collider.name}");
                target.TakeDamage(Player, Damage);
                break;
            }
            if (hit2D.collider.gameObject.CompareTag("Ground"))
            {
                hit = true;
                hitPoint = hit2D.point;
                break;
            }
        }
        FindAnyObjectByType<CommandBufferTracer>().CreateTracer(AttackPoint.position, hitPoint, 0.1f);
        OnFire?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (Player == null) return;

        // Отрисовка диапазона стрельбы для визуального представления
        Gizmos.color = Color.red;
        Gizmos.DrawLine(AttackPoint.position, AttackPoint.position + (AttackPoint.right * Range * Player.GetComponent<PlayerMovement>().FacingDirection));
    }
}
