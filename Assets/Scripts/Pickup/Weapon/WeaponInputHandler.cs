using UnityEngine;

public class WeaponInputHandler : MonoBehaviour
{
    private Player m_player;

    private void Start()
    {
        m_player = GetComponent<Player>();
    }

    private void Update()
    {
        if (m_player.IsDead) return;
        HandleWeaponSelectionInput();
        UseWeapon();
    }

    private void HandleWeaponSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Pistol))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Pistol]);

        if (Input.GetKeyDown(KeyCode.Alpha2) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Plasma))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Plasma]);

        if (Input.GetKeyDown(KeyCode.Alpha3) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Shotgun))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Shotgun]);

        if (Input.GetKeyDown(KeyCode.Alpha4) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Assault))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Assault]);

        if (Input.GetKeyDown(KeyCode.Alpha5) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Sniper))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Sniper]);

        if (Input.GetKeyDown(KeyCode.Alpha6) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Heavy))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Heavy]);

        if (Input.GetKeyDown(KeyCode.Alpha7) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.Rocket))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.Rocket]);
    }

    private void UseWeapon()
    {
        if (m_player.CurrentWeapon == null)
            return;

        // Primary Attack (À Ã)
        if (Input.GetButtonDown("Fire1"))
            m_player.CurrentWeapon.UsePrimaryAttack();

        if (Input.GetButton("Fire1"))
            m_player.CurrentWeapon.StartPrimaryAttack();

        if (Input.GetButtonUp("Fire1"))
            m_player.CurrentWeapon.StopPrimaryAttack();

        // Secondary Attack (œ Ã)
        if (Input.GetButtonDown("Fire2"))
            m_player.CurrentWeapon.UseSecondaryAttack();

        if (Input.GetButton("Fire2"))
            m_player.CurrentWeapon.StartSecondaryAttack();

        if (Input.GetButtonUp("Fire2"))
            m_player.CurrentWeapon.StopSecondaryAttack();

        // Reload (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ReloadWeapon();
        }
    }

    private void ReloadWeapon()
    {
        if (m_player.CurrentWeapon != null)
        {
            m_player.CurrentWeapon.Reload();
        }
    }
}
