using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class SimpleCreatureMover : MonoBehaviour
{
    [SerializeField] private float m_FollowTurnSharpness = 10f;
    [SerializeField] private Transform m_Camera;
    [SerializeField] private CameraManager m_CameraManager;
    [SerializeField] private Animator m_Animator;

    [Header("Movement")]
    [SerializeField] private float m_WalkSpeed = 2f;
    [SerializeField] private float m_RunSpeed = 5f;
    [SerializeField] private float m_MouseSensitivity = 3f;
    [Header("Dash")]
    [SerializeField] private KeyCode m_DashKey = KeyCode.Q;
    [SerializeField] private float m_DashSpeed = 12f;     // burst strength
    [SerializeField] private float m_DashDecay = 6f;      // how fast it fades
    [SerializeField] private float m_DashCooldown = 0.6f;

    [Header("Inertia")]
    [SerializeField] private float m_Acceleration = 40f;       // normal: snappy
    [SerializeField] private float m_SandeAcceleration = 4f;   // sande: slidey

    [Header("Sandevistan feel")]
    [SerializeField] private float m_SandeGravityScale = 0.5f;
    [SerializeField] private float m_SandeDashDecay = 2f;
    [SerializeField] private float m_Grip = 40f;     
    [SerializeField] private float m_SandeGrip = 1f;    

    [SerializeField, Range(0f, 1440f)] private float m_RotateSpeed = 480f;
    [SerializeField] private float m_FixedTurnSharpness = 8f;

    [Header("Jump and gravity")]
    [SerializeField] private float m_JumpHeight = 1.2f;
    [SerializeField] private float m_Gravity = -25f;

    [SerializeField] private float m_GroundedStick = -2f;

    [Header("Animator parameters")]
    [SerializeField] private string m_SpeedParam = "Vert";
    [SerializeField] private string m_StateParam = "State";

    [SerializeField] private float m_AnimSmoothing = 8f;

    [SerializeField] private string m_HorizontalAxis = "Horizontal";
    [SerializeField] private string m_VerticalAxis = "Vertical";
    [SerializeField] private string m_JumpButton = "Jump";
    [SerializeField] private KeyCode m_RunKey = KeyCode.LeftShift;

    private DeerShooter m_DeerShooter;
    private CharacterController m_Controller;
    private int m_SpeedHash;
    private int m_StateHash;
    private float m_Yaw;
    private bool m_DashPressed;
    private Vector3 m_DashVelocity;
    private float m_LastDashTime = -999f;
    private Vector2 m_Input;
    private bool m_RunHeld;
    private bool m_JumpPressed;
    private float m_VerticalVelocity;
    private float m_AnimSpeed;
    private float m_AnimState;
    private SandevistanController m_Sande;
    private Vector3 m_MoveVelocity;
    public Vector2 Input2D => m_Input;
    public bool IsRunning => m_RunHeld;
    public bool IsGrounded => m_Controller != null && m_Controller.isGrounded;


    private void Awake()
    {
        m_Sande = GetComponent<SandevistanController>();
        m_Controller = GetComponent<CharacterController>();
        m_DeerShooter = GetComponent<DeerShooter>();

        if (m_Animator == null)
            m_Animator = GetComponent<Animator>();

        if (m_Camera == null && Camera.main != null)
            m_Camera = Camera.main.transform;

        if (m_Camera == null)
            Debug.LogWarning($"[{nameof(SimpleCreatureMover)}] No camera assigned and no MainCamera found. " +
                             "Movement will use world axes.", this);

        m_SpeedHash = Animator.StringToHash(m_SpeedParam);
        m_StateHash = Animator.StringToHash(m_StateParam);
        m_Yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        bool sande = m_Sande.SandyOn;
        float grip = sande ? m_SandeGrip : m_Grip;
        float accel = sande ? m_SandeAcceleration : m_Acceleration;
        float gravity = m_Gravity * (sande ? m_SandeGravityScale : 1f);
        float dashDecay = sande ? m_SandeDashDecay : m_DashDecay;
        float dt = Time.deltaTime* m_Sande.PlayerTimeMultiplier;

        ReadInput();
        bool following = m_CameraManager != null && m_CameraManager.IsFollowing;
        Cursor.lockState = following ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !following;

        Vector3 moveDir;
        if (following)
        {
            m_Yaw += Input.GetAxis("Mouse X") * m_MouseSensitivity;
            Quaternion target = Quaternion.Euler(0f, m_Yaw, 0f);
            if (m_FollowTurnSharpness > 0f)
            {
                float t = 1f - Mathf.Exp(-m_FollowTurnSharpness * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
            }
            else
            {
                transform.rotation = target;
            }
            moveDir = transform.rotation * new Vector3(m_Input.x, 0f, m_Input.y);
        }
        else
        {
            moveDir = CameraRelativeDirection(m_Input);
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(moveDir, Vector3.up);
                float turnT = 1f - Mathf.Exp(-m_FixedTurnSharpness * dt);
                transform.rotation = Quaternion.Slerp(transform.rotation, desired, turnT);

                moveDir = transform.forward * moveDir.magnitude;
            }
            m_Yaw = transform.eulerAngles.y;
        }

        

        if (m_Controller.isGrounded)
        {
            if (m_VerticalVelocity < 0f)
                m_VerticalVelocity = m_GroundedStick;

            if (m_JumpPressed)
                m_VerticalVelocity = Mathf.Sqrt(m_JumpHeight * -2f * m_Gravity);
        }
        else
        {
            m_VerticalVelocity += gravity * dt;
        }
        if (m_DashPressed && Time.unscaledTime - m_LastDashTime >= m_DashCooldown)
        {
            Vector3 dashDir = moveDir.sqrMagnitude > 0.0001f ? moveDir : transform.forward;
            dashDir.y = 0f;
            m_DashVelocity = dashDir.normalized * m_DashSpeed;
            m_LastDashTime = Time.unscaledTime;
        }


        m_DashVelocity = Vector3.Lerp(m_DashVelocity, Vector3.zero, 1f - Mathf.Exp(-dashDecay * dt));

        float inputAmount = moveDir.magnitude;

        float speed = m_RunHeld ? m_RunSpeed : m_WalkSpeed;

        
        Vector3 targetVel = moveDir * speed;
        float accelT = 1f - Mathf.Exp(-accel * dt);
        float gripT = 1f - Mathf.Exp(-grip * dt);

        if (targetVel.sqrMagnitude > 0.0001f)
        {
            Vector3 steerDir = targetVel.normalized;
            Vector3 along = Vector3.Project(m_MoveVelocity, steerDir);   
            Vector3 side = m_MoveVelocity - along;                      

            along = Vector3.Lerp(along, targetVel, accelT);
            side = Vector3.Lerp(side, Vector3.zero, gripT);

            m_MoveVelocity = along + side;
        }
        else
        {
            
            m_MoveVelocity = Vector3.Lerp(m_MoveVelocity, Vector3.zero, gripT);
        }

        Vector3 velocity = m_DeerShooter.IsAiming ? m_MoveVelocity : m_MoveVelocity + m_DashVelocity;

        velocity.y = m_VerticalVelocity;

        m_Controller.Move(velocity * dt);

        UpdateAnimator(inputAmount, dt);
    }

    private void ReadInput()
    {
        m_Input = new Vector2(Input.GetAxisRaw(m_HorizontalAxis), Input.GetAxisRaw(m_VerticalAxis));
        m_Input = Vector2.ClampMagnitude(m_Input, 1f);
        m_DashPressed = Input.GetKeyDown(m_DashKey);
        m_RunHeld = Input.GetKey(m_RunKey);
        m_JumpPressed = Input.GetButtonDown(m_JumpButton);
    }

    
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
        m_Animator.speed = m_Sande.PlayerTimeMultiplier;
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
