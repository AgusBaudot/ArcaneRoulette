using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UI
{
    public class ConsoleUI : BaseUIPanel
    {
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private UnityEngine.UI.ScrollRect _scrollRect;

        private StringBuilder _logHistory = new StringBuilder();

        protected override void Awake()
        {
            base.Awake();
            _logHistory.AppendLine("<color=#00FFFF>=== Developer Console ===</color>");
            _logHistory.AppendLine("<color=#3C3C3C>Type 'help' for a list of available commands.</color>");

            if (_logText != null)
            {
                _logText.text = _logHistory.ToString();
            }
        }

        public override void Show()
        {
            base.Show();
            _inputField.text = "";
            _inputField.ActivateInputField();
            _inputField.onSubmit.AddListener(OnSubmitCommand);
        }

        public override void Hide()
        {
            base.Hide();
            _inputField.onSubmit.RemoveListener(OnSubmitCommand);
        }

        private void Update()
        {
            if (_inputField != null && _inputField.isFocused)
            {
                if (Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    if (Cheats.Instance != null && !string.IsNullOrEmpty(Cheats.Instance.LastCommand))
                    {
                        _inputField.text = Cheats.Instance.LastCommand;
                        
                        _inputField.caretPosition = _inputField.text.Length;
                    }
                }
            }
        }

        private void OnSubmitCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            string result = Cheats.Instance.ExecuteCommand(input);
            _logHistory.AppendLine(result + "\n");
            _logText.text = _logHistory.ToString();

            _inputField.text = "";
            _inputField.ActivateInputField();

            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();

            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 0;
            }
        }
    }
}