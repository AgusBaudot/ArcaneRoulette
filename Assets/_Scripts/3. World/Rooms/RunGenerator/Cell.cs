using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RoomManager;

namespace World 
{
    public class Cell : MonoBehaviour
    {
        public int index, value;
        public RoomType roomType;

        public GameObject cellBox;

        public void SetSpecialRoomMaterial(Material color)
        {
            cellBox.GetComponent<Renderer>().material = color;
        }

        public void SetRoomType(RoomType newRoomType) 
        {
            roomType = newRoomType;
        }
    }
}

