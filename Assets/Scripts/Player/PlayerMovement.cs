using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float jumpPower;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D body;
    private Animator anim;
    private BoxCollider2D boxCollider;
    private float wallJumpCooldown;
    private float horizontalInput;
    private float rawHorizontalInput;
    private Vector3 defaultWorldScale;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        defaultWorldScale = transform.lossyScale;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        rawHorizontalInput = Input.GetAxisRaw("Horizontal");

        // Flip
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
            // Movement
            body.linearVelocity = new Vector2(horizontalInput * speed, body.linearVelocity.y);

            // Wall slide
            if (touchingWall && !grounded && horizontalInput != 0)
            {
                body.gravityScale = 1.5f;

                if (body.linearVelocity.y < -2f)
                    body.linearVelocity = new Vector2(body.linearVelocity.x, -2f);
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
            wallJumpCooldown += Time.deltaTime;
        }
    }

    private void Jump(bool touchingWall, bool grounded)
    {
        if (grounded)
        {
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpPower);
            anim.SetTrigger("jump");
        }
        else if (touchingWall && !grounded)
        {
            float direction = -Mathf.Sign(transform.localScale.x);
            body.linearVelocity = new Vector2(direction * 6f, jumpPower);

            
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
}
