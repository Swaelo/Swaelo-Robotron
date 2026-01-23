// ================================================================================================================================
// File:        RoomBuilder.cs
// Description:	Used to build new rooms into the dungeon, place down their walls, doors to move between etc
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class RoomBuilder : MonoBehaviour
{
    //Singleton instance
    public static RoomBuilder Instance = null;
    public void Awake() { Instance = this; }

    public List<GameObject> BuildRoom(DungeonRoom Room)
    {
        return BuildRoom(Room.Width, Room.Height, Room.Position);
    }

    //Builds a rectanular room centered at 'center' using 4 wall segments, returns the walls in a list order North->East->South->West
    private List<GameObject> BuildRoom(float Width, float Height, Vector3 Center, float WallHeight = .25f, float WallThickness = .25f)
    {
        //Create a list to store the wall objects which are spawned in
        List<GameObject> WallObjects = new List<GameObject>();

        //Check the dungeon manager exists
        DungeonManager Dungeon = DungeonManager.Instance;
        if(Dungeon == null)
        {
            T.Log("Error building room, couldn't find the Dungeon Manager, room will not be built.");
            return WallObjects;
        }

        //Grab the wall prefab we will use to setup the boundaries of this room
        GameObject WallPrefab = Dungeon.RoomPrefabs.GetPrefab("Room Outer Wall");

        //Make sure the parameters being used to create this room are valid
        if(!BuildParametersValid(Width, Height, Center, WallPrefab))
        {
            T.Log("Error building room, parameters are invalid, room will not be built.");
            return WallObjects;
        }

        //Find the half extents of the room
        float HalfWidth = Width * 0.5f;
        float HalfHeight = Height * 0.5f;

        //Wall positions will pivot centered at the middle of the rooms given spawn location
        float YSpawn = Center.y + (WallHeight * 0.5f);

        //North +Z
        GameObject NorthWall = CreateWall(
            WallPrefab,
            "North_Wall",
            new Vector3(Center.x, Center.y + HalfHeight, 0f),
            Quaternion.identity,
            new Vector3(Width, WallHeight, WallThickness)
        );

        //South -Z
        GameObject SouthWall = CreateWall(
            WallPrefab,
            "South_Wall",
            new Vector3(Center.x, Center.y - HalfHeight, 0f),
            Quaternion.identity,
            new Vector3(Width, WallHeight, WallThickness)
        );

        //East +X - rotate so prefab X length runs along world z
        GameObject EastWall = CreateWall(
            WallPrefab,
            "East_Wall",
            new Vector3(Center.x + HalfWidth, Center.y, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(Height, WallHeight, WallThickness)
        );

        //West (-X)
        GameObject WestWall = CreateWall(
            WallPrefab,
            "West_Wall",
            new Vector3(Center.x - HalfWidth, Center.y, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(Height, WallHeight, WallThickness)
        );

        //Add all the walls to the list
        WallObjects.Add(NorthWall);
        WallObjects.Add(EastWall);
        WallObjects.Add(SouthWall);
        WallObjects.Add(WestWall);

        //Return the final list of wal objects that were created
        return WallObjects;
    }

    //Check for any errors which could prevent the room from being built correctly
    private bool BuildParametersValid(float Width, float Height, Vector3 Center, GameObject WallPrefab)
    {
        //Error out if we couldn't find the wall prefab
        if(WallPrefab == null)
        {
            T.Log("Room builder failure, cannot find the Room Outer Wall prefab, exiting out, room will not be buil.");
            return false;
        }

        //Make sure the room of the size is valid
        if(Width <= 0f || Height <= 0f)
        {
            T.Log("Room builder failure, cannot create a room with invalid dimensions " + Width + " by " + Height + ", room will not be built.");
            return false;
        }

        //All checks passed, room is valid to be built
        return true;
    }

    //Spawns a wall for the room
    private GameObject CreateWall(GameObject Prefab, string Name, Vector3 Position, Quaternion Rotation, Vector3 Scale)
    {
        //Spawn in the wall prefab
        GameObject Wall = Instantiate(Prefab, Position, Rotation);
        Wall.name = Name;

        Wall.transform.localScale = Scale;

        return Wall;
    }
}