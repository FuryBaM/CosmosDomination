using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using static Player;
using static NodePickup;
using System;

[CustomEditor(typeof(NavMesh2D))]
public class NavMesh2DEditor : Editor
{
    [SerializeField] private NodeSpawn nodeSpawnPrefab;
    [SerializeField] private NodeFlag nodeFlagPrefab;
    [SerializeField] private NodePickup nodePickupPrefab;
    [SerializeField] private NodeWaypoint nodeWaypointPrefab;
    [SerializeField] private NodeHoldpoint nodeHoldpointPrefab;
    public enum NodeType
    {
        Waypoint,
        AiAction,
        Spawn,
        Pickup,
        Holdpoint,
        Flag
    }

    private NavMesh2D navMesh;
    private bool editMode = false;
    private GameObject selectedNode = null;
    private NodeType selectedNodeType = NodeType.Waypoint;
    private PickupType selectedPickupType = PickupType.Weapon;
    private Team selectedTeam = Team.None;

    // Переменные для перетаскивания узлов
    private bool isDraggingNode = false;
    private Vector3 dragOffset;

    private void OnEnable()
    {
        navMesh = (NavMesh2D)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        // Переключение режима редактирования
        editMode = GUILayout.Toggle(editMode, "Режим редактирования узлов", "Button");
    }

    private void OnSceneGUI()
    {
        if (!editMode) return;

        Handles.BeginGUI();

        GUILayout.BeginArea(new Rect(10, 10, 210, 300), "", EditorStyles.helpBox);
        GUILayout.Space(10);

        DrawNodeTypeSelection();
        DrawAddNodeButton();
        DrawNodeSpecificSettings();
        DrawNodeManagementButtons();
        DrawNodeData();

        GUILayout.EndArea();

        Handles.EndGUI();
    }

    // Выбор типа узла
    private void DrawNodeTypeSelection()
    {
        selectedNodeType = (NodeType)EditorGUILayout.EnumPopup("Тип узла", selectedNodeType);
    }

    // Кнопка добавления узла
    private void DrawAddNodeButton()
    {
        if (GUILayout.Button("Добавить узел в центр сцены"))
        {
            AddNodeAtCenter();
        }
    }

    // Настройки для выбранного узла
    private void DrawNodeSpecificSettings()
    {
        if (selectedNode != null)
        {
            DrawWaypointSettings();
            DrawPickupSettings();
            DrawTeamSettings();
            DrawDeleteNodeButton();
        }
    }

    // Настройки для узла Waypoint
    private void DrawWaypointSettings()
    {
        NodeWaypoint waypoint = selectedNode.GetComponent<NodeWaypoint>();
        if (waypoint != null && GUILayout.Button("Добавить AI Action"))
        {
            AddAiActionToWaypoint(waypoint, waypoint.transform.position);
        }
    }

