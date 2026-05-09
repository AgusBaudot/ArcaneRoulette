using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace World 
{
    [CreateAssetMenu(fileName = "Room", menuName = "World/Maps/Room")]
    public class RoomScriptable : ScriptableObject
    {
        public RoomType roomType;

    }

}
