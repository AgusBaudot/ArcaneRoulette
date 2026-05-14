using System;
using Core;
using Foundation;
using UnityEngine;

namespace World
{
    public class HealingPatch : MonoBehaviour
    {
        [SerializeField] [Range(0, 1)] private float _healAmount = 0.5f; 
        [SerializeField] private float _radius = 2f;

        private bool _playerInside;

        private void Awake()
        {
            var col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = _radius * 0.25f;
            transform.GetChild(0).gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.E) && _playerInside)
            {
                FindObjectOfType<PlayerHealth>().Heal(Mathf.RoundToInt(GameStateManager.RunState.MaxHp * _healAmount));
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                transform.GetChild(0).gameObject.SetActive(true);
                _playerInside = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                transform.GetChild(0).gameObject.SetActive(false);
                _playerInside = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(transform.position.x, 0, transform.position.z), _radius);
        }
    }
}