    private void DrawNodeData()
    {
        if (selectedNode == null) return;
        NodeWaypoint waypoint = selectedNode.GetComponent<NodeWaypoint>();
        if (waypoint != null)
        {
            GUILayout.Label("Connections:", EditorStyles.boldLabel);
            DrawList("Connection", waypoint.Connections, (index) =>
            {
                // Допустим, кнопка для удаления
                if (GUILayout.Button("Удалить", GUILayout.Width(100)))
                {
                    Undo.RecordObject(waypoint, "Remove Connection");
                    waypoint.Connections.RemoveAt(index);
                    EditorUtility.SetDirty(waypoint);
                }
            });

            if (waypoint.NodeActions.Count > 0)
            {
                GUILayout.Label("Node AI Actions:", EditorStyles.boldLabel);
                DrawList("Action", waypoint.NodeActions, (index) =>
                {
                    NodeAiAction action = waypoint.NodeActions[index];

                    // Преобразование строки в Enum и отображение выпадающего списка
                    if (System.Enum.TryParse(action.ActionName, out NodeAiAction.ActionType actionType))
                    {
                        NodeAiAction.ActionType newActionType = (NodeAiAction.ActionType)EditorGUILayout.EnumPopup(actionType, GUILayout.Width(100));

                        // Если значение изменилось, обновляем строку
                        if (newActionType != actionType)
                        {
                            Undo.RecordObject(action, "Change Action Type");
                            action.SetAction(newActionType); // Предполагается, что SetAction обновляет ActionName
                            EditorUtility.SetDirty(action);
                        }
                    }
                    else
                    {
                        // Если строка не соответствует Enum, выводим ошибку или задаём значение по умолчанию
                        GUILayout.Label("Invalid Action Type", GUILayout.Width(100));
                    }
                });
            }
        }

        NodeSpawn spawn = selectedNode.GetComponent<NodeSpawn>();
        if (spawn != null)
        {
            GUILayout.Label("Spawn Settings:", EditorStyles.boldLabel);
            GUILayout.Label($"Current Team: {spawn.teamSpawn.ToString()}");
        }

        NodeFlag flag = selectedNode.GetComponent<NodeFlag>();
        if (flag != null)
        {
            GUILayout.Label("Flag Settings:", EditorStyles.boldLabel);
            GUILayout.Label($"Current Team: {flag.team.ToString()}");
        }

        NodePickup pickup = selectedNode.GetComponent<NodePickup>();
        if (pickup != null)
        {
            GUILayout.Label("Pickup Settings:", EditorStyles.boldLabel);
            GUILayout.Label($"Current Pickup: {pickup.pickupType.ToString()}");
        }
    }
    private void DrawList<T>(string label, IList<T> list, System.Action<int> drawElementActions)
    {
        for (int i = 0; i < list.Count; i++)
        {
            GUILayout.BeginHorizontal();

            // Если элемент — Unity Object, выводим его InstanceID
            if (list[i] is UnityEngine.Object unityObject)
            {
                GUILayout.Label($"ID: {unityObject.GetInstanceID()}", GUILayout.Width(100));
            }
            else
            {
                GUILayout.Label($"{i + 1}: {list[i]?.ToString() ?? "null"}", GUILayout.Width(100));
            }

            // Выполняем действия для элемента
            drawElementActions?.Invoke(i);

            GUILayout.EndHorizontal();
        }
    }


    // Настройки для узла Pickup
    private void DrawPickupSettings()
    {
        NodePickup nodePickup = selectedNode.GetComponent<NodePickup>();
        if (nodePickup != null)
        {
            selectedPickupType = (PickupType)EditorGUILayout.EnumPopup("Тип пикапа", selectedPickupType);
            if (GUILayout.Button("Изменить тип пикапа"))
            {
                ChangePickupType(nodePickup);
            }
        }
    }

    // Настройки для узлов команды (Team)
    private void DrawTeamSettings()
    {
        NodeSpawn nodeSpawn = selectedNode.GetComponent<NodeSpawn>();
        NodeFlag nodeFlag = selectedNode.GetComponent<NodeFlag>();
        if (nodeSpawn != null || nodeFlag != null)
        {
            GUILayout.Label("Команда:");
            selectedTeam = (Team)EditorGUILayout.EnumPopup("Команда (Team)", selectedTeam);
            if (GUILayout.Button("Изменить команду(team)"))
            {
                if (nodeSpawn != null)
                {
                    ChangeSpawnNodeTeam(nodeSpawn);
                }
                else if (nodeFlag != null)
                {
                    ChangeFlagNodeTeam(nodeFlag);
                }
            }
        }
    }

    // Кнопка удаления узла
    private void DrawDeleteNodeButton()
    {
        if (GUILayout.Button("Удалить выбранный узел"))
        {
            RemoveSelectedNode();
        }
    }

    // Кнопки управления узлами (сохранение/загрузка)
    private void DrawNodeManagementButtons()
    {
        if (GUILayout.Button("Сохранить узлы"))
        {
            navMesh.SaveNodes();
        }

        if (GUILayout.Button("Загрузить узлы"))
        {
            navMesh.LoadNodes();
        }
    }


