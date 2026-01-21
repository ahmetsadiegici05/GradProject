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
    private bool wasGrounded;

    // Earthquake slide
    private float earthquakeSlideSpeed = 0f;
    private float jumpSlideResetTimer = 0f;
    private const float JUMP_SLIDE_RESET_DURATION = 0.5f;
    
    // Visual Shake
    private Vector3 initialVisualLocalPos;

    // Step Sound
    private float stepTimer;
    [SerializeField] private float stepInterval = 0.35f;

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

        // Stage ilerledikçe hız artınca bazen ince collider'lardan "tünelleme" yaşanabiliyor.
        // Continuous çarpışma + interpolation bunu belirgin şekilde azaltır.
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (visualRoot == null)
            visualRoot = transform;
            
        // Görselin orijinal pozisyonunu kaydet (titreme için)
        if (visualRoot != transform)
            initialVisualLocalPos = visualRoot.localPosition;
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

        // Karakteri yerçekimine göre döndür (Collider hizalansın)
        AlignPlayerToGravity(gravityDir);

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

        // Landing Sound
        if (groundedForAnim && !wasGrounded)
        {
            if (SoundManager.instance != null)
                SoundManager.instance.PlaySound(SoundManager.instance.landSound);
        }
        wasGrounded = groundedForAnim;

        if (wallJumpCooldown > 0.2f)
        {
            if (isRotatingWorld)
                return;

            // Deprem aktifse oyuncu hareketi azalt (kaymaya direnemesin)
            bool earthquakeActive = EarthquakeManager.Instance != null && EarthquakeManager.Instance.IsEarthquakeActive;
            
            // Havada tam kontrol, yerde kısıtlı kontrol (%30)
            float earthquakeMovementPenalty = (earthquakeActive && (grounded || onMovingPlatform)) ? 0.3f : 1f;

            // Movement - yerçekimine göre hareket
            // Time slow aktifken oyuncu normal hızda kalmak için kompansasyon uygula
            float effectiveSpeed = speed * speedMultiplier * TimeCompensation * earthquakeMovementPenalty;
            Vector2 moveVelocity = rightDir * horizontalInput * effectiveSpeed;
            
            // Yerçekimi yönündeki hızı koru (düşme/yükselme)
            float fallSpeed = Vector2.Dot(body.linearVelocity, gravityDir);
            Vector2 fallVelocity = gravityDir * fallSpeed;
            
            // Deprem kayma etkisi
            if (earthquakeActive)
            {
                float slideAmount = GetEarthquakeSlideAmount(grounded || onMovingPlatform);
                
                // Eğer havadaysak veya slide hızı sıfırsa normal fizik uygula (Kontrol kaybı yok)
                if (slideAmount <= 0.01f)
                {
                    body.linearVelocity = moveVelocity + fallVelocity;
                }
                else
                {
                    // Jitter
                    float jitterIntensity = slideAmount * 0.2f; 
                    float jitter = Random.Range(-jitterIntensity, jitterIntensity);
                    
                    // Kayma Hızı
                    Vector2 slideVelocity = rightDir * (slideAmount + jitter);
                    
                    // Kademeli kontrol kaybı
                    float speedRatio = Mathf.Clamp01((slideAmount - 5f) / 25f); 
                    float controlPercent = Mathf.Lerp(0.5f, 0.1f, speedRatio);
                    
                    Vector2 suppressedInput = moveVelocity * controlPercent;
                    
                    Vector2 finalVelocity = suppressedInput + fallVelocity + slideVelocity;
                    body.linearVelocity = finalVelocity;
                    
                    // Görsel Titreme (Visual Shake)
                    if (visualRoot != null && visualRoot != transform)
                    {
                        // Hıza bağlı olarak titreşim şiddetini ayarla
                        float shakeMagnitude = (slideAmount / 30f) * 0.15f; // Max hızda 0.15 birim kayma
                        float shakeX = Random.Range(-shakeMagnitude, shakeMagnitude);
                        float shakeY = Random.Range(-shakeMagnitude, shakeMagnitude);
                        visualRoot.localPosition = initialVisualLocalPos + new Vector3(shakeX, shakeY, 0f);
                    }

                    // Debug.Log(...);
                }
            }
            else
            {
                // Deprem bittiğinde hızı sıfırla
                earthquakeSlideSpeed = 0f;
                // Normal hareket
                body.linearVelocity = moveVelocity + fallVelocity;

                // Titremeyi resetle
                if (visualRoot != null && visualRoot != transform)
                {
                    visualRoot.localPosition = initialVisualLocalPos;
                }
            }

            // Step Sound Logic
            if ((groundedForAnim) && Mathf.Abs(horizontalInput) > 0.01f)
            {
                // Dinamik Hız: speedMultiplier arttıkça adım sıklaşır
                float currentMultiplier = speedMultiplier > 0.1f ? speedMultiplier : 1f;
                float dynamicInterval = stepInterval / currentMultiplier;

                stepTimer -= Time.deltaTime * TimeCompensation;
                if (stepTimer <= 0)
                {
                    if (SoundManager.instance != null)
                        SoundManager.instance.PlayStepSound();
                    
                    stepTimer = dynamicInterval;
                }
            }
            else
            {
                stepTimer = 0.05f;
            }

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
            // Deprem kaymasını resetle
            ResetEarthquakeSlide();
            
            // Mevcut yatay hızı koru, yukarı doğru zıpla
            float horizontalSpeed = Vector2.Dot(body.linearVelocity, rightDir);
            body.linearVelocity = rightDir * horizontalSpeed + upDir * effectiveJumpPower;
            anim.SetTrigger("jump");

            if (SoundManager.instance != null)
                SoundManager.instance.PlaySound(SoundManager.instance.jumpSound);
        }
        else if (touchingWall && !grounded)
        {
            // Deprem kaymasını resetle
            ResetEarthquakeSlide();
            
            float direction = -Mathf.Sign(transform.localScale.x);
            // Wall jump: yatay hareket kompansasyonu, dikey minimal
            body.linearVelocity = rightDir * direction * 6f * TimeCompensation + upDir * effectiveJumpPower;

            ApplyFacingScale(direction);
            wallJumpCooldown = 0;

            if (SoundManager.instance != null)
                SoundManager.instance.PlaySound(SoundManager.instance.jumpSound);
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

    private void AlignPlayerToGravity(Vector2 gravityDir)
    {
        // "Up" yönü yerçekiminin tersi
        Vector2 upDir = -gravityDir;
        if (upDir.sqrMagnitude < 0.0001f)
            return;

        float targetAngle = Mathf.Atan2(upDir.y, upDir.x) * Mathf.Rad2Deg - 90f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetAngle);

        float maxStep = alignToGravityDegreesPerSecond <= 0f
            ? 99999f
            : alignToGravityDegreesPerSecond * Time.deltaTime * TimeCompensation;

        // Player'ın kendisini döndür (Collider dahil)
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxStep);
        
        // VisualRoot eğer script tarafından dönüyorsa (parent ile aynı değilse ve child ise),
        // parent döndüğü için visualRoot rotation'ı sıfırlanmalı/sabitlenmeli.
        if (visualRoot != null && visualRoot != transform)
        {
            // Eğer visualRoot child ise, parent (transform) döndüğünde o da döner. 
            // Ekstra dönmesine gerek yok, local olarak sıfırla.
            visualRoot.localRotation = Quaternion.identity;
        }
    }

    private bool isGrounded()
    {
        // Yerçekimi yönüne göre zemin kontrolü
        Vector2 gravityDir = Physics2D.gravity.normalized;
        
        // Rotation'lı durumda doğru BoxCast için:
        // Size: local size * scale ile eşleşmeli.
        // Rotation: transform.eulerAngles.z
        
        // Scale'in pozitif hali
        Vector2 scale = transform.lossyScale;
        Vector2 boxSize = boxCollider.size * new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        
        // Offset de rotate edilmeli -> transform.TransformPoint ile
        // (Not: BoxCollider.offset local space'te)
        // Ancak Physics2D.BoxCast 'origin' ister. bounds.center zaten world space center'dır.
        // Ama Rotasyonlu durumda bounds.center DOĞRU merkezdir, ama bounds.size yanlıştır (AABB).
        // Bu yüzden boxSize'ı kendimiz hesapladık. Origin olarak yine bounds.center veya transform + rotatedOffset kullanabiliriz.
        // TransformPoint(offset) en garantisidir.
        
        Vector2 origin = transform.TransformPoint(boxCollider.offset);
        float angle = transform.eulerAngles.z;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            boxSize,
            angle,
            gravityDir,
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
        // transform.right zaten rightDir ile eşleşecektir (eğer dönüş tamamlandıysa).
        
        // Facing Scale, transform rotation'dan bağımsızdır (Local Scale X).
        // Eğer scale.x pozitifse transform.right yönüne, negatifse -transform.right yönüne bakıyor.
        float facingDir = Mathf.Sign(transform.lossyScale.x);
        
        // Check direction
        Vector2 checkDir = rightDir * facingDir;
        
        // BoxCast params
        Vector2 scale = transform.lossyScale;
        Vector2 boxSize = boxCollider.size * new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        Vector2 origin = transform.TransformPoint(boxCollider.offset);
        float angle = transform.eulerAngles.z;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            boxSize,
            angle,
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

    /// <summary>
    /// Deprem sırasında kayma miktarını hesaplar (birim/saniye)
    /// </summary>
    private float GetEarthquakeSlideAmount(bool isGrounded)
    {
        // Time Slow aktifse kayma yok
        if (TimeSlowAbility.Instance != null && TimeSlowAbility.Instance.IsSlowMotionActive)
        {
            earthquakeSlideSpeed = 0f;
            return 0f;
        }

        // Zıplama reset timer'ı kontrol et
        if (jumpSlideResetTimer > 0f)
        {
            jumpSlideResetTimer -= Time.deltaTime;
            earthquakeSlideSpeed = 0f;
            return 0f;
        }

        // Kayma hızı parametreleri - KULLANICI İSTEĞİ (Max 30, Havada Yok)
        float maxSlideSpeed = 30f;       // Max 30
        float slideAcceleration = 15f;   
        float startSpeed = 10f;          
        
        if (earthquakeSlideSpeed < startSpeed)
            earthquakeSlideSpeed = startSpeed;
            
        earthquakeSlideSpeed = Mathf.MoveTowards(earthquakeSlideSpeed, maxSlideSpeed, slideAcceleration * Time.deltaTime);

        // Zıplarsa etki etmesin (Havada 0)
        if (!isGrounded)
            return 0f;

        return earthquakeSlideSpeed;
    }

    /// <summary>
    /// Zıplama yapıldığında kayma reset timer'ı başlat (Jump metodundan çağrılır)
    /// </summary>
    private void ResetEarthquakeSlide()
    {
        if (EarthquakeManager.Instance != null && EarthquakeManager.Instance.IsEarthquakeActive)
        {
            jumpSlideResetTimer = JUMP_SLIDE_RESET_DURATION;
            earthquakeSlideSpeed = 0f;
        }
    }
}
