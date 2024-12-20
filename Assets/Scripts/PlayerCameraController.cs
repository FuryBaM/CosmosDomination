using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public Transform cameraTransform; // Ссылка на камеру
    public Vector3 offset; // Смещение камеры относительно игрока
    public float speed = 5f; // Фактор влияния курсора на позицию камеры

    private Camera _camera; // Ссылка на компонент Camera

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Получаем ссылку на компонент Camera
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
