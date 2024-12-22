using System;
using System.Collections.Generic;
using UnityEngine;
using static Player;

#if UNITY_EDITOR
using UnityEditor;
#endif
public enum Team
{
    None,
    Blue,
    Red,
    Yellow,
    Green,
}
public class Player : MonoBehaviour
{
    // Поля класса
    private Team m_playerTeam = Team.None;
    private HashSet<string> abilities = new HashSet<string>();
    private HashSet<string> statusEffects = new HashSet<string>();

    // Свойства
    public string Name { get; private set; } = "";
    public int Health { get; private set; } = 100;
    public int MaxHealth { get; private set; } = 100;
    public int Armor { get; private set; } = 0;
    public int MaxArmor { get; private set; } = 100;
    public bool IsDead { get; private set; } = false;
    public int CashBalance { get; private set; } = 0;
    public Team PlayerTeam { get { return m_playerTeam; } private set { m_playerTeam = value; } }
    public NodeFlag CapturedFlag { get; private set; } = null;
    public Dictionary<WeaponTypeEnum, BaseWeapon> KvpWeapons { get; private set; } = new Dictionary<WeaponTypeEnum, BaseWeapon>();
    public BaseWeapon CurrentWeapon { get; private set; }
    [SerializeField] private Transform m_hand;
    public Transform Hand { get { return m_hand; } set { m_hand = value; } }

    // События
    public event Action<Player, int> OnTakeDamage;
    public event Action<Player, int> OnHeal;
    public event Action<Player, int> OnRepairArmor;
    public event Action<Player, Player> OnDead;
    public event Action<Player, NodeFlag> OnFlagLost;
    public event Action<BaseWeapon> OnWeaponSwitch;
    private void Awake()
    {
        Health = Mathf.Clamp(Health, 0, MaxHealth);
    }
    public bool GetFlag(NodeFlag nodeFlag)
    {
        if (!IsDead && CapturedFlag == null)
        {
            CapturedFlag = nodeFlag;
            return true;
        }
        return false;
    }
    public NodeFlag ResetFlag(bool lostFlag = true)
    {
        if (CapturedFlag == null) return null;
        NodeFlag deliveredFlag = CapturedFlag;
        CapturedFlag = null;
        if (lostFlag)
        {
            deliveredFlag.ResetCapture();
            OnFlagLost?.Invoke(this, deliveredFlag);
        }
        return deliveredFlag;
    }
    public bool HasFlag()
    {
        return CapturedFlag != null;
    }
    public void SetName(string name)
    {
        Name = name;
    }
    public void SetTeam(Team team)
    {
        PlayerTeam = team;
    }
    private void Update()
    {
        if (transform.position.y < -100)
        {
            Kill();
        }
    }
    public void Heal(Player healer, int health)
    {
        if (IsDead) return; // Не лечим мертвого игрока

        if (health < 0)
        {
            Debug.LogWarning("Heal amount cannot be negative.");
            return;
        }

        int previousHealth = Health;
        Health = Mathf.Min(Health + health, MaxHealth); // Учитываем максимальное здоровье

        int healedAmount = Health - previousHealth; // Рассчитываем фактическое количество исцеления

        if (healedAmount > 0)
        {
            if (healer != null && healer != this)
            {
                Debug.Log($"{healer.Name} healed {Name} by {healedAmount} health points.");
            }
            else
            {
                Debug.Log($"{Name} healed themselves by {healedAmount} health points.");
            }
        }
        else
        {
            Debug.Log($"{Name} is already at maximum health.");
        }
        OnHeal?.Invoke(healer, healedAmount);
    }
    public void AddArmor(Player repairer, int armorAmount)
    {
        if (IsDead) return; // Не добавляем броню мертвому игроку

        if (armorAmount < 0)
        {
            Debug.LogWarning("Armor amount cannot be negative.");
            return;
        }

        int initialArmor = Armor;
        Armor = Mathf.Min(Armor + armorAmount, MaxArmor); // Учитываем максимальное количество брони

        int addedArmor = Armor - initialArmor; // Рассчитываем количество добавленной брони

        if (addedArmor > 0)
        {
            if (repairer != null && repairer != this)
            {
                Debug.Log($"{repairer.Name} added {addedArmor} armor points to {Name}.");
            }
            else
            {
                Debug.Log($"{Name} added {addedArmor} armor points to themselves.");
            }
        }
        else
        {
            Debug.Log($"{Name} is already at maximum armor.");
        }
        OnRepairArmor?.Invoke(repairer, addedArmor);
    }

