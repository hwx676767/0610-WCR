using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 5f;

    [Header("动画控制")]
    public Animator animator;

    [SyncVar(hook = nameof(OnFacingChanged))]
    private bool spriteFlipX;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float moveInput;
    private bool currentFlipX;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        transform.localScale = Vector3.one;
        currentFlipX = spriteFlipX;
        ApplyFacing(currentFlipX);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        currentFlipX = spriteFlipX;
        ApplyFacing(currentFlipX);
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;

        if (PlayerInputLock.IsMovementBlocked())
        {
            moveInput = 0f;
            UpdateAnimation();
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        UpdateAnimation();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer)
            return;

        if (PlayerInputLock.IsMovementBlocked())
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        rb.linearVelocity = new Vector2(
            moveInput * moveSpeed,
            rb.linearVelocity.y
        );
    }

    private void UpdateAnimation()
    {
        bool isMoving = Mathf.Abs(moveInput) > 0.1f;

        if (animator != null)
            animator.SetBool("isWalking", isMoving);
    }

    private void UpdateFacing()
    {
        if (moveInput > 0.1f)
            SetFacing(true);
        else if (moveInput < -0.1f)
            SetFacing(false);
    }

    private void SetFacing(bool flipX)
    {
        if (currentFlipX == flipX)
            return;

        currentFlipX = flipX;
        ApplyFacing(flipX);

        if (isLocalPlayer)
            CmdSetFacing(flipX);
    }

    [Command]
    private void CmdSetFacing(bool flipX)
    {
        spriteFlipX = flipX;
    }

    private void OnFacingChanged(bool oldFlipX, bool newFlipX)
    {
        currentFlipX = newFlipX;

        if (isLocalPlayer)
            return;

        ApplyFacing(newFlipX);
    }

    private void ApplyFacing(bool flipX)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = flipX;
    }
}
