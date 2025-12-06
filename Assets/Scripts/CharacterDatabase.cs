using UnityEngine;
using System.Collections.Generic;


public static class CharacterDatabase
{

    public static KillerType GetKillerByName(string name)
    {
        string path = "ScriptableObjects/Killers/" + name + "/" + name + " Type";
        Debug.Log("Loading Killer: " + name + " With path: " + path);
        return Resources.Load(path) as KillerType;
    }


    public static SurvivorType GetSurvivorByName(string name)
    {
        string path = "ScriptableObjects/Survivors/" + name + "/" + name + " Type";
        Debug.Log("Loading Survivor: " + name + " With path: " + path);
        return Resources.Load(path) as SurvivorType;
    }
}