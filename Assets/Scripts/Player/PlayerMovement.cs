using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float jumpPower;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    [Header("Rotation / Visual")]
    [Tooltip("Sadece görseli döndürmek için Sprite/Graphics child'ını buraya atayın. Boşsa bu GameObject döner.")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float alignToGravityDegreesPerSecond = 720f;

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;
    private float wallJumpCooldown;
    private float horizontalInput;
    private float rawHorizontalInput;
    private Vector3 defaultWorldScale;

    private float speedMultiplier = 1f;
    private float jumpMultiplier = 1f;
    
    // Time slow kompansasyonu için
    // Tam kompansasyon (1/timeScale) çok agresif olur çünkü fizik de yavaşlıyor
    // Sqrt kullanarak daha dengeli bir his elde ediyoruz
    private float TimeCompensation
    {
        get
        {
            if (TimeSlowAbility.Instance == null || !TimeSlowAbility.Instance.IsSlowMotionActive)
                return 1f;
            // Örn: timeScale=0.3 → raw=3.33 → sqrt≈1.82
            float raw = TimeSlowAbility.Instance.PlayerTimeCompensation;
            return Mathf.Sqrt(raw);
        }
    }
    
    // Zıplama için daha az kompansasyon (yerçekimi de yavaşladığından)
    private float JumpTimeCompensation
    {
        get
        {
            if (TimeSlowAbility.Instance == null || !TimeSlowAbility.Instance.IsSlowMotionActive)
                return 1f;
            // Zıplama için çok az kompansasyon yeterli (1.2-1.3x)
            float raw = TimeSlowAbility.Instance.PlayerTimeCompensation;
            return 1f + (raw - 1f) * 0.1f; // %10'u kadar kompansasyon
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        defaultWorldScale = transform.lossyScale;

        // Fizik rotasyonunu kilitle, görsel rotasyonu biz yöneteceğiz.
        body.freezeRotation = true;

        if (visualRoot == null)
            visualRoot = transform;
    }

    private void Update()
    {
        bool isRotatingWorld = WorldRotationManager.Instance != null && WorldRotationManager.Instance.IsRotating;

        // Dönüş sırasında hareketi kilitle ama görsel hizalamayı sürdür.
        horizontalInput = isRotatingWorld ? 0f : Input.GetAxis("Horizontal");
        rawHorizontalInput = isRotatingWorld ? 0f : Input.GetAxisRaw("Horizontal");

        // Yerçekimine göre yön vektörleri
        Vector2 gravityDir = Physics2D.gravity.normalized;
        Vector2 rightDir = new Vector2(-gravityDir.y, gravityDir.x); // Yerçekimine dik = "sağ"

        // Karakteri yerçekimine göre döndür (ayakta kalsın)
        AlignVisualToGravity(gravityDir);

        // Flip (yerçekimi yönüne göre)
        if (rawHorizontalInput > 0.01f)
            ApplyFacingScale(1f);
        else if (rawHorizontalInput < -0.01f)
            ApplyFacingScale(-1f);

        bool grounded = isGrounded();
        bool onMovingPlatform = IsOnMovingPlatform();
        bool touchingWall = onWall();

        // Animator parameters
        bool groundedForAnim = grounded || onMovingPlatform;
        anim.SetBool("Run", Mathf.Abs(rawHorizontalInput) > 0.01f && groundedForAnim);
        anim.SetBool("grounded", groundedForAnim);

        if (wallJumpCooldown > 0.2f)
        {
            if (isRotatingWorld)
                return;

            // Movement - yerçekimine göre hareket
            // Time slow aktifken oyuncu normal hızda kalmak için kompansasyon uygula
            float effectiveSpeed = speed * speedMultiplier * TimeCompensation;
            Vector2 moveVelocity = rightDir * horizontalInput * effectiveSpeed;
            
            // Yerçekimi yönündeki hızı koru (düşme/yükselme)
            float fallSpeed = Vector2.Dot(body.linearVelocity, gravityDir);
            Vector2 fallVelocity = gravityDir * fallSpeed;
            
            body.linearVelocity = moveVelocity + fallVelocity;

            // Wall slide
            if (touchingWall && !grounded && horizontalInput != 0)
            {
                body.gravityScale = 1.5f;

                float currentFallSpeed = Vector2.Dot(body.linearVelocity, gravityDir);
                if (currentFallSpeed > 2f) // Yerçekimi yönünde çok hızlı düşüyorsa
                {
                    Vector2 clampedFall = gravityDir * 2f;
                    body.linearVelocity = moveVelocity + clampedFall;
                }
            }
            else
            {
                body.gravityScale = 7f;
            }

            // Jump
            if (Input.GetKeyDown(KeyCode.Space))
                Jump(touchingWall, grounded || onMovingPlatform);
        }
        else
        {
            // Time slow aktifken cooldown normal hızda azalsın
            wallJumpCooldown += Time.deltaTime * TimeCompensation;
        }
    }

    private void Jump(bool touchingWall, bool grounded)
    {
        Vector2 gravityDir = Physics2D.gravity.normalized;
        Vector2 upDir = -gravityDir; // Yerçekiminin tersi = "yukarı"
        Vector2 rightDir = new Vector2(-gravityDir.y, gravityDir.x);

        // Zıplama için minimal kompansasyon (yerçekimi de yavaşladığından)
        float effectiveJumpPower = jumpPower * jumpMultiplier * JumpTimeCompensation;

        if (grounded)
        {
            // Mevcut yatay hızı koru, yukarı doğru zıpla
            float horizontalSpeed = Vector2.Dot(body.linearVelocity, rightDir);
            body.linearVelocity = rightDir * horizontalSpeed + upDir * effectiveJumpPower;
            anim.SetTrigger("jump");
        }
        else if (touchingWall && !grounded)
        {
            float direction = -Mathf.Sign(transform.localScale.x);
            // Wall jump: yatay hareket kompansasyonu, dikey minimal
            body.linearVelocity = rightDir * direction * 6f * TimeCompensation + upDir * effectiveJumpPower;

            ApplyFacingScale(direction);
            wallJumpCooldown = 0;
        }
    }

    private void ApplyFacingScale(float direction)
    {
        Vector3 parentScale = transform.parent ? transform.parent.lossyScale : Vector3.one;

        float safeX = Mathf.Abs(parentScale.x) < 0.0001f ? 1f : parentScale.x;
        float safeY = Mathf.Abs(parentScale.y) < 0.0001f ? 1f : parentScale.y;
        float safeZ = Mathf.Abs(parentScale.z) < 0.0001f ? 1f : parentScale.z;

        float targetX = Mathf.Abs(defaultWorldScale.x) * Mathf.Sign(direction == 0 ? 1f : direction);

        transform.localScale = new Vector3(
            targetX / safeX,
            defaultWorldScale.y / safeY,
            defaultWorldScale.z / safeZ
        );
    }

    private void AlignVisualToGravity(Vector2 gravityDir)
    {
        if (visualRoot == null)
            return;

        // "Up" yönü yerçekiminin tersi
        Vector2 upDir = -gravityDir;
        if (upDir.sqrMagnitude < 0.0001f)
            return;

        // upDir = (0,1) iken açı 0 olmalı
        float targetAngle = Mathf.Atan2(upDir.y, upDir.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);

        // Time slow aktifken rotasyon hızı da kompanse edilmeli
        float maxStep = alignToGravityDegreesPerSecond <= 0f
            ? 99999f
            : alignToGravityDegreesPerSecond * Time.deltaTime * TimeCompensation;

        visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation, targetRot, maxStep);
    }

    private bool isGrounded()
    {
        // Yerçekimi yönüne göre zemin kontrolü
        Vector2 gravityDir = Physics2D.gravity.normalized;
        
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            gravityDir,  // Yerçekimi yönünde kontrol
            0.1f,
            groundLayer
        );
        return hit.collider != null;
    }

    private bool onWall()
    {
        // Baktığı yöne göre duvar kontrolü (yerçekimine dik)
        Vector2 gravityDir = Physics2D.gravity.normalized;
        Vector2 rightDir = new Vector2(-gravityDir.y, gravityDir.x);
        Vector2 checkDir = rightDir * Mathf.Sign(transform.localScale.x);
        
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            checkDir,
            0.1f,
            wallLayer
        );
        return hit.collider != null;
    }

    private bool IsOnMovingPlatform()
    {
        if (transform.parent == null)
            return false;

        return transform.parent.GetComponent<MovingPlatform>() != null;
    }

    // SHOOTING CONDITION — saldırabilir mi?
    public bool canAttack()
    {
        // Duvar tutuşu dahil her durumda saldırabilir
        return true;
    }

    public void SetMovementMultipliers(float newSpeedMultiplier, float newJumpMultiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, newSpeedMultiplier);
        jumpMultiplier = Mathf.Max(0.1f, newJumpMultiplier);
    }
}
