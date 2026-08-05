using System;
using UnityEngine;

namespace World
{
    public class RoomConnections : MonoBehaviour
    {
        #region  Serialized References

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

        #endregion

        #region Fields

        public event Action<EdgeDirection> OnDoorActivated;

        private AllDoorsInfo _allDoorsInfo;
        private Vector3 _playerSpawnDown, _playerSpawnUp, _playerSpawnLeft, _playerSpawnRight;

        private static readonly int UnlockHash = Animator.StringToHash("Unlock");
        private static readonly int LockHash = Animator.StringToHash("Lock");

        #endregion

        #region Init

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
        
        public void InitializeDeadEnds()
        {
            if (!_allDoorsInfo.Down.UnlockOnClear) SetDoorState(_bottomDoor, LockHash, true);
            if (!_allDoorsInfo.Up.UnlockOnClear) SetDoorState(_upDoor, LockHash, true);
            if (!_allDoorsInfo.Left.UnlockOnClear) SetDoorState(_leftDoor, LockHash, true);
            if (!_allDoorsInfo.Right.UnlockOnClear) SetDoorState(_rightDoor, LockHash, true);
        }

        private Vector3 GetFlatSpawnPosition(Vector3 doorPosition, Vector3 offset)
        {
            return new Vector3(doorPosition.x + offset.x, 0f, doorPosition.z + offset.z);
        }

        #endregion

        #region Info

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

        #endregion

        #region Animation Triggers

        public void RoomCleared()
        {
            if (_allDoorsInfo.Down.UnlockOnClear) SetDoorState(_bottomDoor, UnlockHash, false);
            if (_allDoorsInfo.Up.UnlockOnClear) SetDoorState(_upDoor, UnlockHash, false);
            if (_allDoorsInfo.Left.UnlockOnClear) SetDoorState(_leftDoor, UnlockHash, false);
            if (_allDoorsInfo.Right.UnlockOnClear) SetDoorState(_rightDoor, UnlockHash, false);
        }

        public void LockDoors()
        {
            if (_allDoorsInfo.Down.UnlockOnClear) SetDoorState(_bottomDoor, LockHash, true);
            if (_allDoorsInfo.Up.UnlockOnClear) SetDoorState(_upDoor, LockHash, true);
            if (_allDoorsInfo.Left.UnlockOnClear) SetDoorState(_leftDoor, LockHash, true);
            if (_allDoorsInfo.Right.UnlockOnClear) SetDoorState(_rightDoor, LockHash, true);
        }

        private void SetDoorState(GameObject door, int stateHash, bool isLocked)
        {
            if (door == null) return;

            if (door.TryGetComponent<Animator>(out var animator))
            {
                animator.SetTrigger(stateHash);
            }

            if (door.TryGetComponent<Collider>(out var col))
            {
                col.enabled = isLocked;
            }
        }

        #endregion

        #region Enable/Disable Lifecycle

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

        #endregion
    }
}