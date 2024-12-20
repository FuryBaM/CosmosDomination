using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Water : MonoBehaviour
{
    private struct Dot
    {
        public float x;
        public float y;
        public float oy; // Original position
        public float toY; // Target position
        public float yVel; // Vertical velocity
        public float yAcc; // Vertical acceleration
        public float mass; // Mass
    }

    public int segments = 50; // Number of segments
    public float width = 10f; // Width of the water
    public float height = 1f; // Initial height of the water
    public Material waterMaterial;

    public GameObject[] units; // Array of units interacting with water

    private Dot[] dots;
    private Mesh waterMesh;
    private Vector3[] vertices;
    private int[] triangles;

    private float waterTimer = 0.02f; // Timer for wave generation, adjusted for 50 FPS

    private void Start()
    {
        InitializeWater();
        SetMaterialProperties(); // Set shader properties at start
    }

    private void FixedUpdate()
    {
        if (waterTimer <= 0)
        {
            // Create waves
            StartWave(0, 2f / 10f);
            StartWave(segments - 1, 2f / 10f);
            StartWave(segments / 2, 1f / 10f);
            waterTimer = 0.02f * 50 * 1; // Reset timer (for 50 FPS)
        }
        waterTimer -= Time.fixedDeltaTime;

        // Interact with units
        foreach (GameObject unit in units)
        {
            if (unit == null) continue;
            Vector2 unitPosition = unit.transform.position;

            if (InWater(unitPosition))
            {
                int index = Mathf.RoundToInt((unitPosition.x - transform.position.x) / (width / (segments - 1)));

                // Assuming units have a UnitMovement component
                PlayerMovement unitMovement = unit.GetComponent<PlayerMovement>();
                Player player = unitMovement.GetComponent<Player>();
                if (unitMovement != null)
                {
                    if (!player.IsDead)
                    {
                        var playerRB = unitMovement.GetComponent<Rigidbody2D>();
                        if (!(!unitMovement.IsGrounded && playerRB.linearVelocity.y > 0))
                        {
                            if (!unitMovement.IsGrounded && playerRB.linearVelocity.y < 0)
                            {
                                StartWave(index, -5f / 10f);
                            }
                            else if (playerRB.linearVelocity.y < 0)
                            {
                                StartWave(index - 1, 2f / 10f);
                                StartWave(index - 3, -2f / 10f);
                                StartWave(index + 1, -1f / 10f);
                            }
                            else if (playerRB.linearVelocity.x <= 0)
                            {
                                if (unitMovement.IsDucking)
                                {
                                    StartWave(index, 0.5f / 10f);
                                }
                            }
                            else
                            {
                                StartWave(index + 1, 2f / 10f);
                                StartWave(index + 3, -2f / 10f);
                                StartWave(index - 1, -1f / 10f);
                            }
                        }
                        else
                        {
                            StartWave(index, 4f / 10f);
                        }
                    }
                }
            }
        }

        UpdatePhysics();
        UpdateMesh();
    }

    private void InitializeWater()
    {
        dots = new Dot[segments];
        for (int i = 0; i < segments; i++)
        {
            float xPos = i * (width / (segments - 1));
            float yPos = height;

            dots[i] = new Dot
            {
                x = xPos,
                y = yPos,
                oy = yPos,
                toY = yPos,
                yVel = 0,
                yAcc = 0,
                mass = 10
            };
        }

        // Create the mesh
        waterMesh = new Mesh();
        vertices = new Vector3[segments * 2];
        triangles = new int[(segments - 1) * 6];

        for (int i = 0; i < segments - 1; i++)
        {
            int index = i * 6;
            int vi = i * 2;

            triangles[index] = vi;
            triangles[index + 1] = vi + 1;
            triangles[index + 2] = vi + 2;

            triangles[index + 3] = vi + 1;
            triangles[index + 4] = vi + 3;
            triangles[index + 5] = vi + 2;
        }

        GetComponent<MeshFilter>().mesh = waterMesh;
        GetComponent<MeshRenderer>().material = waterMaterial;

        UpdateMesh();
    }

    private void SetMaterialProperties()
    {
        // Set the color of the water
        waterMaterial.SetColor("_WaterColor", new Color(0, 0.5f, 1, 0.5f)); // Blue color with some transparency
    }

    private void UpdatePhysics()
    {
        float spread = 0.7f;
        float damping = 1.05f;
        float tension = 0.7f;

        for (int i = 0; i < dots.Length; i++)
        {
            float acc = 0f;

            if (i > 0)
            {
                float diff = dots[i - 1].y - dots[i].y;
                acc += diff * -spread;
            }

            if (i < dots.Length - 1)
            {
                float diff = dots[i].y - dots[i + 1].y;
                acc += diff * spread;
            }

            float diffOriginal = dots[i].y - dots[i].oy;
            acc += tension * diffOriginal;

            dots[i].yAcc = -acc / dots[i].mass;
            dots[i].yVel += dots[i].yAcc;
            dots[i].toY += dots[i].yVel;
            dots[i].yVel /= damping;
        }

        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].y = dots[i].toY;
        }
    }

    private void UpdateMesh()
    {
        // Update the vertices of the mesh
        for (int i = 0; i < dots.Length; i++)
        {
            vertices[i * 2] = new Vector3(transform.position.x + dots[i].x, transform.position.y + dots[i].y, 0);
            vertices[i * 2 + 1] = new Vector3(transform.position.x + dots[i].x, transform.position.y, 0); // Bottom point of water
        }

        waterMesh.Clear();
        waterMesh.vertices = vertices;
        waterMesh.triangles = triangles;
        waterMesh.RecalculateNormals();

        // Pass data to shader
        waterMaterial.SetFloat("_Width", width);
        waterMaterial.SetFloat("_Height", height);
        waterMaterial.SetFloat("_Segments", segments);
        waterMaterial.SetFloat("_WaterTimer", waterTimer);
    }

    public void StartWave(int index, float velocity)
    {
        if (index >= 0 && index < dots.Length)
        {
            dots[index].yVel = velocity;
        }
    }

    public void StartWaveAtPosition(float position, float velocity)
    {
        int index = Mathf.RoundToInt(position / (width / (segments - 1)));
        StartWave(index, velocity);
    }

    private bool InWater(Vector2 position)
    {
        float left = transform.position.x - (width / 2f); // Center object correctly
        float right = transform.position.x + (width / 2f);
        float bottom = transform.position.y;
        float top = transform.position.y + height;

        return position.x >= left && position.x <= right && position.y >= bottom && position.y <= top;
    }

    private void OnDrawGizmos()
    {
        if (dots == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + new Vector3(width / 2f, height / 2f, 0), new Vector3(width, height, 0));
        Gizmos.color = Color.blue;
        foreach (var dot in dots)
        {
            Gizmos.DrawSphere(transform.position + new Vector3(dot.x, dot.y, 0), 0.1f);
        }
    }
}
