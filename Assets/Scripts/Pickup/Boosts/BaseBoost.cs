using UnityEngine;

// ������� ����� ��� ���� ������
public abstract class BaseBoost : MonoBehaviour
{
    public string boostName; // ��� �����
    public Sprite boostIcon; // ������ ��� �����������
    public float duration; // ������������ �������� ����� (0, ���� ����������)

    // ����� ��� ���������� �����
    public abstract void ApplyBoost(Player player);

    // �����������: ����� ��� ���������� ������� (���� ���� ���������)
    public virtual void EndBoost(Player player)
    {
        Debug.Log($"{boostName} effect ended.");
    }
}
