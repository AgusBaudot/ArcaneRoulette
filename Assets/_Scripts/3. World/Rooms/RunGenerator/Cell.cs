using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public int index, value;

    public GameObject cellBox;

    public void SetSpecialRoomMaterial(Material color) 
    {
        cellBox.GetComponent<Renderer>().material = color;
    }
}
