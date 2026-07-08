using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public static class PlayerInputLock
{
    public static bool IsMovementBlocked()
    {
        if (BubbleChatInputUI.IsBubbleInputOpen)
            return true;

        GameObject selected = EventSystem.current?.currentSelectedGameObject;
        if (selected == null)
            return false;

        return selected.GetComponentInParent<TMP_InputField>() != null;
    }
}
