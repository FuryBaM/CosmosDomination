using UnityEngine;
using UnityEngine.XR;

public class KeyboardInput : MonoBehaviour
{
    private PlayerMovement _playerMovement;

    private void Start()
    {
        _playerMovement = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        // ��������� ������� ���� � ������� �����������
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0f;

        // ������������ ����������� �� ���� � ����
        Vector2 direction = (mousePosition - _playerMovement.Hand.position).normalized;

        // ��������� ���� ��������
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        _playerMovement.RotateHandTowards(angle);
        if (horizontal > 0f)
        {
            _playerMovement.keys |= (int)MovementFlags.RIGHT;
            _playerMovement.keys &= ~(int)MovementFlags.LEFT;
        }
        else if (horizontal < 0f)
        {
            _playerMovement.keys |= (int)MovementFlags.LEFT;
            _playerMovement.keys &= ~(int)MovementFlags.RIGHT;
        }
        else
        {
            _playerMovement.keys &= ~(int)MovementFlags.LEFT;
            _playerMovement.keys &= ~(int)MovementFlags.RIGHT;
        }

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.W))
        {
            _playerMovement.Jump();
        }
        if (Input.GetKey(KeyCode.S))
        {
            _playerMovement.Duck(true);
        }
        if (Input.GetKeyUp(KeyCode.S))
        {
            _playerMovement.Duck(false);
        }
    }
}
