using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using World;
using static RoomManager;

public class FloorManager : MonoBehaviour
{
    public enum RunDificult 
    {
        easy,
        medium,
        hard
    }
    [SerializeField] private RoomManager[] _rooms;
    [SerializeField] private int _roomIndex;
    [SerializeField] private int MaxRooms;
    [SerializeField] private RoomManager _roomPrefab;
    [SerializeField] private RoomDoor _startRoom;
    [SerializeField] private RunDificult rundificult;
    private int extraenemies;
    private RoomManager _currentRoom;
    private GameObject _player;


    private void Start()
    {
        _rooms = new RoomManager[MaxRooms];
        _player = GameObject.FindGameObjectWithTag("Player");
        _startRoom.OnPlayerEnter += StartRun;
        SetDificult();
    }

    private void StartRun(Collider other)
    {
        _currentRoom = CreateAndAddRoom();
        TeleportPlayer(_currentRoom.GetPlayerSpawnEntry());
        BindRoom(_currentRoom);
    }

    private void ContinueRoom(Collider other)
    {
        if(_roomIndex >= MaxRooms) 
        {
            UnbindRoom(_currentRoom);
            Debug.Log("Termino la run");
        }
        else 
        {
            UnbindRoom(_currentRoom);
            _currentRoom = CreateAndAddRoom();
            _currentRoom._enemyMeleeCount = _roomIndex + extraenemies;
            _currentRoom._enemyRangeCount = _roomIndex + extraenemies;
            TeleportPlayer(_currentRoom.GetPlayerSpawnEntry());
            BindRoom(_currentRoom);
        }   
    }

    private void BindRoom(RoomManager room)
    {
        room._ContinueDoor.OnPlayerEnter += ContinueRoom;
    }

    private void UnbindRoom(RoomManager room)
    {
        room._ContinueDoor.OnPlayerEnter -= ContinueRoom;
    }

    private RoomManager CreateAndAddRoom()
    {
        RoomManager newRoom = Instantiate(_roomPrefab);

        if (_roomIndex == 0)
        {
            newRoom.transform.position = new Vector3(0, 0, 0);
        }
        else
        {
            RoomManager previousRoom = _rooms[_roomIndex - 1];
            newRoom.transform.position =
                previousRoom.transform.position + new Vector3(0, 0, 100);
        }

        _rooms[_roomIndex] = newRoom;
        newRoom._roomId = _roomIndex;

        _roomIndex++;

        return newRoom;
    }

    private void TeleportPlayer(Transform teleport) 
    {
        _player.transform.position = teleport.position;
    }

    public int MaximumRooms => MaxRooms;

    private void SetDificult() 
    {
        switch (rundificult) 
        {
            case RunDificult.easy:
                extraenemies = 1;
                break;
            case RunDificult.medium:
                extraenemies = 2;
                break;
            case RunDificult.hard:
                extraenemies = 3;
                break;
        }
    }
}
