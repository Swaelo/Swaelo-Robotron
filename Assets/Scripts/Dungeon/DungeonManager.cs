// ================================================================================================================================
// File:        DungeonManager.cs
// Description:	Manages the floor of the dungeon, the generation of rooms, what room the player is in, room transitions etc
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    //Singleton instance
    public static DungeonManager Instance = null;
    void Awake() { Instance = this; }

    //This room is setup immediately when the game begins, the player starts in this room
    private DungeonRoom StartRoom = null;

    //Prefab catalog used to spawn and generate rooms inside the dungeon
    public PrefabCatalog RoomPrefabs;

    public void Start()
    {
        //Create the initial spawn room
        StartRoom = new DungeonRoom
        {
            Width = 15f,
            Height = 8f,
            Position = Vector3.zero
        };

        RoomBuilder.Instance.BuildRoom(StartRoom);
    }
}