// ================================================================================================================================
// File:        DungeonRoom.cs
// Description:	Stores all information for one room in the dungeon
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class DungeonRoom
{
    public float Width;
    public float Height;
    public Vector3 Position;
    public bool Cleared;
    public List<GameObject> Walls;

    public DungeonRoom NorthNeighbour = null;
    public DungeonRoom EastNeighbour = null;
    public DungeonRoom SouthNeighbour = null;
    public DungeonRoom WestNeighbour = null;
}