using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;

[ExecuteAlways] // ��������� ��������� ��� � ��������� � Play Mode
[System.Serializable]
public class NodeWaypoint : MonoBehaviour
{
    public GameObject Ground; // ����� ���� null ��� ������� ��������� �����

    [SerializeField]
    public List<NodeWaypoint> Connections = new List<NodeWaypoint>();

    [SerializeField]
    public List<NodeAiAction> NodeActions = new List<NodeAiAction>();
    public void AddAction(NodeAiAction.ActionType type, Vector2 position)
    {
        // ������ ����� ������ ��� ��������
        GameObject nodeObject = new GameObject("Node Action");

        // ������������� ������������ ������, ����� ����� ������ ��� ��������
        nodeObject.transform.SetParent(transform, false);
        nodeObject.transform.position = position;

        // ��������� ��������� NodeAiAction
        NodeAiAction nodeAiAction = nodeObject.AddComponent<NodeAiAction>();

        // ������������� ��� ��������
        nodeAiAction.SetAction(type);

        // ��������� �������� � ������ (���� �����)
        NodeActions.Add(nodeAiAction);

        // ���� ��� �������������� � Unity, ����� �������� Undo ��� ������ ��������
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(nodeObject, "Add Node Action");
#endif
    }
    private void OnDrawGizmos()
    {
        // ������������ ����
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.05f);

        // ������������ ����������
        Gizmos.color = Color.yellow;
        foreach (var connectedNode in Connections)
        {
            if (connectedNode != null)
            {
                Gizmos.DrawLine(transform.position, connectedNode.transform.position);
            }
        }

        // ������������ ��������
        Gizmos.color = Color.blue;
        foreach (var action in NodeActions)
        {
            if (action != null)
            {
                Gizmos.DrawLine(transform.position, action.transform.position);
            }
        }
    }
}
