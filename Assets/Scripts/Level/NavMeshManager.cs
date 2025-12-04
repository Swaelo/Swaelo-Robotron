// ================================================================================================================================
// File:        NavMeshManager.cs
// Description:	Creates a dynamic navigation mesh every time a new wave begins and the enemies have finished being spawned in
// Certain enemies will use this for their navigation through the level
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance; //Singleton instance
    private void Awake() { Instance = this; }

    //Size and resolution of the nav mesh
    private Vector2 GridSize = Vector2.zero;    //Length and width of the nav mesh
    private float CellSize = 0.5f;   //Size of nodes on the graph

    //Matrix of nodes which made up the navigation mesh
    public List<List<MeshNode>> NodeGraph = new List<List<MeshNode>>();

    //Toggles if the navmesh is visible for debugging purposes
    public bool VisibleNavMesh = false;

    //Set once the nav mesh generation has been completed, level cannot start until this happens
    public bool NavMeshReady = false;

    //Initialised the navmesh, sets up 2D array of nodes which define the whole navmesh grid to be used for pathfinding
    public void GenerateNavMesh()
    {
        //Find the level size, and use that with the desired mesh resolution to find the actual grid size
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;
        float LevelWidth = LevelBorders.Instance.LevelWidth;
        float LevelHeight = LevelBorders.Instance.LevelHeight;
        GridSize.x = Mathf.CeilToInt(LevelWidth / CellSize);
        GridSize.y = Mathf.CeilToInt(LevelHeight / CellSize);

        //Generate a grid of mesh nodes which will make up our nav mesh
        for(int x = 0; x < GridSize.x; x++)
        {
            //Initialize the row of the grid
            NodeGraph.Add(new List<MeshNode>());

            for(int y = 0; y < GridSize.y; y++)
            {
                //Initialize the column of the grid
                MeshNode NewNode = new MeshNode();

                //Get the world position of the new node
                NewNode.NodePos = new Vector3(
                    XBounds.x + x * CellSize,
                    YBounds.x + y * CellSize,
                    0f);

                //Add it into the grid
                NodeGraph[x].Add(NewNode);

                //Make them visible if needed
                if(VisibleNavMesh)
                {
                    string NodeName = "NavNode " + x + ", " + y;
                    NewNode.InitRenderer(NodeName);
                }

                //Also, they should know their position in the array
                NewNode.GridPosition = new Vector2(x, y);
            }
        }

        NavMeshReady = true;
    }

    //Returns the navmesh in the grid closest to the given world position
    public MeshNode GetNodeFromWorldPos(Vector3 WorldPos)
    {
        //Get the level bounds as we need to clamp the given position inside it
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;

        //Clamp the world position inside the grid bounds
        float XClamped = Mathf.Clamp(WorldPos.x, XBounds.x, XBounds.y);
        float YClamped = Mathf.Clamp(WorldPos.y, YBounds.x, YBounds.y);

        //Convert clamped position into grid coordinates
        int XIndex = Mathf.FloorToInt((XClamped - XBounds.x) / CellSize);
        int YIndex = Mathf.FloorToInt((YClamped - YBounds.x) / CellSize);

        //Clamp indices to make sure we don't go out of bounds
        XIndex = Mathf.Clamp(XIndex, 0, NodeGraph.Count - 1);
        YIndex = Mathf.Clamp(YIndex, 0, NodeGraph[0].Count - 1);

        //Return the given node graph
        return NodeGraph[XIndex][YIndex];
    }
}