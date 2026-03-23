using UnityEngine;

[CreateAssetMenu(menuName = "ArcaneRoulette/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public GameObject worldPrefab;
    public bool stackable = true;
    public int maxStack = 99;
    public string id;
}
