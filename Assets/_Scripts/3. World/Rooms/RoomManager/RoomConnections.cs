using System;
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
        [SerializeField] private float _offsetSpawn = 1f;
        [SerializeField] private float _liftDoors = 5f;

        private AllDoorsInfo _allDoorsInfo;
        private Vector3 _playerSpawnDown, _playerSpawnUp, _playerSpawnLeft, _playerSpawnRight;
        public event Action<EdgeDirection> OnDoorActivated;

        // ---- Init ----
        public void SetDoorColors(AllDoorsInfo info)
        {
            if (_upDoor.TryGetComponent<Renderer>(out var upRend)) upRend.material = info.Up.Material;
            if (_bottomDoor.TryGetComponent<Renderer>(out var downRend)) downRend.material = info.Down.Material;
            if (_leftDoor.TryGetComponent<Renderer>(out var leftRend)) leftRend.material = info.Left.Material;
            if (_rightDoor.TryGetComponent<Renderer>(out var rightRend)) rightRend.material = info.Right.Material;
            _allDoorsInfo = info;
        }
        public void CalculateSpawnsEntry()
        {
            _playerSpawnDown = GetFlatSpawnPosition(_bottomDoor.transform.position, new Vector3(0f, 0f, _offsetSpawn));
            _playerSpawnUp = GetFlatSpawnPosition(_upDoor.transform.position, new Vector3(0f, 0f, -_offsetSpawn));
            _playerSpawnLeft = GetFlatSpawnPosition(_leftDoor.transform.position, new Vector3(_offsetSpawn, 0f, 0f));
            _playerSpawnRight = GetFlatSpawnPosition(_rightDoor.transform.position, new Vector3(-_offsetSpawn, 0f, 0f));
        }
        private Vector3 GetFlatSpawnPosition(Vector3 doorPosition, Vector3 offset)
        {
            return new Vector3(doorPosition.x + offset.x, 0f, doorPosition.z + offset.z);
        }

        // ---- Info ----
        private void EnterDoor(EdgeDirection direction)
        {
            OnDoorActivated?.Invoke(direction);
        }
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
        public void RoomCleared()
        {
            Vector3 liftOffset = new Vector3(0f, _liftDoors, 0f);

            if (_allDoorsInfo.Down.UnlockOnClear) _bottomDoor.transform.position += liftOffset;
            if (_allDoorsInfo.Up.UnlockOnClear) _upDoor.transform.position += liftOffset;
            if (_allDoorsInfo.Left.UnlockOnClear) _leftDoor.transform.position += liftOffset;
            if (_allDoorsInfo.Right.UnlockOnClear) _rightDoor.transform.position += liftOffset;
        }

        // ---- Enable Disable ----
        public void EnableConnections()
        {
            if (_bottom != null) _bottom.OnPlayerEnter += EnterDoor;
            if (_up != null) _up.OnPlayerEnter += EnterDoor;
            if (_left != null) _left.OnPlayerEnter += EnterDoor;
            if (_right != null) _right.OnPlayerEnter += EnterDoor;
        }
        public void DisableConnections()
        {
            if (_bottom != null) _bottom.OnPlayerEnter -= EnterDoor;
            if (_up != null) _up.OnPlayerEnter -= EnterDoor;
            if (_left != null) _left.OnPlayerEnter -= EnterDoor;
            if (_right != null) _right.OnPlayerEnter -= EnterDoor;
        }
    }
}
