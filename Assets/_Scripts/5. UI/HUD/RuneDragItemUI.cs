using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// Attach to a rune tile (typically RuneSlot.prefab) to make it draggable inside the panel.
public sealed class RuneDragItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum DragOriginKind
    {
        Inventory,
        SlotAbility,
        SlotElement,
        SlotModifier
    }

    // Which rune to place.
    public Foundation.RuneDefinitionSO Rune { get; private set; }

    // How many "units" this drag represents (for now always 1).
    public int Quantity { get; private set; } = 1;

    // Where the drag came from.
    public DragOriginKind OriginKind { get; private set; } = DragOriginKind.Inventory;
    public Foundation.SlotIndex OriginSlot { get; private set; }
    public int OriginModifierIndex { get; private set; } = -1; //0..4 for modifier drags

    // Set by panel after it computes local available counts.
    public bool CanDrag { get; private set; }

    private bool _droppedOnTarget;
    private Canvas _canvas;
    private RectTransform _rect;
    private RectTransform _parentBeforeDrag;
    private CanvasGroup _canvasGroup;

    private Vector2 _offset;

    public void Configure(
        Foundation.RuneDefinitionSO rune,
        bool canDrag,
        DragOriginKind originKind,
        Foundation.SlotIndex originSlot,
        int originModifierIndex,
        int quantity = 1)
    {
        Rune = rune;
        CanDrag = canDrag;
        OriginKind = originKind;
        OriginSlot = originSlot;
        OriginModifierIndex = originModifierIndex;
        Quantity = Mathf.Max(1, quantity);
    }

    public void MarkDroppedOnTarget()
    {
        _droppedOnTarget = true;
    }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _rect = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.blocksRaycasts = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag || Rune == null)
        {
            eventData.pointerDrag = null;
            return;
        }

        _droppedOnTarget = false;
        _parentBeforeDrag = _rect.parent as RectTransform;
        transform.SetParent(_canvas.transform, worldPositionStays: true);

        _canvasGroup.blocksRaycasts = false;

        if (_canvas != null)
        {
            // Keep the tile under the pointer.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rect,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);
            _offset = _rect.localPosition - (Vector3)localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_canvas == null) return;
        _rect.anchoredPosition = eventData.position + _offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;

        // Unity sometimes doesn't call IDropHandler.OnDrop() in edge cases,
        // but the pointer can still end over a valid drop target.
        if (!_droppedOnTarget && eventData != null && eventData.pointerEnter != null)
        {
            if (eventData.pointerEnter.GetComponentInParent<RuneDropTargetUI>() != null)
                _droppedOnTarget = true;
        }

        // Some edge cases report the raycast target in pointerCurrentRaycast instead of pointerEnter.
        if (!_droppedOnTarget && eventData != null && eventData.pointerCurrentRaycast.gameObject != null)
        {
            if (eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<RuneDropTargetUI>() != null)
                _droppedOnTarget = true;
        }

        // Robust fallback: run a full UI raycast at the drag-end position.
        // This catches cases where pointerEnter/pointerCurrentRaycast isn't updated reliably.
        if (!_droppedOnTarget && eventData != null && EventSystem.current != null)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == null) continue;
                if (go.GetComponentInParent<RuneDropTargetUI>() != null)
                {
                    _droppedOnTarget = true;
                    break;
                }
            }
        }

        // If it was not dropped on any rune slot target, and it originated from an
        // equipped slot, we clear only that rune from the origin slot (visual-only).
        // We do NOT dismantle the whole spell slot here; that was too destructive.
        if (!_droppedOnTarget && OriginKind != DragOriginKind.Inventory)
        {
            var ui = FindObjectOfType<SpellCraftingUI>();
            if (ui != null)
            {
                var panel = ui.GetPanelForSlot(OriginSlot);
                panel?.ClearSlotFromOrigin(OriginKind, OriginModifierIndex);
            }
        }

        if (_parentBeforeDrag != null)
        {
            transform.SetParent(_parentBeforeDrag, worldPositionStays: true);
        }
    }
}

