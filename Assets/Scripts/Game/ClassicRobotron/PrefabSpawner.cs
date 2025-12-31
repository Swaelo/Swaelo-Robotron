// ================================================================================================================================
// File:        PrefabSpawner.cs
// Description:	Provides an easy way to access any prefabs needed from any other part of the codebase
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    //Singleton Instance
    public static PrefabSpawner Instance;
    private void Awake() { Instance = this; }

    //List of all the available prefabs that can be spawned
    [System.Serializable]
    public struct Prefab
    {
        public string Name;
        public GameObject Object;
    }
    public Prefab[] Prefabs;

    //Takes the name of a prefab, spawns it with the given position/rotation values and returns the object
    public GameObject SpawnPrefab(string Name, Vector3 Position, Quaternion Rotation)
    {
        //Find the requested prefab, spawn it and return it
        foreach (Prefab Prefab in Prefabs)
            if (Prefab.Name == Name)
                return Instantiate(Prefab.Object, Position, Rotation);

        //Print an error message and return null if the requested prefab couldnt be found in the dictionary
        Debug.Log("Couldnt spawn unknown prefab: " + Name);
        return null;
    }

    //Takes the name of a prefab and returns that prefab
    public GameObject GetPrefab(string Name)
    {
        //Find and return the request prefab
        foreach (Prefab Prefab in Prefabs)
            if (Prefab.Name == Name)
                return Prefab.Object;

        //Print an error message and return null if the prefab couldnt be found
        Debug.Log("Couldnt return unknown prefab: " + Name);
        return null;
    }

    //Returns randomly one of the 4 electrode prefabs
    public GameObject GetElectrodePrefab()
    {
        int ElectrodeType = Random.Range(1, 4);
        string PrefabName = "Electrode" +
            (ElectrodeType == 1 ? "A" :
            ElectrodeType == 2 ? "B" :
            ElectrodeType == 3 ? "C" : "D");
        return GetPrefab(PrefabName);
    }
}
