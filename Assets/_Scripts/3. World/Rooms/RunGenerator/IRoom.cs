using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World 
{
    public interface IRoom
    {
        //Activate
        int Index { get; }
        int Value { get; }
        RoomType RoomType{ get; }
        RoomState State { get; }
    }
}
