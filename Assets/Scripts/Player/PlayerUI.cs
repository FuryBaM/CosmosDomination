using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    private Player m_player;
    [SerializeField] private PlayerUILinker m_canvasPrefab;
    private TMP_Text m_AmmoText;
    private TMP_Text m_HealthText;
    private TMP_Text m_ArmorText;
    private PlayerUILinker playerUILinker;
    private void Start()
    {
        playerUILinker = Instantiate(m_canvasPrefab);
        m_AmmoText = playerUILinker.ammoText;
        m_HealthText = playerUILinker.healthText;
        m_ArmorText = playerUILinker.armorText;
        UpdateHealth();
        UpdateArmor();
        UpdateCurrentWeaponAmmo();
    }
    private void Awake()
    {
        m_player = GetComponent<Player>();
    }
    private void OnEnable()
    {
        m_player.OnWeaponSwitch += WeaponSwitchHandle;
        m_player.OnHeal += HealHandle;
        m_player.OnRepairArmor += RepairArmorHandle;
        m_player.OnTakeDamage += TakeDamageHandle;
    }
    private void OnDisable()
    {
        m_player.OnWeaponSwitch -= WeaponSwitchHandle;
        m_player.OnHeal -= HealHandle;
        m_player.OnRepairArmor -= RepairArmorHandle;
        m_player.OnTakeDamage -= TakeDamageHandle;
    }

    private void SubscribeToWeapon(BaseWeapon weapon)
    {
        if (weapon == null) return;
        weapon.OnFire += FireHandle;
        weapon.OnReload += ReloadHandle;
        weapon.OnAmmoChange += AmmoChangeHandle;
    }
    private void UnsubscribeFromWeapon(BaseWeapon weapon)
    {
        if (weapon == null) return;
        weapon.OnFire -= FireHandle;
        weapon.OnReload -= ReloadHandle;
        weapon.OnAmmoChange -= AmmoChangeHandle;
    }
    private void HealHandle(Player healer, int heal)
    {
        UpdateHealth();
    }
    private void RepairArmorHandle(Player repairer, int damage)
    {
        UpdateArmor();
    }
    private void TakeDamageHandle(Player killer, int armor)
    {
        UpdateHealth();
        UpdateArmor();
    }
    private void AmmoChangeHandle()
    {
        UpdateCurrentWeaponAmmo();
    }
    private void WeaponSwitchHandle(BaseWeapon previousWeapon)
    {
        UnsubscribeFromWeapon(previousWeapon);
        SubscribeToWeapon(m_player.CurrentWeapon);
        UpdateCurrentWeaponAmmo();
    }
    private void FireHandle()
    {
        UpdateCurrentWeaponAmmo();
    }
    private void ReloadHandle()
    {
        UpdateCurrentWeaponAmmo();
    }
    private void UpdateCurrentWeaponAmmo()
    {
        if (m_player == null || m_player.CurrentWeapon == null || m_AmmoText == null) return;
        m_AmmoText.text = $"{m_player.CurrentWeapon.CurrentAmmo}/{m_player.CurrentWeapon.Clip}";
    }
    private void UpdateHealth()
    {
        if (m_player == null || m_HealthText == null) return;
        m_HealthText.text = $"HP {m_player.Health}/{m_player.MaxHealth}";
    }
    private void UpdateArmor()
    {
        if (m_player == null || m_ArmorText == null) return;
        m_ArmorText.text = $"AP {m_player.Armor}";
    }
}
