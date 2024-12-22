using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent (typeof(BoxCollider2D))]
public class NodePickup : MonoBehaviour
{
    public enum PickupType
    {
        Weapon,
        Boost,
        Health,
        Armor
    }

    [Header("Pickup Settings")]
    public PickupType pickupType = PickupType.Weapon; // Тип пикапа

    // Предзаданные пути
    private List<string> weaponPaths = new List<string>
    {
        "Pickups/Glock",
        "Pickups/AssaultRifle",
        "Pickups/SniperRifle"
    };

    private List<string> boostPaths = new List<string>
    {
    };

    private string healthPath = "Pickups/Medkit"; // Путь к префабу здоровья
    private string armorPath = "Pickups/Armor"; // Путь к префабу брони

    [Header("Visual Settings")]
    public SpriteRenderer spriteRenderer; // Рендер спрайта для отображения пикапа
    public BoxCollider2D boxCollider2D; // Рендер спрайта для отображения пикапа

    [Header("Spawn Settings")]
    public Vector3 offset; // Оффсет для позиции пикапа
    public float spawnInterval = 10f; // Интервал респавна
    public bool autoRespawn = true; // Автоматический респавн

    private GameObject spawnedPickup; // Объект скрытого пикапа
    private BasePickup currentPickup; // Логика текущего пикапа
    private float spawnTimer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider2D = GetComponent<BoxCollider2D>();
        boxCollider2D.isTrigger = true;
        spawnTimer = spawnInterval;
        SetPickup();
    }

    private void Update()
    {
        if (autoRespawn && spawnedPickup == null)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                Debug.Log("Setting pickup");
                SetPickup();
                spawnTimer = spawnInterval;
            }
        }
    }

    private void SetPickup()
    {
        // Устанавливаем текущий пикап и загружаем его из ресурсов
        string path = GetPrefabPath();
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("No valid prefab path found for the selected pickup type.");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"Failed to load prefab at path: {path}");
            return;
        }

        if (spawnedPickup != null)
        {
            Destroy(spawnedPickup);
        }

        // Создаем объект, но скрываем его
        spawnedPickup = Instantiate(prefab, transform.position + offset, Quaternion.identity);
        spawnedPickup.SetActive(false);

        // Настроим спрайт через BasePickup
        currentPickup = spawnedPickup.GetComponent<BasePickup>();
        if (currentPickup != null)
        {
            spriteRenderer.sprite = currentPickup.pickupSprite; // Извлекаем спрайт из BasePickup
        }
        else
        {
            Debug.LogWarning("Pickup does not have BasePickup component!");
        }
    }

    private string GetPrefabPath()
    {
        switch (pickupType)
        {
            case PickupType.Weapon:
                return GetRandomPath(weaponPaths);
            case PickupType.Boost:
                return GetRandomPath(boostPaths);
            case PickupType.Health:
                return healthPath;
            case PickupType.Armor:
                return armorPath;
            default:
                return null;
        }
    }

    private string GetRandomPath(List<string> paths)
    {
        if (paths == null || paths.Count == 0)
        {
            Debug.LogWarning("Path list is empty for the selected type.");
            return null;
        }

        return paths[Random.Range(0, paths.Count)];
    }

    public void Interact(Player player)
    {
        if (spawnedPickup != null)
        {
            if (spawnedPickup.GetComponent<WeaponPickup>())
            {
                BaseWeapon weapon = spawnedPickup.GetComponent<BaseWeapon>();
                if (player.KvpWeapons.ContainsKey(weapon.WeaponType))
                {
                    if (!player.KvpWeapons[weapon.WeaponType].AddAmmoToClip(weapon.MaxAmmo)) return;
                    Destroy(weapon.gameObject);
                    spawnedPickup = null;
                    spriteRenderer.sprite = null;
                    spawnTimer = spawnInterval;
                    return;
                }
            }
            spawnedPickup.SetActive(true); // Делаем пикап активным
            var pickup = spawnedPickup.GetComponent<BasePickup>();
            if (pickup != null)
            {
                pickup.Pickup(player); // Вызываем логику подбора
            }
            spawnedPickup = null;
            spriteRenderer.sprite = null;
            spawnTimer = spawnInterval;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player;
        if (spawnedPickup == null) return;
        if (!collision.TryGetComponent<Player>(out player)) return;
        Interact(player);
    }
    private void OnDrawGizmos()
    {
        // Визуализация узла
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
}
