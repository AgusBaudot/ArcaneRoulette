using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World
{
    public class RoomConnections : MonoBehaviour
    {
        [Header("Triggers")]
        [SerializeField] private RoomDoor _bottom;
        [SerializeField] private RoomDoor _up;
        [SerializeField] private RoomDoor _left;
        [SerializeField] private RoomDoor _right;
        [Header("Doors")]
        [SerializeField] private GameObject _bottomDoor;
        [SerializeField] private GameObject _upDoor;
        [SerializeField] private GameObject _leftDoor;
        [SerializeField] private GameObject _rightDoor;
        [Header("PlayerSpawn")]
        float _offsetSpawn = 5f;
        private Vector3 _playerSpawnBottom, _playerSpawnUp, _playerSpawnLeft, _playerSpawnRight;

        public event Action<EdgeDirection> OnDoorActivated;

        public Vector3 GetPlayerSpawnBottom() => _playerSpawnBottom;
        public Vector3 GetPlayerSpawnUp() => _playerSpawnUp;
        public Vector3 GetPlayerSpawnLeft() => _playerSpawnLeft;
        public Vector3 GetPlayerSpawnRight() => _playerSpawnRight;

        public void Start()
        {
            CalculateSpawnsEntry();
        }
        public void EnableConnections()
        {
            if (_bottom != null) _bottom.OnPlayerEnter += EnterDoor;
            if (_up != null) _up.OnPlayerEnter += EnterDoor;
            if (_left != null) _left.OnPlayerEnter += EnterDoor;
            if (_right != null) _right.OnPlayerEnter += EnterDoor;
        }
        private void DisableConnections()
        {
            _bottom.OnPlayerEnter -= EnterDoor;
            _up.OnPlayerEnter -= EnterDoor;
            _left.OnPlayerEnter -= EnterDoor;
            _right.OnPlayerEnter -= EnterDoor;
        }
        private void CalculateSpawnsEntry()
        {
            _playerSpawnBottom = _bottomDoor.transform.position + new Vector3(0, 0, _offsetSpawn);
            _playerSpawnUp = _upDoor.transform.position + new Vector3(0, 0, -_offsetSpawn);
            _playerSpawnLeft = _leftDoor.transform.position + new Vector3(_offsetSpawn, 0, 0);
            _playerSpawnRight = _rightDoor.transform.position + new Vector3(-_offsetSpawn, 0, 0);
        }
        private void EnterDoor(EdgeDirection direction)
        {
            OnDoorActivated?.Invoke(direction);
        }
    }
}

