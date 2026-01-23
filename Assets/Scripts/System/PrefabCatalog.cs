// ================================================================================================================================
// File:        PrefabCatalog.cs
// Description:	Stores references to a catalog of prefabs that will be accessed and spawned into the game during runtime
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Catalogs/Prefab Catalog")]
public class PrefabCatalog : MonoBehaviour
{
    //The name of this prefab catalog
    public string CatalogName = "Unnamed";

    //Small class used to store one single prefab entry in the catalog
    [Serializable]
    public class Entry
    {
        public string Name;   //Unique ID of the prefab object
        public GameObject Prefab;   //Reference to the prefab object itself from Unity
    }

    //List of all prefab entries stored in this catalog
    [SerializeField] private List<Entry> Entries = new();
    private Dictionary<string, Entry> ByName; //Prefabs are spawned in dictionary for quick and easy lookup

    //Sets up the catalog when the parent gameobject is ready
    private void OnEnable()
    {
        BuildIndex();
    }

    //Setups the dictionary lookup table for all prefabs available
    private void BuildIndex()
    {
        //Initialise the lookup dictionary
        ByName = new Dictionary<string, Entry>(StringComparer.Ordinal);

        //Go through and setup each entry in the catalog
        foreach(var Entry in Entries)
        {
            //If the prefab has no name, skip this one
            if(string.IsNullOrWhiteSpace(Entry.Name))
            {
                T.Log("Error initialising prefab in the " + CatalogName + " prefab catalog, a prefab had no name, skipping it.");
                continue;
            }

            //If no prefab object has been assigned, skip it
            if(Entry.Prefab == null)
            {
                T.Log("Error initialising the " + Entry.Name + " prefab in the " + CatalogName + " prefab catalog. No prefab object has been assigned, skipping it.");
                continue;
            }

            //Make sure a prefab doesn't already exist in this catalog with the same name
            if(ByName.ContainsKey(Entry.Name))
            {
                T.Log("Error initialising the " + Entry.Name + " prefab in the " + CatalogName + " prefab catalog. A prefab already exists in this catalog with that name, skipping this one.");
                continue;
            }

            //After all integrity checks have passed, add the new prefab into the lookup dictionary
            ByName.Add(Entry.Name, Entry);
        }
    }

    //Returns a prefab from the catalog by its name
    public GameObject GetPrefab(string PrefabName)
    {
        //Make sure the lookup table has already been initialised
        if(ByName == null)
        {
            //Log the error then set it up so we can use it
            T.Log("Trying to get " + PrefabName + " from " + CatalogName + " prefab catalog, but the lookup table hasn't been setup yet, doing that now.");
            BuildIndex();
        }

        //Look up and find the prefab we are looking for
        if(ByName.TryGetValue(PrefabName, out var Prefab) && Prefab.Prefab != null)
            return Prefab.Prefab;

        //If this prefab couldn't be found, error out
        T.Log("Error getting " + PrefabName + " prefab from the " + CatalogName + " prefab catalog. No prefab by that name exists in this catalog. Returning null.");
        return null;
    }
}