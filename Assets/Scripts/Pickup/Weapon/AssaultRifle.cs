using System;
using UnityEngine;

public class AssaultRifle : BaseWeapon
{
    public override event Action OnAmmoChange;
    public override event Action OnFire;
    public override event Action OnReload;
    public override event Action OnWeaponDestroy;

    private void Awake()
    {
        Init();
    }

    public void Init()
    {
        MaxAmmo = 25;
        CurrentAmmo = MaxAmmo;
        Clip = 25;
        ClipSize = 100;
        Range = 100;
        Damage = 6;
        FireDelay = 0.1f;
        ReloadTime = 2.8f;
        WeaponType = WeaponTypeEnum.Assault;
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

    public override bool AddAmmoToClip(int ammo)
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

    public void Fire()
    {
        if (CurrentAmmo <= 0 || IsReloading())
        {
            Debug.Log("Out of ammo or reloading.");
            return;
        }
        if (Time.time - FireLastTime < FireDelay)
        {
            return;
        }
        FireLastTime = Time.time;

        WasteAmmo(1);
        Debug.Log("Assault: Single shot fired.");

        // Выполняем RaycastAll, чтобы получить все столкновения вдоль луча
        Vector2 hitPoint = AttackPoint.right * Player.GetComponent<PlayerMovement>().FacingDirection * Range;
        bool hit = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(AttackPoint.position, AttackPoint.right * Player.GetComponent<PlayerMovement>().FacingDirection, Range, GetHitLayerMask());
        foreach (var hit2D in hits)
        {
            // Проверяем, является ли объект владельцем, и игнорируем его
            var target = hit2D.collider.GetComponent<Player>();
            if (target != null && target != Player && !target.IsDead) // Игнорируем владельца
            {
                if (target.PlayerTeam == Player.PlayerTeam) continue;
                Debug.Log($"Hit: {hit2D.collider.name}");
                target.TakeDamage(Player, Damage);
                hit = true;
                hitPoint = hit2D.point;
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

    public override void StartPrimaryAttack()
    {
        Fire();
        if (CurrentAmmo <= 0)
        {
            Reload();
        }
        Debug.Log("AssaultRifle: Automatic fire in progress...");
    }

    public override void StopPrimaryAttack()
    {
        Debug.Log("AssaultRifle: Stopping automatic fire.");
    }

    public override void UseSecondaryAttack()
    {
        // Нет дополнительной функции для вторичной атаки
    }

    public override void StartSecondaryAttack()
    {
        // Secondary attack не требует удержания
    }

    public override void StopSecondaryAttack()
    {
        // Secondary attack не требует удержания
    }

    public override bool CanHoldFire()
    {
        return true; // Удержание разрешено, так как это автоматическое оружие
    }

    public override void ApplyAttachment()
    {
        Debug.Log("AssaultRifle: Attachment applied.");
    }

    public override void Reload()
    {
        if (Clip <= 0 || CurrentAmmo == MaxAmmo || IsReloading())
        {
            Debug.Log("Cannot reload. Either no reserve ammo or magazine is full.");
            return;
        }
        ReloadLastTime = Time.time;
        int ammoToSubtract = MaxAmmo - CurrentAmmo;
        FullUpAmmo();
        WasteAddAmmo(ammoToSubtract);
        OnReload?.Invoke();
    }

    private void OnDrawGizmos()
    {
        if (Player == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(AttackPoint.position, AttackPoint.position + (AttackPoint.right * Range * Player.GetComponent<PlayerMovement>().FacingDirection));
    }
}
