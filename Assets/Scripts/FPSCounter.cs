using UnityEngine;
using UnityEditor;

public class FPSCounter : MonoBehaviour
{
    private float deltaTime = 0.0f;

    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    private void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(10, 10, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;
        style.normal.textColor = Color.white;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.} FPS", fps);
        GUI.Label(rect, text, style);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(FPSCounter))]
public class FPSCounterEditor : Editor
{
    private void OnSceneGUI()
    {
        Handles.BeginGUI();
        {
            GUILayout.BeginArea(new Rect(10, 10, 200, 50));
            GUIStyle style = new GUIStyle();
            style.fontSize = 16;
            style.normal.textColor = Color.green;

            float fps = 1.0f / Time.deltaTime;
            GUILayout.Label($"FPS: {fps:0.}", style);

            GUILayout.EndArea();
        }
        Handles.EndGUI();
    }
}
#endif
