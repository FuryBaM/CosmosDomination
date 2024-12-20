#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;

[Flags]
enum MovementFlags
{
    LEFT = 1 << 0,
    RIGHT = 1 << 1,
    UP = 1 << 2,
    DOWN = 1 << 3
}

public class PlayerMovement : MonoBehaviour
{
    // Основные параметры движения
    public float xVel { get; private set; } = 0f;
    public float yVel { get; private set; } = 0f;
    public float playerRotation = 0f;
    public float tiltDirection = 0f;
    public float Height { get; private set; } = 0f;

    public float xVelSlide = 0f;

    public float xAcc = 2.1f * 0.6f;           // Ускорение
    public float xBrake = 1.7f * 0.6f;        // Торможение
    public float xCrouchBrake = 0.5f * 0.6f;  // Торможение в приседе
    public float xAirAcc = 1.5f * 0.6f;       // Ускорение в воздухе
    public float xAirBrake = 0.4f * 0.6f;     // Торможение в воздухе
    public float xMax = 11f;                  // Максимальная скорость (не зависит от времени)

    public float yGrav = 0.8f * 0.6f;         // Гравитация
    public float yMax = 20f;                  // Максимальная вертикальная скорость (не зависит от времени)
    public float yJump = 13f * 0.6f;          // Сила прыжка
    public float yJumpBoost = 6f * 0.6f;      // Дополнительная высота прыжка
    public float yDjump = 10f * 0.6f;         // Сила двойного прыжка
    private float maxTiltAngle = 30f;

    private float modSpeed = 1f;
    private float modMax = 0.75f;
    private float modBrake = 2f;
    private float modJump = 0.75f;
    private float modSideJump = 0f;
    private float modGrav = 0.6f;
    private float modSlide = 0.3f;
    private float modMove = 0f;
    private bool modMegaJump = false;

    private bool manualJump = false;
    public bool Jumping { get; private set; } = false;
    private bool crouching = false;
    private bool dJump = false;
    private float jumpDelay = 0.05f;
    private float lastJumpTime = 0f;

    private Rigidbody2D _rigidbody;

    [SerializeField] private Transform m_hand;
    public Transform Hand { get { return m_hand; } set { m_hand = value; } }

    public bool IsGrounded { get; private set; } = true;
    public bool IsDoubleJumped { get { return dJump; } }
    public bool IsDucking { get { return crouching; } }
    public Vector2 GroundTouch { get; private set; }
    public ContactPoint2D GroundContact { get; private set; }
    public float FacingDirection;

    private Player m_player;

    [SerializeField] private float groundCheckRadius = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    public LayerMask GroundLayer { get { return groundLayer; } set { groundLayer = value; } }

