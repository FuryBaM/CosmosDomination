using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;

[ExecuteAlways] // Позволяет выполнять код в редакторе и Play Mode
[System.Serializable]
public class NodeWaypoint : MonoBehaviour
{
    public GameObject Ground; // Может быть null для вручную созданных узлов

    [SerializeField]
    public List<NodeWaypoint> Connections = new List<NodeWaypoint>();

    [SerializeField]
    public List<NodeAiAction> NodeActions = new List<NodeAiAction>();
    public void AddAction(NodeAiAction.ActionType type, Vector2 position)
    {
        // Создаём новый объект для действия
        GameObject nodeObject = new GameObject("Node Action");

        // Устанавливаем родительский объект, чтобы новый объект был дочерним
        nodeObject.transform.SetParent(transform, false);
        nodeObject.transform.position = position;

        // Добавляем компонент NodeAiAction
        NodeAiAction nodeAiAction = nodeObject.AddComponent<NodeAiAction>();

        // Устанавливаем тип действия
        nodeAiAction.SetAction(type);

        // Добавляем действие в список (если нужно)
        NodeActions.Add(nodeAiAction);

        // Если это редактирование в Unity, можно добавить Undo для отмены действия
#if UNITY_EDITOR
        Undo.RegisterCreatedObjectUndo(nodeObject, "Add Node Action");
#endif
    }
    private void OnDrawGizmos()
    {
        // Визуализация узла
        Gizmos.color = Color.white;
        Gizmos.DrawSphere(transform.position, 0.05f);

        // Визуализация соединений
        Gizmos.color = Color.yellow;
        foreach (var connectedNode in Connections)
        {
            if (connectedNode != null)
            {
                Gizmos.DrawLine(transform.position, connectedNode.transform.position);
            }
        }

        // Визуализация действий
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
