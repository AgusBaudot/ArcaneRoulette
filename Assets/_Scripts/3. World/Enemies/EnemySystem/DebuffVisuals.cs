using Foundation;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public class DebuffVisuals : MonoBehaviour, IDebuffReceiver
    {
        [Header("Icons (Place in a Horizontal Layout Group)")]
        [SerializeField] private Image _atkIcon;
        [SerializeField] private Image _speedIcon;
        [SerializeField] private Image _attackSpeedIcon;
        [SerializeField] private Image _antiHealIcon;

        private IDebuffReadable _debuffs;

        private void Awake()
        {
            HideAllIcons();
        }

        public void RegisterDebuff(IDebuffReadable debuff)
        {
            _debuffs = debuff;
            _debuffs.OnDebuffApplied += HandleDebuffApplied;
            _debuffs.OnDebuffRemoved += HandleDebuffRemoved;

            foreach (var type in _debuffs.ActiveTypes)
            {
                HandleDebuffApplied(type);
            }
        }

        public void UnregisterDebuff()
        {
            if (_debuffs != null)
            {
                _debuffs.OnDebuffApplied -= HandleDebuffApplied;
                _debuffs.OnDebuffRemoved -= HandleDebuffRemoved;
            }
            
            _debuffs = null;
            HideAllIcons(); 
        }

        private void HandleDebuffApplied(DebuffType type)
        {
            Image target = GetIconForType(type);
            if (target != null) 
            {
                target.gameObject.SetActive(true);
                target.transform.SetAsLastSibling();
            }
        }

        private void HandleDebuffRemoved(DebuffType type)
        {
            Image target = GetIconForType(type);
            if (target != null) 
            {
                target.gameObject.SetActive(false);
            }
        }

        private Image GetIconForType(DebuffType type)
        {
            return type switch
            {
                DebuffType.ATK => _atkIcon,
                DebuffType.Speed => _speedIcon,
                DebuffType.AttackSpeed => _attackSpeedIcon,
                DebuffType.AntiHeal => _antiHealIcon,
                _ => null
            };
        }

        private void HideAllIcons()
        {
            if (_atkIcon) _atkIcon.gameObject.SetActive(false);
            if (_speedIcon) _speedIcon.gameObject.SetActive(false);
            if (_attackSpeedIcon) _attackSpeedIcon.gameObject.SetActive(false);
            if (_antiHealIcon) _antiHealIcon.gameObject.SetActive(false);
        }
    }
}