using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    [SerializeField] private RoomManager[] _roomManagers;


    //suscribirnos al evento roomClear de la room[i] para desbloquear la siguiente room
    // si la room esta clear la siguiente = idle y la instanciamos
}
