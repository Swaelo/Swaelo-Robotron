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

    // //Size of the room
    // public float Width = 5f;
    // public float Height = 5f;

    // //World position the room is centered on
    // public Vector3 Position = Vector3.zero;

    // //Tracks if the room has been cleared of enemies or not
    // public bool Cleared = false;

    // //The wall objects which surround the outside of this room
    // public List<GameObject> Walls = null;

    // public void SetupRoom(float RoomWidth, float RoomHeight, Vector3 RoomPos)
    // {
    //     //Store these values locally for later
    //     Width = RoomWidth;
    //     Height = RoomHeight;
    //     Position = RoomPos;

    //     //Put down the walls of the room and store the wall objects here
    //     Walls = Game.I.Dungeon.Builder.BuildRoom(RoomWidth, RoomHeight, RoomPos);
    // }
}