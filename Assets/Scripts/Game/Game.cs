// ================================================================================================================================
// File:        Game.cs
// Description: Manages current state of the game
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class Game : MonoBehaviour
{
    //Store singleton instance and start the game
    public static Game I;
    private void Awake(){ I = this; }

    //Other components to use
    public NavMeshManager NavMesh;
    public GameObject Player;
    public PrefabSpawner Prefabs;

    public void Start() { StartGame(); }

    //Gets everything prepared that is required for the game to run
    private void StartGame()
    {
        //Get reference to all the important classes and systems use to manage the game
        NavMesh = NavMeshManager.Instance;
        Player = GameObject.Find("Player");
        Prefabs = PrefabSpawner.Instance;

        //Initialise them all to get the game world setup
        NavMesh.GenerateNavMesh();
    }
}