    private void ChangePickupType(NodePickup nodePickup)
    {
        nodePickup.pickupType = selectedPickupType;
    }

    private void ChangeSpawnNodeTeam(NodeSpawn nodeSpawn)
    {
        nodeSpawn.teamSpawn = selectedTeam;
    }

    private void ChangeFlagNodeTeam(NodeFlag nodeFlag)
    {
        nodeFlag.team = selectedTeam;
    }

    private void AddAiActionToWaypoint(NodeWaypoint waypoint, Vector3 mousePosition)
    {
        // Создаем новый объект для NodeAiAction
        GameObject aiActionObj = new GameObject("AiAction");
        aiActionObj.transform.position = mousePosition;

        // Добавляем компонент NodeAiAction
        NodeAiAction aiAction = aiActionObj.AddComponent<NodeAiAction>();
        aiAction.transform.SetParent(waypoint.transform, true);

        // Если у NodeWaypoint есть список для действий, добавляем NodeAiAction в этот список
        if (waypoint.NodeActions == null)
            waypoint.NodeActions = new List<NodeAiAction>();

        waypoint.NodeActions.Add(aiAction);
        aiAction.SetAction(NodeAiAction.ActionType.Jump);
        aiAction.GetComponent<BoxCollider2D>().isTrigger = true;
        // Отметим объект как измененный
        EditorUtility.SetDirty(waypoint);
        Undo.RegisterCreatedObjectUndo(aiActionObj, "Create Ai Action");
    }

    private void AddNodeAtPosition(Vector3 position)
    {
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Add Node");
        int group = Undo.GetCurrentGroup();

        GameObject nodeObj = null;

        switch (selectedNodeType)
        {
            case NodeType.Waypoint:
                nodeObj = Instantiate(nodeWaypointPrefab, position, Quaternion.identity).gameObject;
                break;

            case NodeType.Spawn:
                nodeObj = Instantiate(nodeSpawnPrefab, position, Quaternion.identity).gameObject;
                break;

            case NodeType.Pickup:
                nodeObj = Instantiate(nodePickupPrefab, position, Quaternion.identity).gameObject;
                break;

            case NodeType.Holdpoint:
                nodeObj = Instantiate(nodeHoldpointPrefab, position, Quaternion.identity).gameObject;
                break;

            case NodeType.Flag:
                nodeObj = Instantiate(nodeFlagPrefab, position, Quaternion.identity).gameObject;
                break;

            default:
                Undo.CollapseUndoOperations(group);
                EditorUtility.SetDirty(navMesh);
                return;
        }

        // Устанавливаем родительский объект, если необходимо
        nodeObj.transform.SetParent(navMesh.transform, false);

        // Регистрация для Undo
        Undo.RegisterCreatedObjectUndo(nodeObj, "Create Node");

        Undo.CollapseUndoOperations(group);
        EditorUtility.SetDirty(navMesh);
    }

    private void AddNodeAtCenter()
    {
        Vector3 position = SceneView.lastActiveSceneView.pivot;
        AddNodeAtPosition(position);
    }

    private void RemoveSelectedNode()
    {
        if (selectedNode == null) return;

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Delete Node");
        int group = Undo.GetCurrentGroup();

        if (selectedNode.GetComponent<NodeWaypoint>() != null)
        {
            // Удаляем соединения с другими узлами
            foreach (var node in navMesh.GetNodes())
            {
                if (node.Connections.Contains(selectedNode.GetComponent<NodeWaypoint>()))
                {
                    Undo.RecordObject(node, "Remove Connection");
                    node.Connections.Remove(selectedNode.GetComponent<NodeWaypoint>());
                }
            }

            navMesh.GetNodes().Remove(selectedNode.GetComponent<NodeWaypoint>());
        }

        Undo.DestroyObjectImmediate(selectedNode);

        selectedNode = null;

        Undo.CollapseUndoOperations(group);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!editMode || navMesh == null) return;

