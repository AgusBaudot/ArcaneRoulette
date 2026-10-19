using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class OptionsUI : BaseUIPanel
    {
        [SerializeField] private Button _closeButton;

        protected override void Awake()
        {
            base.Awake();
            
            _closeButton.onClick.AddListener(RequestClose);
        }
    }
}