using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("移动速度")]
    public float moveSpeed = 8f;

    [Header("动画控制")]
    public Animator animator;

    [Header("站立位置")]
    [SerializeField]
    private float defaultStandY = -2.5f;

    [Header("图层")]
    [SerializeField]
    private int defaultSortingOrder = 10;

    [SerializeField]
    private int sittingSortingOffset = -2;

    [SerializeField]
    private int localPlayerSortingOrder = 100;

    [SyncVar(hook = nameof(OnFacingChanged))]
    private bool spriteFlipX;

    [SyncVar]
    private int syncedSitAreaId;

    [SyncVar]
    private Vector3 syncedStandPosition;

    [SyncVar(hook = nameof(OnSittingChanged))]
    private bool isSitting;

    private readonly HashSet<SitArea> sitAreasInRange = new HashSet<SitArea>();

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private NetworkTransformReliable networkTransform;
    private float moveInput;
    private bool currentFlipX;
    private SitArea occupiedSitArea;
    private RigidbodyType2D standingBodyType;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        networkTransform = GetComponent<NetworkTransformReliable>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (rb != null)
            standingBodyType = rb.bodyType;

        transform.localScale = Vector3.one;
        currentFlipX = spriteFlipX;
        ApplyFacing(currentFlipX);
        ApplySittingState(isSitting);
        ApplySpriteSorting();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        currentFlipX = spriteFlipX;
        ApplyFacing(currentFlipX);
        ApplySittingState(isSitting);
        ApplySpriteSorting();
    }

    public override void OnStopServer()
    {
        ReleaseOccupiedSitArea();
        base.OnStopServer();
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

        HandleSitInput();

        if (isSitting)
        {
            moveInput = 0f;
            UpdateAnimation();
            return;
        }

        moveInput = Input.GetAxisRaw("Horizontal");

        if (Mathf.Abs(moveInput) > 0.1f)
            CmdStand();

        UpdateAnimation();
        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer || isSitting)
            return;

        if (PlayerInputLock.IsMovementBlocked())
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = new Vector2(
            moveInput * moveSpeed,
            rb.velocity.y
        );
    }

    public void NotifyEnterSitArea(SitArea area)
    {
        if (!isLocalPlayer || area == null)
            return;

        sitAreasInRange.Add(area);
    }

    public void NotifyExitSitArea(SitArea area)
    {
        if (!isLocalPlayer || area == null)
            return;

        if (isSitting && area.SitAreaId == syncedSitAreaId)
            return;

        sitAreasInRange.Remove(area);
    }

    private SitArea GetBestSitArea()
    {
        SitArea bestArea = null;
        float bestDistance = float.MaxValue;

        foreach (SitArea area in sitAreasInRange)
        {
            if (area == null)
                continue;

            float distance = Vector2.Distance(transform.position, area.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestArea = area;
            }
        }

        return bestArea;
    }

    private void HandleSitInput()
    {
        if (!Input.GetKeyDown(KeyCode.S))
            return;

        if (isSitting)
        {
            CmdStand();
            return;
        }

        SitArea targetArea = GetBestSitArea();
        if (targetArea == null)
            return;

        CmdTrySit(targetArea.SitAreaId);
    }

    [Command]
    private void CmdTrySit(int sitAreaId)
    {
        if (isSitting)
            return;

        if (!SitArea.TryGetArea(sitAreaId, out SitArea area))
            return;

        if (area.IsOccupied)
            return;

        if (!area.TryOccupy(netIdentity))
            return;

        occupiedSitArea = area;
        syncedStandPosition = transform.position;
        syncedSitAreaId = sitAreaId;

        Vector3 sitPosition = area.SitWorldPosition;
        ApplySitTransform(sitPosition);
        isSitting = true;
    }

    [Command]
    private void CmdStand()
    {
        if (!isSitting)
            return;

        ReleaseOccupiedSitArea();

        Vector3 standPosition = new Vector3(
            syncedStandPosition.x,
            defaultStandY,
            syncedStandPosition.z
        );

        syncedSitAreaId = 0;
        ApplyStandTransform(standPosition);
        isSitting = false;
    }

    private void ReleaseOccupiedSitArea()
    {
        if (occupiedSitArea != null)
        {
            occupiedSitArea.Release(netIdentity);
            occupiedSitArea = null;
        }
    }

    private void OnSittingChanged(bool oldValue, bool newValue)
    {
        ApplySittingState(newValue);
    }

    private void ApplySittingState(bool sitting)
    {
        if (animator != null)
        {
            animator.SetBool("isSitting", sitting);
            animator.SetBool("isWalking", false);
        }

        if (networkTransform != null)
            networkTransform.enabled = !sitting;

        if (rb == null)
        {
            ApplySpriteSorting();
            return;
        }

        if (sitting)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
            ApplySitTransform(ResolveSitPosition());
        }
        else
        {
            rb.bodyType = standingBodyType;
            ApplyStandTransform(new Vector3(
                syncedStandPosition.x,
                defaultStandY,
                syncedStandPosition.z
            ));
        }

        ApplySpriteSorting();
    }

    private Vector3 ResolveSitPosition()
    {
        if (syncedSitAreaId > 0 && SitArea.TryGetArea(syncedSitAreaId, out SitArea area))
            return area.SitWorldPosition;

        return transform.position;
    }

    private void ApplySitTransform(Vector3 sitPosition)
    {
        transform.position = sitPosition;

        if (isServer && networkTransform != null)
            networkTransform.ServerTeleport(sitPosition, transform.rotation);
    }

    private void ApplyStandTransform(Vector3 standPosition)
    {
        transform.position = standPosition;

        if (isServer && networkTransform != null)
            networkTransform.ServerTeleport(standPosition, transform.rotation);
    }

    private void ApplySpriteSorting()
    {
        if (spriteRenderer == null)
            return;

        if (isSitting)
        {
            spriteRenderer.sortingOrder = defaultSortingOrder + sittingSortingOffset;
            return;
        }

        spriteRenderer.sortingOrder = isLocalPlayer
            ? localPlayerSortingOrder
            : defaultSortingOrder;
    }

    private void UpdateAnimation()
    {
        if (isSitting)
        {
            if (animator != null)
            {
                animator.SetBool("isSitting", true);
                animator.SetBool("isWalking", false);
            }

            return;
        }

        bool isMoving = Mathf.Abs(moveInput) > 0.1f;

        if (animator != null)
        {
            animator.SetBool("isSitting", false);
            animator.SetBool("isWalking", isMoving);
        }
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
