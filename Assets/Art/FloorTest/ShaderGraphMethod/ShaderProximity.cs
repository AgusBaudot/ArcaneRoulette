using UnityEngine;

public class ShaderProximity : MonoBehaviour
{
    public Material floorMaterial; // Asigná acá el material de tu piso

    void Update()
    {
        // Le pasamos la posición actual de este objeto al shader
        floorMaterial.SetVector("_ObjectPosition", transform.position);
    }
}