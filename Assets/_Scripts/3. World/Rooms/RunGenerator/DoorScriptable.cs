using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    [CreateAssetMenu(fileName = "Door", menuName = "World/Maps/Door")]
    public class DoorScriptable : ScriptableObject
    {
        public RoomType roomType;
        public Material materialDoor;
    }
}