    public int keys = 0;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        m_player = GetComponent<Player>();
        // Отключаем стандартную гравитацию Unity, чтобы контролировать ее самостоятельно
        _rigidbody.gravityScale = 0f;
        Height = CalculateHeight();
    }
    private void FixedUpdate()
    {
        HandleTilting();
        // Обновляем состояние прыжка и двойного прыжка
        if (IsGrounded)
        {
            manualJump = false;
            Jumping = false;
            dJump = false;
            yVel = 0;
        }
        else
        {
            Jumping = true;
            // Применяем гравитацию
            yVel -= yGrav * modGrav;
            if (yVel > yMax * modGrav)
            {
                yVel = yMax * modGrav;
            }
        }
        Move();
    }

    public void ResetMods()
    {
        modMegaJump = false;
        modSpeed = 1f;
        modMax = 1f;
        modBrake = 1f;
        modJump = 1f;
        modSideJump = 0f;
        modGrav = 1f;
        modSlide = 0f;
        modMove = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Проверяем, что столкнулись с землей
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                // Вычисляем угол между нормалью и вектором "вверх"
                float angle = Vector2.Angle(contact.normal, Vector2.up);

                // Если угол наклона меньше или равен 60 градусов
                if (angle <= maxTiltAngle)
                {
                    IsGrounded = true;
                    GroundTouch = contact.point;
                    GroundContact = contact;
                    break;
                }
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Обновляем статус, чтобы учитывать постоянный контакт
        if (collision.gameObject.CompareTag("Ground"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                float angle = Vector2.Angle(contact.normal, Vector2.up);
                if (angle <= maxTiltAngle)
                {
                    IsGrounded = true;
                    GroundTouch = contact.point;
                    GroundContact = contact;
                    return;
                }
            }
            IsGrounded = false;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // При выходе из контакта с землей устанавливаем IsGrounded в false
        if (collision.gameObject.CompareTag("Ground"))
        {
            IsGrounded = false;
        }
    }

    private void Move()
    {
        float horizontalInput = 0;
        if ((keys & (int)MovementFlags.LEFT) != 0) horizontalInput = -1;
        if ((keys & (int)MovementFlags.RIGHT) != 0) horizontalInput = 1;
        if (IsDucking) horizontalInput = 0;
        if (m_player.IsDead)
        {
            horizontalInput = 0;
            crouching = false;
        }
        if (horizontalInput < 0 && !crouching && xVel > -(xMax + 3))
        {
            // Движение влево
            float acceleration = (Jumping ? -xAirAcc : -xAcc) * modSpeed;
            xVel += acceleration;
            if (xVel < -xMax * modMax)
            {
                xVel = -xMax * modMax;
            }
        }
        else if (horizontalInput > 0 && !crouching && xVel < xMax + 3)
        {
            // Движение вправо
            float acceleration = (Jumping ? xAirAcc : xAcc) * modSpeed;
            xVel += acceleration;
            if (xVel > xMax * modMax)
            {
                xVel = xMax * modMax;
            }
        }
        else
        {
            // Торможение
            float deceleration = (Jumping ? xAirBrake : (!crouching ? xBrake : xCrouchBrake)) * modBrake;
            if (!Jumping && (xVel > xMax || xVel < -xMax))
            {
                deceleration *= 2.5f;
            }
            if (xVel > deceleration)
            {
                xVel -= deceleration;
            }
            if (xVel < -deceleration)
            {
                xVel += deceleration;
            }
            if (xVel > -deceleration - 0.1f && xVel < deceleration + 0.1f)
            {
                xVel = 0;
            }
        }
        // Обновляем скорость Rigidbody2D
        if (modSlide == 0)
        {
            xVel += xVelSlide;
        }
        xVelSlide = Mathf.Round(playerRotation/90f) * modSlide * tiltDirection;
        if (xVelSlide > 0)
        {
            xVelSlide -= 0.05f;
        }
        if (xVelSlide < 0)
        {
            xVelSlide += 0.05f;
        }
        if (xVelSlide > -0.1f && xVelSlide < 0.1f)
        {
            xVelSlide = 0;
        }
        _rigidbody.linearVelocity = new Vector2(xVel + xVelSlide, yVel);
    }

    public void Jump()
    {
        if (IsDucking || m_player.IsDead)
        {
            return;
        }
        if (!Jumping)
        {
            // Первый прыжок
            transform.position = new Vector2(transform.position.x, transform.position.y + yJumpBoost * Time.fixedDeltaTime);
            yVel = yJump;
            Jumping = true;
            manualJump = true;
            IsGrounded = false;
            lastJumpTime = Time.time;
            // Здесь можно вызвать анимацию прыжка
        }
        else if (!dJump && Time.time - lastJumpTime > jumpDelay)
        {
            // Двойной прыжок
            yVel *= 0.5f;
            yVel += yDjump;
            dJump = true;
            // Здесь можно вызвать анимацию двойного прыжка
        }
    }

    public void RotateHandTowards(float angle)
    {
        if (m_player.IsDead) return;
        // Определяем, нужно ли игроку смотреть влево
        bool isFacingLeft = angle + playerRotation > 90 || angle + playerRotation < -90;
        FacingDirection = isFacingLeft ? -1 : 1;

        // Изменяем масштаб игрока для отзеркаливания
        Vector3 playerScale = transform.localScale;
        if (isFacingLeft)
        {
            playerScale.x = -Mathf.Abs(playerScale.x);
            angle += 180f; // Корректируем угол при повороте налево
        }
        else
        {
            playerScale.x = Mathf.Abs(playerScale.x);
        }
        transform.localScale = playerScale;

        // Поворачиваем руку по переданному углу
        m_hand.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void HandleTilting()
    {
        Vector2 normal = GroundContact.normal;
        Vector2 surface = -Vector2.Perpendicular(normal);
        playerRotation = Mathf.Abs(Mathf.Atan2(surface.y, surface.x) * Mathf.Rad2Deg);
        tiltDirection = normal.x > 0f ? 1 : -1;
        if (playerRotation > 180)
        {
            playerRotation -= 180;
        }
        if (IsGrounded == true && playerRotation < maxTiltAngle)
        {
            transform.up = Vector3.Lerp(transform.up, normal, Time.deltaTime * 7.5f);
        }
        else
        {
            transform.up = Vector3.Lerp(transform.up, Vector2.up, Time.deltaTime * 7.5f);
        }
    }

    public void Duck(bool isDuck)
    {
        if (m_player.IsDead)
        {
            crouching = false;
            return;
        }
        crouching = isDuck;
        // Здесь можно вызвать анимацию приседания
    }
    public void ReleaseKeys() 
    {
        if ((keys & (int)MovementFlags.DOWN) != 0)
        {
            keys ^= (int)MovementFlags.DOWN;
        }
        if ((keys & (int)MovementFlags.LEFT) != 0)
        {
            keys ^= (int)MovementFlags.LEFT;
        }
        if ((keys & (int)MovementFlags.RIGHT) != 0)
        {
            keys ^= (int)MovementFlags.RIGHT;
        }
    }
    public float CalculateHeight()
    {
        float maxY = float.MinValue; // Самая верхняя точка
        float minY = float.MaxValue; // Самая нижняя точка

        // Получаем все SpriteRenderer объекта и его дочерних объектов
        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>();

        foreach (var spriteRenderer in spriteRenderers)
        {
            // Границы спрайта в мировых координатах
            Bounds spriteBounds = spriteRenderer.bounds;

            // Обновляем минимальную и максимальную высоту
            minY = Mathf.Min(minY, spriteBounds.min.y);
            maxY = Mathf.Max(maxY, spriteBounds.max.y);
        }

        // Высота персонажа
        float height = maxY - minY;
        return height;
    }
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (IsGrounded)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GroundTouch, 0.1f);
        }
        Handles.BeginGUI();
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.green;
        style.fontSize = 7;
        style.alignment = TextAnchor.MiddleCenter;

        Vector3 position = transform.position + Vector3.down * 2; // Над головой игрока
        UnityEditor.Handles.Label(position, $"IsGrounded: {IsGrounded}\nDoubleJump: {dJump}\nDucking: {IsDucking}\nrot: {playerRotation}", style);
    }
#endif
}