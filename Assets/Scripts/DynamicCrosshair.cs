using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    public Texture2D crosshairTexture; // Текстура для прицела
    public Vector2 baseSize = new Vector2(32, 32); // Базовый размер прицела
    public float recoilEffect = 1f; // Эффект отдачи на размер прицела
    public float recoilDecaySpeed = 2f; // Скорость уменьшения отдачи

    private Vector2 currentSize; // Текущий размер прицела
    private float recoil; // Текущая отдача

    void Start()
    {
        currentSize = baseSize;
    }

    void Update()
    {
        // Постепенное уменьшение отдачи
        if (recoil > 0)
        {
            recoil -= recoilDecaySpeed * Time.deltaTime;
            recoil = Mathf.Max(recoil, 0);
        }

        // Изменяем размер прицела в зависимости от текущей отдачи
        currentSize = baseSize + Vector2.one * recoil;
    }

    public void ApplyRecoil(float amount)
    {
        // Увеличиваем текущую отдачу
        recoil += amount;
    }

    void OnGUI()
    {
        if (crosshairTexture != null)
        {
            // Рисуем прицел
            Vector2 cursorPos = Event.current.mousePosition;
            Rect crosshairRect = new Rect(
                cursorPos.x - currentSize.x / 2,
                cursorPos.y - currentSize.y / 2, // Инверсия Y для GUI
                currentSize.x,
                currentSize.y
            );
            GUI.DrawTexture(crosshairRect, crosshairTexture);
        }
    }
}
