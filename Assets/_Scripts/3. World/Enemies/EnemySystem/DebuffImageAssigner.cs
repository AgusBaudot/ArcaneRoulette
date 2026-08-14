using Foundation;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace World
{
    public class DebuffImageAssigner : MonoBehaviour
    {
        [SerializeField] private Sprite _redDebuff;
        [SerializeField] private Sprite _blueDebuff;
        [SerializeField] private Sprite _greenDebuff;
        [SerializeField] private Sprite _orangeDebuff;
        [SerializeField] private Image _image;

        private void Start()
        {
            var enemyHealth = GetComponent<EnemyHealth>();
            
            enemyHealth.OnDebuffApplied += OnDebuffApplied;
            enemyHealth.OnDebuffRemoved += OnDebuffRemoved;

            _image.enabled = false;
        }

        private void OnDebuffApplied(DebuffType type)
        {
            _image.enabled = true;

            switch (type)
            {
                case DebuffType.ATK:
                    _image.sprite = _redDebuff;
                    break;

                case DebuffType.Speed:
                    _image.sprite = _blueDebuff;
                    break;

                case DebuffType.AttackSpeed:
                    _image.sprite = _orangeDebuff;
                    break;

                case DebuffType.AntiHeal:
                    _image.sprite = _greenDebuff;
                    break;
            }
        }

        private void OnDebuffRemoved()
        {
            _image.enabled = false;
        }

        private void OnDisable()
        {
            var enemyHealth = GetComponent<EnemyHealth>();

            enemyHealth.OnDebuffApplied -= OnDebuffApplied;
            enemyHealth.OnDebuffRemoved -= OnDebuffRemoved;
        }
    }
}