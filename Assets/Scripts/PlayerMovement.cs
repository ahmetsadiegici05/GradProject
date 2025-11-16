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

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        horizontalInput = Input.GetAxis("Horizontal");

        // Flip
        if (horizontalInput > 0.01f)
            transform.localScale = Vector3.one;
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1, 1, 1);

        // Animator parameters
        anim.SetBool("Run", horizontalInput != 0);
        anim.SetBool("grounded", isGrounded());

        bool touchingWall = onWall();
        bool grounded = isGrounded();

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
                Jump(touchingWall, grounded);
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

            // yüzü zıpladığı yöne döndür
            transform.localScale = new Vector3(direction, 1, 1);

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

    // SHOOTING CONDITION — saldırabilir mi?
    public bool canAttack()
    {
        return horizontalInput == 0 && isGrounded() && !onWall();
    }
}
