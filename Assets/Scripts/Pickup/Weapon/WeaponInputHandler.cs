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
        if (Input.GetKeyDown(KeyCode.Alpha1) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.PrimaryWeapon))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.PrimaryWeapon]);

        if (Input.GetKeyDown(KeyCode.Alpha2) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.SecondaryWeapon))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.SecondaryWeapon]);

        if (Input.GetKeyDown(KeyCode.Alpha3) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.MeleeWeapon))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.MeleeWeapon]);

        if (Input.GetKeyDown(KeyCode.Alpha4) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.ThrowingWeapon))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.ThrowingWeapon]);

        if (Input.GetKeyDown(KeyCode.Alpha5) && m_player.KvpWeapons.ContainsKey(WeaponTypeEnum.PlantingWeapon))
            m_player.SelectWeapon(m_player.KvpWeapons[WeaponTypeEnum.PlantingWeapon]);
    }

    private void UseWeapon()
    {
        if (m_player.CurrentWeapon == null)
            return;

        // Primary Attack (ЛКМ)
        if (Input.GetButtonDown("Fire1"))
            m_player.CurrentWeapon.UsePrimaryAttack();

        if (Input.GetButton("Fire1"))
            m_player.CurrentWeapon.StartPrimaryAttack();

        if (Input.GetButtonUp("Fire1"))
            m_player.CurrentWeapon.StopPrimaryAttack();

        // Secondary Attack (ПКМ)
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
            Debug.Log("Reloading weapon...");
            m_player.CurrentWeapon.Reload();
            // Здесь вы можете добавить метод для перезарядки оружия, если он реализован
            // m_player.CurrentWeapon.Reload();
        }
    }
}
