using Unity.VisualScripting;
using UnityEngine;

public class PlayerHeightFromSprites : MonoBehaviour
{
    public float CalculateHeight()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("��� SpriteRenderer ��� ������� �����.");
            return 0f;
        }

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var renderer in renderers)
        {
            Bounds bounds = renderer.bounds;
            minY = Mathf.Min(minY, bounds.min.y);
            maxY = Mathf.Max(maxY, bounds.max.y);
        }

        return maxY - minY;
    }

    private void Start()
    {
        float height = CalculateHeight();
        Debug.Log($"���� ������ �� ��������: {height} ������.");
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            float height = CalculateHeight();
            Debug.Log($"���� ������ �� ��������: {height} ������.");
        }
    }
}
