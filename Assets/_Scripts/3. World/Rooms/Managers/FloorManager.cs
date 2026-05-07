using UnityEngine;
using World;

public class FloorManager : MonoBehaviour
{
    public enum RunDificult 
    {
        easy,
        medium,
        hard
    }
    
    [Header("Room Sequence")]
    [Tooltip("Place your room prefabs here in the exact order they should spawn.")]
    [SerializeField] private RoomManager[] _roomPrefabs; 
    
    [Header("Settings")]
    [SerializeField] private RoomDoor _startRoom;
    [SerializeField] private RunDificult rundificult;
    
    private int _roomIndex = 0;
    private int extraenemies;
    private RoomManager _currentRoom;
    private GameObject _player;
    
    public int MaximumRooms => _roomPrefabs.Length;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player");
        _startRoom.OnPlayerEnter += StartRun;
        SetDificult();
    }

    private void StartRun(Collider other)
    {
        if (_roomPrefabs == null || _roomPrefabs.Length == 0)
        {
            Debug.LogError("FloorManager has no room prefabs assigned in the Inspector!");
            return;
        }

        _currentRoom = CreateAndAddRoom();
        TeleportPlayer(_currentRoom.GetPlayerSpawnEntry());
        BindRoom(_currentRoom);
    }

    private void ContinueRoom(Collider other)
    {
        UnbindRoom(_currentRoom);
        
        if(_roomIndex >= _roomPrefabs.Length) 
        {
            Debug.Log("Termino la run");
            return;
        }
        
        _currentRoom = CreateAndAddRoom();
        
        _currentRoom._enemyMeleeCount = _roomIndex + extraenemies;
        _currentRoom._enemyRangeCount = _roomIndex + extraenemies;
        
        TeleportPlayer(_currentRoom.GetPlayerSpawnEntry());
        BindRoom(_currentRoom);
    }

    private void BindRoom(RoomManager room)
        => room._ContinueDoor.OnPlayerEnter += ContinueRoom;

    private void UnbindRoom(RoomManager room)
    {
        if (room != null)
            room._ContinueDoor.OnPlayerEnter -= ContinueRoom;
    }

    private RoomManager CreateAndAddRoom()
    {
        // Grab the prefab from the array using our current index
        RoomManager prefabToSpawn = _roomPrefabs[_roomIndex];
        
        RoomManager newRoom = Instantiate(prefabToSpawn);

        newRoom.transform.position = new Vector3(0, 0, _roomIndex * 100f);
        newRoom._roomId = _roomIndex;

        _roomIndex++;

        return newRoom;
    }

    private void TeleportPlayer(Transform teleport) 
    {
        if (_player != null && teleport != null)
            _player.transform.position = teleport.position;
    }

    private void SetDificult() 
    {
        switch (rundificult) 
        {
            case RunDificult.easy: extraenemies = 1; break;
            case RunDificult.medium: extraenemies = 2; break;
            case RunDificult.hard: extraenemies = 3; break;
        }
    }
}