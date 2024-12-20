using UnityEngine;

public class DynamicCrosshair : MonoBehaviour
{
    public Texture2D crosshairTexture; // �������� ��� �������
    public Vector2 baseSize = new Vector2(32, 32); // ������� ������ �������
    public float recoilEffect = 1f; // ������ ������ �� ������ �������
    public float recoilDecaySpeed = 2f; // �������� ���������� ������

    private Vector2 currentSize; // ������� ������ �������
    private float recoil; // ������� ������

    void Start()
    {
        currentSize = baseSize;
    }

    void Update()
    {
        // ����������� ���������� ������
        if (recoil > 0)
        {
            recoil -= recoilDecaySpeed * Time.deltaTime;
            recoil = Mathf.Max(recoil, 0);
        }

        // �������� ������ ������� � ����������� �� ������� ������
        currentSize = baseSize + Vector2.one * recoil;
    }

    public void ApplyRecoil(float amount)
    {
        // ����������� ������� ������
        recoil += amount;
    }

    void OnGUI()
    {
        if (crosshairTexture != null)
        {
            // ������ ������
            Vector2 cursorPos = Event.current.mousePosition;
            Rect crosshairRect = new Rect(
                cursorPos.x - currentSize.x / 2,
                cursorPos.y - currentSize.y / 2, // �������� Y ��� GUI
                currentSize.x,
                currentSize.y
            );
            GUI.DrawTexture(crosshairRect, crosshairTexture);
        }
    }
}
