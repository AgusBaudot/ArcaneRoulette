using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public interface IPooleable
{
    void OnSpawn();
    void OnDespawn();
}
