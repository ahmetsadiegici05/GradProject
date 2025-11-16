using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 7f;
    // Zıplama, velocity olarak uygulanıyor (AddForce değil)
    [SerializeField] private float jumpPower = 13.3f; // önerilen: ~3u yükseklik, ~0.45s apex
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;
    // Yerçekimi ayarları
    [SerializeField] private float baseGravityScale = 3.0f;       // önerilen temel değer
    [SerializeField] private float wallSlideGravityScale = 1.5f;  // duvarda kayarken daha hafif

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;
    private float wallJumpCooldown;
    private float horizontalInput;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        // Temel yerçekimini uygula
        body.gravityScale = baseGravityScale;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        // Flip
        if (horizontalInput > 0.01f)
            transform.localScale = Vector3.one;
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

    anim.SetBool("Run", horizontalInput != 0);
        anim.SetBool("grounded", isGrounded());

        bool touchingWall = onWall();
        bool grounded = isGrounded();

        if (wallJumpCooldown > 0.2f)
        {
            // Normal X movement
            body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);

            // WALL SLIDE
            if (touchingWall && !grounded && horizontalInput != 0)
            {
                // hafif kayma
                body.gravityScale = wallSlideGravityScale;

                if (body.linearVelocity.y < -2f)
                    body.linearVelocity = new Vector2(body.linearVelocity.x, -2f);
            }
            else
            {
                // normal gravity
                body.gravityScale = baseGravityScale;
            }

            // Jump
            if (Input.GetKeyDown(KeyCode.Space))
                Jump(touchingWall, grounded);
        }
        else
        {
            wallJumpCooldown += Time.deltaTime;
        }
    }

    private void Jump(bool touchingWall, bool grounded)
    {
        // Ground jump
        if (grounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
            anim.SetTrigger("jump");
        }
        // Wall jump
        else if (touchingWall && !grounded)
        {
            // duvar yönünün tersine zıpla
            float jumpDir = -Mathf.Sign(transform.localScale.x);

            body.linearVelocity = new Vector2(jumpDir * 6f, jumpPower);

            // Yüzü zıpladığı yöne döndür
            transform.localScale = new Vector3(jumpDir, 1, 1);

            wallJumpCooldown = 0;
        }
    }

    private bool isGrounded()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            Vector2.down,
            0.1f,
            groundLayer
        );
        return hit.collider != null;
    }

    private bool onWall()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCollider.bounds.center,
            boxCollider.bounds.size,
            0,
            new Vector2(transform.localScale.x, 0),
            0.1f,
            wallLayer
        );
        return hit.collider != null;
    }
}