    public void TakeDamage(Player killer, int damage)
    {
        if (IsDead) return;
        int modifiedDamage = Mathf.RoundToInt(damage * (Armor > 0 ? 0.5f : 1f));
        int armorDamage = 0;
        Health -= modifiedDamage;
        Health = Mathf.Max(Health, 0);
        if (Health <= 0)
        {
            IsDead = true;
            ResetFlag();
            OnDead.Invoke(this, killer);
        }
        if (Armor > 0)
        {
            armorDamage = Mathf.RoundToInt(damage * 0.8f);
            Armor -= armorDamage;
            Armor = Mathf.Max(Armor, 0);
        }
        Debug.Log($"{Name} took damage {modifiedDamage}");
        OnTakeDamage?.Invoke(killer, modifiedDamage);
    }

    public void Kill()
    {
        TakeDamage(this, Health);
    }

    public void Revive()
    {
        if (!IsDead) return;
        Health = 100;
        IsDead = false;
    }

    public void SelectWeapon(BaseWeapon baseWeapon)
    {
        if (IsDead) return;
        if (baseWeapon == null) return;
        BaseWeapon previousWeapon;
        if (!KvpWeapons.ContainsKey(baseWeapon.WeaponType))
        {
            return;
        }
        else
        {
            previousWeapon = CurrentWeapon;
            if (CurrentWeapon == null)
            {
                CurrentWeapon = KvpWeapons[baseWeapon.WeaponType];
            }
            else
            {
                CurrentWeapon.transform.gameObject.SetActive(false);
                CurrentWeapon = KvpWeapons[baseWeapon.WeaponType];
                CurrentWeapon.transform.gameObject.SetActive(true);
            }
        }
        Debug.Log($"Current weapon is: {CurrentWeapon.name}");
        OnWeaponSwitch?.Invoke(previousWeapon);
    }

    public void PickUpWeapon(BaseWeapon weapon)
    {
        if (weapon == null || IsDead) return;
        if (KvpWeapons.ContainsKey(weapon.WeaponType)) return;
        weapon.SetOwner(this);
        KvpWeapons.Add(weapon.WeaponType, weapon);

        if (CurrentWeapon != null)
        {
            weapon.gameObject.SetActive(false);
        }

        if (!weapon.IsEquipped)
        {
            weapon.EquipWeapon(this);
            SelectWeapon(weapon);
        }
        Debug.Log($"{weapon.name} added to {weapon.Player}");
    }

    public GameObject ThrowWeapon()
    {
        if (CurrentWeapon == null || IsDead) return null;
        KvpWeapons.Remove(CurrentWeapon.WeaponType);
        CurrentWeapon.UnEquipWeapon(this);
        GameObject weaponObject = CurrentWeapon.gameObject;
        CurrentWeapon = null;
        return weaponObject;
    }

    // Ability methods
    public bool HasAbility(string abilityName)
    {
        return abilities.Contains(abilityName);
    }

    public void AddAbility(string abilityName)
    {
        abilities.Add(abilityName);
    }

    public void UseAbility(string abilityName)
    {
        if (HasAbility(abilityName))
        {
            // Implement ability logic here
            Debug.Log($"{Name} used ability: {abilityName}");
        }
    }

    // Status effect methods
    public bool HasStatusEffect(string statusName)
    {
        return statusEffects.Contains(statusName);
    }

    public void AddStatusEffect(string statusName)
    {
        statusEffects.Add(statusName);
    }

    public void RemoveStatusEffect(string statusName)
    {
        statusEffects.Remove(statusName);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Отображение текста с HP и броней над объектом игрока
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        style.fontSize = 7;
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 position = transform.position + Vector3.up * 2; // Над головой игрока
        Handles.Label(position, $"HP: {Health}\nArmor: {Armor}", style);
        if (HasFlag())
        {
            Gizmos.color = PlayerUT.GetColorByTeam(CapturedFlag.team);
            Gizmos.DrawSphere(transform.position + Vector3.up, 1f);
        }
    }
#endif
}
public static class PlayerUT
{
    public static Color GetColorByTeam(Team team)
    {
        Color color;
        switch (team)
        {
            case Team.Blue:
                color = Color.blue;
                break;
            case Team.Red:
                color = Color.red;
                break;
            case Team.Green:
                color = Color.green;
                break;
            case Team.Yellow:
                color = Color.yellow;
                break;
            default:
                color = Color.white;
                break;
        }
        return color;
    }
}