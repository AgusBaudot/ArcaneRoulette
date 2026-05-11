using UnityEngine;

namespace World 
{
    public enum RunDificult
    {
        easy,
        medium,
        hard
    }
    [RequireComponent(typeof(MapGenerator))]
    [RequireComponent(typeof(MapSpawner))]
    public class FloorManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private RoomDoor _startRoom;
        [SerializeField] private RunDificult rundificult;
        private GameObject _player;


        public int MaximumRooms = 1;

        public static FloorManager instance;

        private void Start()
        {
            instance = this;
            _player = GameObject.FindGameObjectWithTag("Player");
            _startRoom.OnPlayerEnter += StartRun;
            SetDificult();
        }

        private void StartRun(EdgeDirection dir)
        {
            if (false)
            {
                Debug.LogError("FloorManager has no room prefabs assigned in the Inspector!");
                return;
            }
            _startRoom.OnPlayerEnter -= StartRun;
            //TeleportPlayer(dir);
            //BindRoom(_currentRoom);
        }


        private void TeleportPlayer(EdgeDirection dir ,Transform teleport)
        {
            if (_player != null && teleport != null)
                _player.transform.position = teleport.position;
        }

        private void SetDificult()
        {
            switch (rundificult)
            {
                //case RunDificult.easy: extraenemies = 1; break;
                //case RunDificult.medium: extraenemies = 2; break;
                //case RunDificult.hard: extraenemies = 3; break;
            }
        }
    }
}
