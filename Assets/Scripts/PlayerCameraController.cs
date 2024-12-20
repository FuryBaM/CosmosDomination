using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform cameraTransform; // ������ �� ������
    public Vector3 offset; // �������� ������ ������������ ������
    public float speed = 5f; // ������ ������� ������� �� ������� ������

    private Camera _camera; // ������ �� ��������� Camera

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // �������� ������ �� ��������� Camera
        _camera = cameraTransform.GetComponent<Camera>();
    }
    private void LateUpdate()
    {
        if (_camera == null) return;
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        offset = (mousePosition - _camera.transform.position) / 4f;
        Vector3 target = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, -10);
        cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, speed*Time.deltaTime);
    }
}
