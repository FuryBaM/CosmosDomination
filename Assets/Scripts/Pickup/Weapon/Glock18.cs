using System;
using UnityEngine;
public class Glock18 : BaseWeapon
{
    public bool IsAutomaticMode {  get; private set; }
    public bool IsReloading { get; private set; }

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
        MaxAmmo = 20;
        CurrentAmmo = MaxAmmo;
        Clip = 60;
        ClipSize = 120;
        Range = 100;
        Damage = 9;
        FireDelay = 0.15f;
        WeaponType = WeaponTypeEnum.SecondaryWeapon;
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

    public void Fire()
    {
        if (CurrentAmmo <= 0 || IsReloading)
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
        Debug.Log("Glock18: Single shot fired.");

        // Выполняем RaycastAll, чтобы получить все столкновения вдоль луча
        Vector2 hitPoint = AttackPoint.right * Player.GetComponent<PlayerMovement>().FacingDirection * Range;
        bool hit = false;
        RaycastHit2D[] hits = Physics2D.RaycastAll(AttackPoint.position, AttackPoint.right * Player.GetComponent<PlayerMovement>().FacingDirection, Range, GetHitLayerMask());
        foreach (var hit2D in hits)
        {
            // Проверяем, является ли объект владельцем, и игнорируем его
            var target = hit2D.collider.GetComponent<Player>();
            if (target != null && target != Player && !target.IsDead)  // Игнорируем владельца
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
        if (IsAutomaticMode)
        {
            Debug.Log("Glock18 Primary Attack (Hold): Automatic fire in progress...");
        }
    }

    public override void StopPrimaryAttack()
    {
        if (IsAutomaticMode)
        {
            Debug.Log("Glock18 Primary Attack (Hold): Stopping automatic fire.");
        }
    }

    public override void UseSecondaryAttack()
    {
        // Переключение между режимами огня
        IsAutomaticMode = !IsAutomaticMode;
        Debug.Log($"Glock18 Secondary Attack: Mode switched to {(IsAutomaticMode ? "Automatic" : "Single Shot")}.");
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
        return IsAutomaticMode; // Разрешить удержание только в автоматическом режиме
    }

    public override void ApplyAttachment()
    {
        Debug.Log("Glock18: Attachment applied.");
    }
    public override void Reload()
    {
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