        HandleMouseEvents();
        DrawNodes();
    }

    private GameObject selectedAiAction = null;
    private Vector3 aiActionDragOffset;

    private void HandleMouseEvents()
    {
        Event e = Event.current;
        Vector3 mousePosition = GetMouseWorldPosition(e);

        switch (e.type)
        {
            case EventType.MouseDown:
                HandleMouseDown(e, mousePosition);
                break;
            case EventType.MouseDrag:
                HandleMouseDrag(e, mousePosition);
                break;
            case EventType.MouseUp:
                HandleMouseUp(e);
                break;
            case EventType.KeyDown:
                HandleKeyDown(e, mousePosition);
                break;
            case EventType.MouseMove:
                HandleMouseMove(mousePosition);
                break;
        }
    }

    private Vector3 GetMouseWorldPosition(Event e)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 position = ray.origin;
        position.z = 0f; // Ограничиваем координату Z
        return position;
    }

    private void HandleMouseDown(Event e, Vector3 mousePosition)
    {
        if (e.button == 0)
        {
            if (IsAltPressed(e))
                HandleAltLeftClick(mousePosition, e);
            else
                HandleLeftClick(mousePosition, e);
        }
        else if (e.button == 1)
        {
            AddNodeAtPosition(mousePosition);
            e.Use();
        }
    }

    private void HandleAltLeftClick(Vector3 mousePosition, Event e)
    {
        GameObject hitNode = GetNodeUnderMouse(mousePosition);
        if (hitNode != null && selectedNode != null)
        {
            SetSpawnWaypoint(hitNode, selectedNode, e);
            CreateConnection(hitNode, selectedNode, e);
        }
    }

    private void SetSpawnWaypoint(GameObject hitNode, GameObject selectedNode, Event e)
    {
        NodeSpawn hitSpawn = hitNode.GetComponent<NodeSpawn>();
        NodeWaypoint selectedWaypoint = selectedNode.GetComponent<NodeWaypoint>();

        if (hitSpawn != null && selectedWaypoint != null)
        {
            Undo.RecordObject(hitSpawn, "Add spawn waypoint");
            Undo.RecordObject(selectedWaypoint, "Add spawn waypoint");
            hitSpawn.waypoint = selectedWaypoint;
            EditorUtility.SetDirty(hitSpawn);
            EditorUtility.SetDirty(selectedWaypoint);
        }
    }

    private void CreateConnection(GameObject hitNode, GameObject selectedNode, Event e)
    {
        NodeWaypoint hitWaypoint = hitNode.GetComponent<NodeWaypoint>();
        NodeWaypoint selectedWaypoint = selectedNode.GetComponent<NodeWaypoint>();

        if (hitWaypoint != null && selectedWaypoint != null && hitWaypoint != selectedWaypoint)
        {
            Undo.RecordObject(selectedWaypoint, "Add Connection");
            Undo.RecordObject(hitWaypoint, "Add Connection");

            AddConnection(selectedWaypoint, hitWaypoint);

            if (IsShiftPressed(e))
                AddConnection(hitWaypoint, selectedWaypoint);

            EditorUtility.SetDirty(selectedWaypoint);
            EditorUtility.SetDirty(hitWaypoint);

            e.Use();
        }
    }

    private void AddConnection(NodeWaypoint source, NodeWaypoint target)
    {
        if (!source.Connections.Contains(target))
            source.Connections.Add(target);
    }

    private void HandleLeftClick(Vector3 mousePosition, Event e)
    {
        GameObject hitNode = GetNodeUnderMouse(mousePosition);
        if (hitNode != null)
        {
            if (TrySelectAiAction(hitNode, mousePosition))
                return;

            SelectNode(hitNode, mousePosition);
            e.Use();
        }
    }

    private void SelectNode(GameObject hitNode, Vector3 mousePosition)
    {
        selectedNode = hitNode;
        isDraggingNode = true;
        dragOffset = selectedNode.transform.position - mousePosition;
    }

    private bool TrySelectAiAction(GameObject hitNode, Vector3 mousePosition)
    {
        NodeAiAction aiAction = hitNode.GetComponent<NodeAiAction>();
        if (aiAction != null && aiAction.transform.parent == selectedNode.transform)
        {
            selectedAiAction = aiAction.gameObject;
            aiActionDragOffset = selectedAiAction.transform.position - mousePosition;
            return true;
        }
        return false;
    }

    private void HandleMouseDrag(Event e, Vector3 mousePosition)
    {
        if (e.button != 0) return;

        if (selectedAiAction != null)
            DragAiAction(mousePosition);
        else if (isDraggingNode)
            DragNode(mousePosition);

        e.Use();
    }

    private void DragAiAction(Vector3 mousePosition)
    {
        Undo.RecordObject(selectedAiAction.transform, "Move Ai Action");
        selectedAiAction.transform.position = mousePosition + aiActionDragOffset;
        EditorUtility.SetDirty(selectedAiAction);
    }

    private void DragNode(Vector3 mousePosition)
    {
        Undo.RecordObject(selectedNode.transform, "Move Node");
        selectedNode.transform.position = mousePosition + dragOffset;
        EditorUtility.SetDirty(selectedNode);
    }

    private void HandleMouseUp(Event e)
    {
        if (e.button != 0) return;

        if (selectedAiAction != null)
            selectedAiAction = null;
        else if (isDraggingNode)
            isDraggingNode = false;

        e.Use();
    }

    private void HandleKeyDown(Event e, Vector3 mousePosition)
    {
        if (e.keyCode == KeyCode.R)
            RemoveAiActionUnderMouse(mousePosition, e);
        else if (e.keyCode == KeyCode.S)
            ToggleAiActionType(e);
        else if (e.character == 'a')
            AddAiAction(mousePosition, e);
    }

    private void RemoveAiActionUnderMouse(Vector3 mousePosition, Event e)
    {
        GameObject hitNode = GetAiActionUnderMouse(mousePosition);
        if (hitNode != null && selectedNode != null)
        {
            NodeWaypoint waypoint = selectedNode.GetComponent<NodeWaypoint>();
            NodeAiAction aiAction = hitNode.GetComponent<NodeAiAction>();

            if (waypoint != null && aiAction != null)
            {
                Undo.RecordObject(waypoint, "Remove Ai Action");
                waypoint.NodeActions.Remove(aiAction);
                Undo.DestroyObjectImmediate(hitNode);
                EditorUtility.SetDirty(waypoint);
                e.Use();
            }
        }
    }

    private void ToggleAiActionType(Event e)
    {
        if (selectedAiAction == null) return;

        NodeAiAction aiAction = selectedAiAction.GetComponent<NodeAiAction>();
        if (aiAction != null)
        {
            Undo.RecordObject(aiAction, "Change Action Type");
            aiAction.SetAction(aiAction.ActionName == "Jump"
                ? NodeAiAction.ActionType.DoubleJump
                : NodeAiAction.ActionType.Jump);
            EditorUtility.SetDirty(aiAction);
            e.Use();
        }
    }

    private void AddAiAction(Vector3 mousePosition, Event e)
    {
        if (selectedNode != null)
        {
            NodeWaypoint waypoint = selectedNode.GetComponent<NodeWaypoint>();
            if (waypoint != null)
            {
                AddAiActionToWaypoint(waypoint, mousePosition);
                e.Use();
            }
        }
    }

    private void HandleMouseMove(Vector3 mousePosition)
    {
        GameObject hitNode = GetAiActionUnderMouse(mousePosition);
        selectedAiAction = hitNode != null && selectedNode != null && hitNode.transform.parent == selectedNode.transform
            ? hitNode
            : null;
        SceneView.RepaintAll();
    }

    // Вспомогательные методы
    private bool IsAltPressed(Event e) => (e.modifiers & EventModifiers.Alt) != 0;
    private bool IsShiftPressed(Event e) => (e.modifiers & EventModifiers.Shift) != 0;


    private GameObject GetNodeUnderMouse(Vector3 mousePosition)
    {
        float minDistance = 0.2f; // Допустимое расстояние до узла
        GameObject closestNode = null;

        foreach (Transform child in navMesh.transform)
        {
            float distance = Vector2.Distance(child.position, mousePosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestNode = child.gameObject;
            }
        }

        return closestNode;
    }

    // Добавлено: Получение NodeAiAction под курсором
    private GameObject GetAiActionUnderMouse(Vector3 mousePosition)
    {
        if (selectedNode == null) return null;

        NodeWaypoint waypoint = selectedNode.GetComponent<NodeWaypoint>();
        if (waypoint == null || waypoint.NodeActions == null) return null;

        float minDistance = 0.2f;
        GameObject closestAction = null;

        foreach (NodeAiAction action in waypoint.NodeActions)
        {
            if (action == null) continue;

            float distance = Vector2.Distance(action.transform.position, mousePosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestAction = action.gameObject;
            }
        }

        return closestAction;
    }

    private void DrawNodes()
    {
        if (navMesh.transform == null) return;

        foreach (Transform child in navMesh.transform)
        {
            Color nodeColor = GetNodeColor(child.gameObject);
            float nodeSize = 0.5f;

            if (child.gameObject == selectedNode)
            {
                nodeColor = Color.yellow;
            }

            Handles.color = nodeColor;
            Vector3 position = child.position;

            if (child.GetComponent<NodeWaypoint>())
            {
                DrawWaypointNode(position, nodeSize);

                // Рисуем связи между узлами
                NodeWaypoint waypoint = child.GetComponent<NodeWaypoint>();
                if (waypoint.Connections != null)
                {
                    foreach (NodeWaypoint connectedWaypoint in waypoint.Connections)
                    {
                        if (connectedWaypoint != null)
                        {
                            Handles.color = Color.yellow;
                            Handles.DrawLine(position, connectedWaypoint.transform.position);
                        }
                    }
                }

                DrawWaypointActions(waypoint);
            }
            else if (child.GetComponent<NodeSpawn>())
            {
                DrawSpawnNode(position, child.GetComponent<NodeSpawn>().waypoint, nodeSize);
            }
            else if (child.GetComponent<NodePickup>())
            {
                DrawPickupNode(position, nodeSize, child.GetComponent<NodePickup>().pickupType);
            }
            else if (child.GetComponent<NodeHoldpoint>())
            {
                DrawHoldpointNode(position, nodeSize);
            }
            else if (child.GetComponent<NodeFlag>())
            {
                DrawFlagNode(position, nodeSize);
            }
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontSize = 7;
            Handles.Label(position + Vector3.up * nodeSize, child.GetInstanceID().ToString(), style);
        }
    }

    private Color GetNodeColor(GameObject node)
    {
        if (node.GetComponent<NodeWaypoint>()) return Color.white;
        if (node.GetComponent<NodeSpawn>()) return Color.green;
        if (node.GetComponent<NodePickup>()) return Color.blue;
        if (node.GetComponent<NodeHoldpoint>()) return Color.red;
        if (node.GetComponent<NodeFlag>()) return Color.cyan;
        return Color.gray;
    }

    private void DrawWaypointNode(Vector3 position, float size)
    {
        Handles.DrawWireDisc(position, Vector3.forward, 0.1f);
    }

    private void DrawWaypointActions(NodeWaypoint waypoint)
    {
        if (waypoint == null || waypoint.NodeActions == null) return;

        foreach (NodeAiAction action in waypoint.NodeActions)
        {
            if (action == null) continue;

            Handles.color = Color.magenta;
            Vector3 actionPosition = action.transform.position;

            // Рисуем линию от узла к действию
            Handles.DrawDottedLine(waypoint.transform.position, actionPosition, 2f);

            // Рисуем BoxCollider2D действия
            BoxCollider2D collider = action.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                Vector2 colliderSize = collider.size;
                Vector2 colliderOffset = collider.offset;
                Vector3[] rectanglePoints = new Vector3[5];
                Vector3 colliderCenter = action.transform.position + (Vector3)colliderOffset;

                rectanglePoints[0] = colliderCenter + new Vector3(-colliderSize.x / 2, -colliderSize.y / 2);
                rectanglePoints[1] = colliderCenter + new Vector3(-colliderSize.x / 2, colliderSize.y / 2);
                rectanglePoints[2] = colliderCenter + new Vector3(colliderSize.x / 2, colliderSize.y / 2);
                rectanglePoints[3] = colliderCenter + new Vector3(colliderSize.x / 2, -colliderSize.y / 2);
                rectanglePoints[4] = rectanglePoints[0]; // Замыкаем прямоугольник

                Handles.DrawPolyLine(rectanglePoints);
            }
            else
            {
                // Если нет коллайдера, рисуем стандартный квадрат
                float actionSize = 0.3f;
                Handles.DrawWireCube(actionPosition, new Vector3(actionSize, actionSize, 0));
            }

            // Добавлено: отображение типа действия рядом с NodeAiAction
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontSize = 7;
            Handles.Label(actionPosition + Vector3.up * 0.2f, $"{action.ActionName}_{action.GetInstanceID()}", style);
        }
    }

    private void DrawSpawnNode(Vector3 position, NodeWaypoint wp, float size)
    {
        Vector3[] points = new Vector3[]
        {
            position + Vector3.up * size,
            position + Vector3.right * size,
            position + Vector3.down * size,
            position + Vector3.left * size
        };
        Handles.DrawAAConvexPolygon(points);
        Handles.DrawSolidDisc(position, Vector3.forward, 0.1f);
        if (wp!=null)
        Handles.DrawLine(
            position,
            wp.transform.position, size
        );
    }

    private void DrawPickupNode(Vector3 position, float size, PickupType pickupType)
    {
        Handles.DrawWireDisc(position, Vector3.forward, size);
        float crossSize = size * 0.5f;
        Handles.DrawLine(
            position + Vector3.up * crossSize,
            position + Vector3.down * crossSize
        );
        Handles.DrawLine(
            position + Vector3.left * crossSize,
            position + Vector3.right * crossSize
        );
        Handles.DrawSolidDisc(position, Vector3.forward, 0.1f);
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 7;
        Handles.Label(position + Vector3.up * 0.2f, pickupType.ToString(), style);
    }

    private void DrawHoldpointNode(Vector3 position, float size)
    {
        Vector3 halfSize = Vector3.one * size * 0.5f;
        Vector3[] points = new Vector3[]
        {
            position + new Vector3(-halfSize.x, halfSize.y, 0),
            position + new Vector3(halfSize.x, halfSize.y, 0),
            position + new Vector3(halfSize.x, -halfSize.y, 0),
            position + new Vector3(-halfSize.x, -halfSize.y, 0)
        };

        Handles.DrawLines(new Vector3[] {
            points[0], points[1],
            points[1], points[2],
            points[2], points[3],
            points[3], points[0],
            points[0], points[2],
            points[1], points[3]
        });
        Handles.DrawSolidDisc(position, Vector3.forward, 0.1f);
    }

    private void DrawFlagNode(Vector3 position, float size)
    {
        float poleHeight = size * 1.5f;
        float flagWidth = size * 0.8f;
        float flagHeight = size * 0.6f;

        Handles.DrawLine(
            position,
            position + Vector3.up * poleHeight
        );

        Vector3 flagBottom = position + Vector3.up * (poleHeight - flagHeight);
        Vector3[] flagPoints = new Vector3[]
        {
            flagBottom,
            flagBottom + Vector3.right * flagWidth,
            flagBottom + Vector3.up * flagHeight + Vector3.right * flagWidth,
            flagBottom + Vector3.up * flagHeight
        };

        Handles.DrawAAConvexPolygon(flagPoints);
        Handles.DrawSolidDisc(position, Vector3.forward, 0.1f);
    }
}