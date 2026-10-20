using UnityEngine;

/// <summary>
/// Self-contained character mover. Reads input, moves relative to a camera,
/// and turns toward the direction of travel. Replaces CreatureMover +
/// MovePlayerInput together -- remove both before adding this.
///
/// Design note: there is deliberately no stored "last heading". While input is
/// held the character turns toward the current input direction; when input
/// stops it simply stops turning and holds whatever angle it had. That is why
/// it can settle on any angle, not just the four cardinals.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class SimpleCreatureMover : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera that movement is relative to. Falls back to Camera.main.")]
    [SerializeField] private Transform m_Camera;
    [Tooltip("Optional. Falls back to an Animator on this object.")]
    [SerializeField] private Animator m_Animator;

    [Header("Movement")]
    [SerializeField] private float m_WalkSpeed = 2f;
    [SerializeField] private float m_RunSpeed = 5f;
    [Tooltip("Degrees per second the character turns toward its direction of travel.")]
    [SerializeField, Range(0f, 1440f)] private float m_RotateSpeed = 480f;

    [Header("Jump and gravity")]
    [SerializeField] private float m_JumpHeight = 1.2f;
    [SerializeField] private float m_Gravity = -25f;
    [Tooltip("Small downward push while grounded so isGrounded stays reliable.")]
    [SerializeField] private float m_GroundedStick = -2f;

    [Header("Animator parameters")]
    [SerializeField] private string m_SpeedParam = "Vert";
    [SerializeField] private string m_StateParam = "State";
    [Tooltip("Higher blends animation values faster. 0 disables smoothing.")]
    [SerializeField] private float m_AnimSmoothing = 8f;

    [Header("Input (legacy Input Manager only)")]
    [SerializeField] private string m_HorizontalAxis = "Horizontal";
    [SerializeField] private string m_VerticalAxis = "Vertical";
    [SerializeField] private string m_JumpButton = "Jump";
    [SerializeField] private KeyCode m_RunKey = KeyCode.LeftShift;

    private CharacterController m_Controller;
    private int m_SpeedHash;
    private int m_StateHash;

    private Vector2 m_Input;
    private bool m_RunHeld;
    private bool m_JumpPressed;

    private float m_VerticalVelocity;
    private float m_AnimSpeed;
    private float m_AnimState;

    // Read-only accessors in case other scripts want them.
    public Vector2 Input2D => m_Input;
    public bool IsRunning => m_RunHeld;
    public bool IsGrounded => m_Controller != null && m_Controller.isGrounded;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();

        if (m_Animator == null)
            m_Animator = GetComponent<Animator>();

        if (m_Camera == null && Camera.main != null)
            m_Camera = Camera.main.transform;

        if (m_Camera == null)
            Debug.LogWarning($"[{nameof(SimpleCreatureMover)}] No camera assigned and no MainCamera found. " +
                             "Movement will use world axes.", this);

        m_SpeedHash = Animator.StringToHash(m_SpeedParam);
        m_StateHash = Animator.StringToHash(m_StateParam);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        ReadInput();

        Vector3 moveDir = CameraRelativeDirection(m_Input);
        float inputAmount = moveDir.magnitude;

        // --- Rotation ---
        // Only turn while there is meaningful input. No input means no target,
        // so the character holds its current angle instead of snapping anywhere.
        if (inputAmount > 0.01f)
        {
            Quaternion desired = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, desired, m_RotateSpeed * dt);
        }

        // --- Gravity and jump ---
        if (m_Controller.isGrounded)
        {
            if (m_VerticalVelocity < 0f)
                m_VerticalVelocity = m_GroundedStick;

            if (m_JumpPressed)
                m_VerticalVelocity = Mathf.Sqrt(m_JumpHeight * -2f * m_Gravity);
        }
        else
        {
            m_VerticalVelocity += m_Gravity * dt;
        }

        // --- Displacement ---
        float speed = m_RunHeld ? m_RunSpeed : m_WalkSpeed;
        Vector3 velocity = moveDir * speed;
        velocity.y = m_VerticalVelocity;

        m_Controller.Move(velocity * dt);

        // --- Animation ---
        UpdateAnimator(inputAmount, dt);
    }

    private void ReadInput()
    {
        m_Input = new Vector2(Input.GetAxisRaw(m_HorizontalAxis), Input.GetAxisRaw(m_VerticalAxis));
        // Keep diagonals from being faster than straight lines.
        m_Input = Vector2.ClampMagnitude(m_Input, 1f);

        m_RunHeld = Input.GetKey(m_RunKey);
        m_JumpPressed = Input.GetButtonDown(m_JumpButton);
    }

    /// <summary>
    /// Converts 2D input into a world direction on the horizontal plane,
    /// using the camera's yaw as the frame of reference.
    /// Read fresh every frame, so it works in fixed, tracking and follow modes.
    /// </summary>
    private Vector3 CameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude < 0.0001f)
            return Vector3.zero;

        Vector3 forward;

        if (m_Camera == null)
        {
            forward = Vector3.forward;
        }
        else
        {
            forward = m_Camera.forward;
            forward.y = 0f;

            // Camera aimed almost straight up or down: its forward is useless,
            // so fall back to its up vector, which is horizontal in that case.
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = m_Camera.up;
                forward.y = 0f;
            }
        }

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 dir = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(dir, 1f);
    }

    private void UpdateAnimator(float inputAmount, float dt)
    {
        if (m_Animator == null) return;

        float targetSpeed = inputAmount * (m_RunHeld ? 1f : 0.5f);
        float targetState = m_Controller.isGrounded ? 0f : 1f;

        if (m_AnimSmoothing > 0f)
        {
            float t = 1f - Mathf.Exp(-m_AnimSmoothing * dt);
            m_AnimSpeed = Mathf.Lerp(m_AnimSpeed, targetSpeed, t);
            m_AnimState = Mathf.Lerp(m_AnimState, targetState, t);
        }
        else
        {
            m_AnimSpeed = targetSpeed;
            m_AnimState = targetState;
        }

        m_Animator.SetFloat(m_SpeedHash, m_AnimSpeed);
        m_Animator.SetFloat(m_StateHash, m_AnimState);
    }
}
