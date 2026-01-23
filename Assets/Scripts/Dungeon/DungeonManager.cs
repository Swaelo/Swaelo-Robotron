// ================================================================================================================================
// File:        DungeonManager.cs
// Description:	Manages the floor of the dungeon, the generation of rooms, what room the player is in, room transitions etc
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    //Singleton instance
    public static DungeonManager Instance = null;
    void Awake() { Instance = this; }

    //This room is setup immediately when the game begins, the player starts in this room
    private DungeonRoom StartRoom = null;

    //List of all other rooms that have been added into the dungeon
    List<DungeonRoom> Rooms = new List<DungeonRoom>();

    //Prefab catalog used to spawn and generate rooms inside the dungeon
    public PrefabCatalog RoomPrefabs;

    //Random seed used to create random dungeon layouts
    private System.Random RNG;
    public int DungeonSeed = 0;

    //Room sides definition
    public enum Side { North, East, South, West }

    public void Start()
    {
        //Create the initial spawn room
        StartRoom = new DungeonRoom
        {
            Width = 15f,
            Height = 8f,
            Position = Vector3.zero
        };

        //Add it into our list of rooms
        Rooms.Add(StartRoom);

        //Build the starting room
        RoomBuilder.Instance.BuildRoom(StartRoom);

        //Try adding a bunch of extra rooms into the dungeon
        AddRooms(15, 3f, 15f);
    }

    //Generates a bunch of rooms in the dungeon
    private void AddRooms(int RoomCount, float MinSize, float MaxSize)
    {
        //Make sure we are trying to spawn in a real number of new rooms
        if(RoomCount <= 0)
        {
            T.Log("Dungeon Manager error, trying to add in " + RoomCount + " new rooms, which is a non positive number.");
            return;
        }

        //Make sure the new room we want to add in is of a valid size
        if(MinSize <= 0f || MaxSize <= 0f || MaxSize < MinSize)
        {
            T.Log("Dungeon Manager error, trying to add in new room with dimension range of " + MinSize + " - " + MaxSize + " which is invalid, no room will be created.");
            return;
        }

        //Safety to avoid infinite loops if the layout gets boxed in
        int MaxAttempts = RoomCount * 50;
        int Attempts = 0;
        int Created = 0;

        //Keep adding new rooms until we reach the desired number, or we run out of attempts
        while(Created < RoomCount && Attempts < MaxAttempts)
        {
            //Track how many spawn attempts have been made
            Attempts++;

            //Pick a random direction to expand in
            Side Direction = GetRandomSide();

            //Find a room that has a free neighbour slot in that direction
            DungeonRoom Anchor = FindRoomWithFreeSide(Direction);
            if(Anchor == null)
                //Keep searching if we couldn't find any rooms with free neighbour slots
                continue;

            //Random size for the new room
            float Width = Random.Range(MinSize, MaxSize);
            float Height = Random.Range(MinSize, MaxSize);

            //Try to create the new room
            DungeonRoom NewRoom = CreateAdjacentRoom(Anchor, Direction, Width, Height);

            //Increment spawned room numbers if this one was a success
            if(NewRoom != null)
                Created++;
        }

        //Print out an error if we were unable to create the desired number of rooms
        if(Created < RoomCount)
        {
            T.Log("Dungeon Manager AddRooms function, tried to add in " + RoomCount + " but ran out of attempts after creating just " + Created + " rooms.");
        }
    }

    //Returns a DungeonRoom that doesn't currently have an adjacent room in the given direction
    private DungeonRoom FindRoomWithFreeSide(Side Direction)
    {
        //Randomize the order we can rooms to avoid biasing toward early roms
        for(int i = 0; i < Rooms.Count; i++)
        {
            //Grab a random room
            int RoomID = Random.Range(0, Rooms.Count);
            DungeonRoom Room = Rooms[RoomID];
            
            //Skip if it doesn't exist for some reason
            if(Room == null)
            {
                T.Log("Dungeon Manager error, FindRoomWithFreeSide checking a null room from the active rooms list, will skip to next room.");
                continue;
            }

            //Return this room if its got no neighbours in that direction
            if(!HasNeighbour(Room, Direction))
                return Room;
        }

        //Return null if no room could be found with a free slot on that side
        T.Log("Error in DungeonManager, couldn't find any room without a neighbour in " + Direction + " direction.");
        return null;
    }

    //Checks if a room currently has a neighbour in the given direction
    private bool HasNeighbour(DungeonRoom Room, Side Direction)
    {
        switch(Direction)
        {
            case Side.North: return Room.NorthNeighbour != null;
            case Side.East: return Room.EastNeighbour != null;
            case Side.South: return Room.SouthNeighbour != null;
            case Side.West: return Room.WestNeighbour != null;
            default: return true;
        }
    }

    //Creates a new room directly adjacent to an existing room, touching edge-to-edge
    public DungeonRoom CreateAdjacentRoom(DungeonRoom Existing, Side Direction, float NewWidth, float NewHeight)
    {
        //Error out if the existing room given doesn't exist
        if(Existing == null)
        {
            T.Log("Dungeon Manager error adding adjacent wall, the given existing room to place it next to was not found, new room will not be created.");
            return null;
        }

        //Half extents of existing and new rooms
        float ExistingHalfWidth = Existing.Width * 0.5f;
        float ExistingHalfHeight = Existing.Height * 0.5f;
        float HalfWidth = NewWidth * 0.5f;
        float HalfHeight = NewHeight * 0.5f;

        //Find the center location for the new room we will spawn in
        Vector3 NewCenter = Existing.Position;
        switch(Direction)
        {
            case Side.North:
                NewCenter += new Vector3(0f, ExistingHalfHeight + HalfHeight, 0f);
                break;
            case Side.East:
                NewCenter += new Vector3(ExistingHalfWidth + HalfWidth, 0f, 0f);
                break;
            case Side.South:
                NewCenter += new Vector3(0f, -(ExistingHalfHeight + HalfHeight), 0f);
                break;
            case Side.West:
                NewCenter += new Vector3(-(ExistingHalfWidth + HalfWidth), 0f, 0f);
                break;
        }

        //Create a new room at the new location
        DungeonRoom NewRoom = new DungeonRoom
        {
            Width = NewWidth,
            Height = NewHeight,
            Position = NewCenter,
            Cleared = false,
            Walls = null
        };

        //Reject the creation of the new room if it would overlap with any of the already existing rooms
        if(WouldOverlapExistingRooms(NewRoom))
            return null;

        //Build the new room and store its walls in itself
        NewRoom.Walls = RoomBuilder.Instance.BuildRoom(NewRoom);

        //Store the new room in our list, then return it
        Rooms.Add(NewRoom);

        //Add the rooms as being each others neighbours
        switch(Direction)
        {
            case Side.North:
                NewRoom.SouthNeighbour = Existing;
                Existing.NorthNeighbour = NewRoom;
                break;
            case Side.East:
                NewRoom.WestNeighbour = Existing;
                Existing.EastNeighbour = NewRoom;
                break;
            case Side.South:
                NewRoom.NorthNeighbour = Existing;
                Existing.SouthNeighbour = NewRoom;
                break;
            case Side.West:
                NewRoom.EastNeighbour = Existing;
                Existing.WestNeighbour = NewRoom;
                break;
        }

        T.Log("Added new room in " + Direction + " direction.");

        return NewRoom;
    }

    //Returns true if candidate overlaps any existing rooms
    private bool WouldOverlapExistingRooms(DungeonRoom Candidate)
    {
        //Error out if the candidate room we are meant to check doesn't exist
        if(Candidate == null)
        {
            T.Log("Dungeon Manager error checking room overlaps, candidate room we are meant to check doesn't exist. Check will fail to be safe.");
            return true;
        }

        //Define a rect boundary from the candidate room
        Rect CandidateRect = RoomRectXY(Candidate);

        //Check for overlaps between any other existing rooms
        for(int i = 0; i < Rooms.Count; i++)
        {
            //Grab the room we want to check against
            DungeonRoom CompareRoom = Rooms[i];

            //Skip this one if it doesn't exist for some reason
            if(CompareRoom == null)
            {
                T.Log("Dungeon Manager error check for room overlaps, room to compare doesnt exist, will skip this check.");
                continue;
            }

            //Get a rect from the room we are comparing, and check if it overlaps with the candidate room
            Rect CompareRect = RoomRectXY(CompareRoom);
            if(CompareRect.Overlaps(CandidateRect))
                return true;
        }

        return false;
    }

    //Builds a Rect in X/Y from a rooms center position and size
    private Rect RoomRectXY(DungeonRoom Room)
    {
        float HalfWidth = Room.Width * 0.5f;
        float HalfHeight = Room.Height * 0.5f;

        float XMin = Room.Position.x - HalfWidth;
        float YMin = Room.Position.y - HalfHeight;

        return new Rect(XMin, YMin, HalfWidth * 2f, HalfHeight * 2f);
    }

    //Returns a random side to be used
    public Side GetRandomSide()
    {
        int SideValue = Random.Range(0, 4);
        return (Side)SideValue;
    }
}