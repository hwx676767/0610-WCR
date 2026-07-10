using Mirror;
using UnityEngine;

public class PlayerAppearance : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnAppearanceChanged))]
    public int appearanceIndex;

    [SerializeField]
    private RuntimeAnimatorController[] appearanceControllers;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    public int AppearanceCount => appearanceControllers != null ? appearanceControllers.Length : 0;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyAppearance();
    }

    public void AssignRandomAppearance()
    {
        if (appearanceControllers == null || appearanceControllers.Length == 0)
            return;

        appearanceIndex = Random.Range(0, appearanceControllers.Length);
    }

    private void OnAppearanceChanged(int oldIndex, int newIndex)
    {
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (appearanceControllers == null || appearanceControllers.Length == 0)
            return;

        int index = Mathf.Clamp(appearanceIndex, 0, appearanceControllers.Length - 1);
        RuntimeAnimatorController controller = appearanceControllers[index];

        if (animator != null && controller != null)
            animator.runtimeAnimatorController = controller;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;
    }
}
