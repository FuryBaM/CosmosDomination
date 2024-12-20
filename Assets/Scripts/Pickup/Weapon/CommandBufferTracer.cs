using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class CommandBufferTracer : MonoBehaviour
{
    public Camera mainCamera;
    public Color tracerColor = Color.red;

    private CommandBuffer commandBuffer;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Создаём общий CommandBuffer
        commandBuffer = new CommandBuffer { name = "BulletTracers" };
        mainCamera.AddCommandBuffer(CameraEvent.AfterImageEffects, commandBuffer);
    }

    public void CreateTracer(Vector3 start, Vector3 end, float duration = 0.1f)
    {
        Material lineMaterial = new Material(Shader.Find("Custom/SmokeTracer"));
        Mesh lineMesh = CreateLineMesh(start, end);

        commandBuffer.DrawMesh(lineMesh, Matrix4x4.identity, lineMaterial);

        // Удаляем линию через время
        StartCoroutine(RemoveMeshAfterTime(lineMesh, duration));
    }

    private IEnumerator RemoveMeshAfterTime(Mesh lineMesh, float delay)
    {
        yield return new WaitForSeconds(delay);
        commandBuffer.Clear(); // Удаляет все команды
    }

    private Mesh CreateLineMesh(Vector3 start, Vector3 end)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[2] { start, end };
        int[] indices = new int[2] { 0, 1 };

        mesh.vertices = vertices;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);

        return mesh;
    }

    private void OnDestroy()
    {
        if (commandBuffer != null)
        {
            mainCamera.RemoveCommandBuffer(CameraEvent.AfterImageEffects, commandBuffer);
            commandBuffer.Release();
            commandBuffer = null;
        }
    }
}
