using UnityEngine;

namespace UI
{
    public class CursorInitializer : MonoBehaviour
    {
        [SerializeField] private Texture2D cursorTexture;
        [SerializeField] private Vector2 hotSpot = Vector2.zero;

        private void Start()
        {
            Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.ForceSoftware);
        }
    }
}