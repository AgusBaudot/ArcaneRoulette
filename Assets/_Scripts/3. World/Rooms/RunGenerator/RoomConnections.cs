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
        [SerializeField] private GameObject _DownDoor;
        [SerializeField] private GameObject _upDoor;
        [SerializeField] private GameObject _leftDoor;
        [SerializeField] private GameObject _rightDoor;
        private AllDoorsInfo _allDoorsInfo;
        [Header("PlayerSpawn")]
        float _offsetSpawn = 1f;
        float _liftDoors = 5f;
        private Vector3 _playerSpawnDown, _playerSpawnUp, _playerSpawnLeft, _playerSpawnRight;

        public event Action<EdgeDirection> OnDoorActivated;
        public Vector3 GetPlayerSpawn(EdgeDirection dir)
        {
            switch (dir)
            {
                case EdgeDirection.Up: return _playerSpawnDown;
                case EdgeDirection.Down: return _playerSpawnUp;
                case EdgeDirection.Left: return _playerSpawnRight;
                case EdgeDirection.Right: return _playerSpawnLeft;
                default: return Vector3.zero;
            }
        }
        public void EnableConnections()
        {
            Debug.Log("Connections Enable");
            if (_bottom != null) _bottom.OnPlayerEnter += EnterDoor;
            if (_up != null) _up.OnPlayerEnter += EnterDoor;
            if (_left != null) _left.OnPlayerEnter += EnterDoor;
            if (_right != null) _right.OnPlayerEnter += EnterDoor;
        }
        public void SetDoorColors(AllDoorsInfo info) 
        {
            _upDoor.GetComponent<Renderer>().material = info.Up.Material;
            _DownDoor.GetComponent<Renderer>().material = info.Down.Material;
            _leftDoor.GetComponent<Renderer>().material = info.Left.Material;
            _rightDoor.GetComponent<Renderer>().material = info.Right.Material;
            _allDoorsInfo = info;
        }
        public void DisableConnections()
        {
            _bottom.OnPlayerEnter -= EnterDoor;
            _up.OnPlayerEnter -= EnterDoor;
            _left.OnPlayerEnter -= EnterDoor;
            _right.OnPlayerEnter -= EnterDoor;
        }
        public void CalculateSpawnsEntry()
        {
            _playerSpawnDown = _DownDoor.transform.position + new Vector3(0, 0, _offsetSpawn);
            _playerSpawnUp = _upDoor.transform.position + new Vector3(0, 0, -_offsetSpawn);
            _playerSpawnLeft = _leftDoor.transform.position + new Vector3(_offsetSpawn, 0, 0);
            _playerSpawnRight = _rightDoor.transform.position + new Vector3(-_offsetSpawn, 0, 0);
        }
        private void EnterDoor(EdgeDirection direction)
        {
            OnDoorActivated?.Invoke(direction);
        }
        public void RoomCleared() 
        {
            if (_allDoorsInfo.Down.UnlockOnClear) _DownDoor.transform.position = _DownDoor.transform.position + new Vector3(0, _liftDoors, 0);
            if (_allDoorsInfo.Up.UnlockOnClear) _upDoor.transform.position = _upDoor.transform.position + new Vector3(0, _liftDoors, 0);
            if (_allDoorsInfo.Left.UnlockOnClear) _leftDoor.transform.position = _leftDoor.transform.position + new Vector3(0, _liftDoors, 0);
            if (_allDoorsInfo.Right.UnlockOnClear) _rightDoor.transform.position = _rightDoor.transform.position + new Vector3(0, _liftDoors, 0);
        }
    }
}

