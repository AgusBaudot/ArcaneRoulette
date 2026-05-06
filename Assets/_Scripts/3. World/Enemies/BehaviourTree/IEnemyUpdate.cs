using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemyUpdate
{
    void Tick();
    float interval {get; set;}
    float timer {get; set;}
}
