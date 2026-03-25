using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Drop target for one rune category slot on the right side.
public sealed class RuneDropTargetUI : MonoBehaviour, IDropHandler
{
    public enum DropKind
    {
        Ability,
        Element,
        Modifier
    }

    // Slot column (Slot0/Slot1/Slot2).
    public Foundation.SlotIndex SlotIndex { get; private set; }

    public DropKind Kind { get; private set; }

    // For Modifier only: 0..4.
    public int ModifierIndex { get; private set; } = -1;

    private Action<RuneDragItemUI> _onDrop;

    public void Configure(
        Foundation.SlotIndex slotIndex,
        DropKind kind,
        int modifierIndex,
        Action<RuneDragItemUI> onDrop)
    {
        SlotIndex = slotIndex;
        Kind = kind;
        ModifierIndex = modifierIndex;
        _onDrop = onDrop;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (_onDrop == null) return;

        var dragged = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<RuneDragItemUI>()
            : null;

        if (dragged == null || dragged.Rune == null) return;

        dragged.MarkDroppedOnTarget();
        _onDrop.Invoke(dragged);
    }
}

