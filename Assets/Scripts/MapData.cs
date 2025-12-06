using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "ScriptableObjects/Map Data", order = 1)]
public class MapData : ScriptableObject
{
    public Sprite mapImage;
    public string mapName;
    public GameObject mapPrefab;
}