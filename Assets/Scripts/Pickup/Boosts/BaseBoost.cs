using UnityEngine;

// Базовый класс для всех бустов
public abstract class BaseBoost : MonoBehaviour
{
    public string boostName; // Имя буста
    public Sprite boostIcon; // Иконка для отображения
    public float duration; // Длительность действия буста (0, если мгновенный)

    // Метод для применения буста
    public abstract void ApplyBoost(Player player);

    // Опционально: Метод для завершения эффекта (если буст временный)
    public virtual void EndBoost(Player player)
    {
        Debug.Log($"{boostName} effect ended.");
    }
}
