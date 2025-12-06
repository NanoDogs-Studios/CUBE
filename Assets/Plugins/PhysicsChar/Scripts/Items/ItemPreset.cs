using System;
using UnityEngine;

public class ItemPreset : MonoBehaviour
{
    public Item item;
    

}
[Serializable]
public class Item
{
    public string name;
    public Hands Type;
}

public enum Hands
{
    Left = 0,
    Right = 1,
    Both = 2
}