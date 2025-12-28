// ================================================================================================================================
// File:        NavMeshManager.cs
// Description:	Creates a dynamic navigation mesh every time a new wave begins and the enemies have finished being spawned in
// Certain enemies will use this for their navigation through the level
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class NavMeshManager : MonoBehaviour
{
    public static NavMeshManager Instance; //Singleton instance
    private void Awake() { Instance = this; }

    //Size and resolution of the nav mesh
    public Vector2 GridSize = Vector2.zero;    //Length and width of the nav mesh
    public float CellSize = 0.25f;   //Size of nodes on the graph

    //Matrix of nodes which made up the navigation mesh
    public List<List<MeshNode>> NodeGraph = new List<List<MeshNode>>();

    //Toggles if the navmesh is visible for debugging purposes
    public bool VisibleNavMesh = false;

    //Set once the nav mesh generation has been completed, level cannot start until this happens
    public bool NavMeshReady = false;

    //Setups up the navmesh, doesnt pay attention to the size of the level bounds, allowing the playable area to be expanded outside the original bounds
    public void NewGenerateNavMesh()
    {
        //Find the size of the nav mesh and find the spawning offsets so the center is at 0,0
        float MeshWidth = GridSize.x * CellSize;
        float MeshHeight = GridSize.y * CellSize;
        float XOffset = -MeshWidth * .5f;
        float YOffset = -MeshHeight * .5f;

        //Setup 2d array of mesh nodes for the navmesh
        for(int MeshX = 0; MeshX < GridSize.x; MeshX++)
        {
            //Initialize each row of the nav mesh
            NodeGraph.Add(new List<MeshNode>());

            for(float MeshY = 0f; MeshY < GridSize.y; MeshY++)
            {
                //Initialize each mesh node in the column
                MeshNode NewNode = new MeshNode();
                //Set its position
                Vector3 NodePos = new Vector3(XOffset + MeshX * CellSize,
                YOffset + MeshY * CellSize, 0f);
                Vector2 GridPos = new Vector2(MeshX, MeshY);
                NewNode.SetPosition(NodePos, GridPos);
                //Add it to the 2d array
                NodeGraph[MeshX].Add(NewNode);

                //Set it to visible if needed
                if(VisibleNavMesh)
                    NewNode.InitRenderer("NavMeshNode: " + MeshX + ", " + MeshY, transform);
            }
        }
        NavMeshReady = true;
    }

    //Initialised the navmesh, sets up 2D array of nodes which define the whole navmesh grid to be used for pathfinding
    public void GenerateNavMesh()
    {
        //Find the level size, and use that with the desired mesh resolution to find the actual grid size
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;
        float LevelWidth = LevelBorders.Instance.GetUseableLevelWidth();
        float LevelHeight = LevelBorders.Instance.GetUseableLevelHeight();
        GridSize.x = Mathf.CeilToInt(LevelWidth / CellSize);
        GridSize.y = Mathf.CeilToInt(LevelHeight / CellSize);

        //Generate a grid of mesh nodes which will make up our nav mesh
        for(int x = 0; x < GridSize.x; x++)
        {
            //Initialize the row of the grid
            NodeGraph.Add(new List<MeshNode>());

            for(int y = 0; y < GridSize.y; y++)
            {
                //Initialize the node in this column of the grid
                MeshNode NewNode = new MeshNode();

                //Set the position of each node at the center of the cell it defines
                NewNode.NodePos = new Vector3(
                    XBounds.x + (x + 0.5f) * CellSize,
                    YBounds.x + (y + 0.5f) * CellSize, 0f);

                //Add it into the grid
                NodeGraph[x].Add(NewNode);

                //Make them visible if needed
                if(VisibleNavMesh)
                {
                    //Name and parent it to keep the scene hierarchy clean
                    string NodeName = "NavNode " + x + ", " + y;
                    NewNode.InitRenderer(NodeName, transform);
                }

                //Also, they should know their position in the array
                NewNode.GridPosition = new Vector2(x, y);
            }
        }

        NavMeshReady = true;
    }

    //Reset all nodes to walkable
    public void ResetNodes()
    {
        foreach(List<MeshNode> List in NodeGraph)
        {
            foreach(MeshNode Node in List)
            {
                Node.SetWalkable(true);
            }
        }
    }

    //Sets any nodes underneath electrodes as unwalkable
    public void MarkElectrodeNodesUnwalkable()
    {
        //Electrons make unwalkable areas on the navmesh, grab all the electrons and set the nodes they occupy as not walkable
        List<BaseEntity> Electrodes = WaveManager.Instance.GetEntityList(EntityType.Electrode);
        foreach(BaseEntity Electrode in Electrodes)
            MarkNodesUnderEntity(Electrode, false);
    }

    //Marks any mesh nodes under the passed entity as not walkable
    public void MarkNodesUnderEntity(BaseEntity Entity, bool Walkable)
    {
        //Get the box collider from this entitys gameobject
        BoxCollider2D EntityCollider = Entity.transform.GetComponent<BoxCollider2D>();
        if(EntityCollider == null)
        {
            Debug.LogWarning("Entity has no box collider I can find.");
            return;
        }

        //Get the bounds of the collider in world space
        Bounds EntityBounds = EntityCollider.bounds;

        //Loop through all the ndoes in the nav mesh
        for (int x = 0; x < NodeGraph.Count; x++)
        {
            for(int y = 0; y < NodeGraph[x].Count; y++)
            {
                //Get each mesh node we will be checking
                MeshNode Node = NodeGraph[x][y];

                //Calculate the nodes world space bounds
                Bounds NodeBounds = new Bounds(Node.NodePos, new Vector3(CellSize * 2, CellSize * 2, 0f));
                
                //Mark the node as unwalkable if the entity bounds intersect
                if(NodeBounds.Intersects(EntityBounds))
                    Node.SetWalkable(Walkable);
            }
        }
    }

    //Returns a list of any mesh nodes which are under the given box collider
    public List<MeshNode> GetNodesUnderBox(BoxCollider2D Box)
    {
        //Create a list to store the nodes under the collider
        List<MeshNode> Nodes = new List<MeshNode>();

        //Get the bounds of the collider in world space
        Bounds BoxBounds = Box.bounds;

        //Loop through all the nodes in the nav mesh
        for(int x = 0; x < NodeGraph.Count; x++)
        {
            for(int y = 0; y < NodeGraph[x].Count; y++)
            {
                //Get the node we will be checking and its bounds
                MeshNode Node = NodeGraph[x][y];
                Bounds NodeBounds = new Bounds(Node.NodePos, new Vector3(CellSize * 2, CellSize * 2, 0f));
                
                //If this node intersects with the given box collider, add it to our list
                if(NodeBounds.Intersects(BoxBounds))
                    Nodes.Add(Node);
            }
        }

        //Return the final list of nodes
        return Nodes;
    }

    //Returns the MeshNode in the grid closest to the given world position
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

    //Returns the walkable MeshNode in the grid closest to the given world position
    public MeshNode GetWalkableNodeFromWorldPos(Vector3 WorldPos)
    {
        //Clamp the world position inside the level bounds
        Vector2 XBounds = LevelBorders.Instance.XBounds;
        Vector2 YBounds = LevelBorders.Instance.YBounds;
        float XClamped = Mathf.Clamp(WorldPos.x, XBounds.x, XBounds.y);
        float YClamped = Mathf.Clamp(WorldPos.y, YBounds.x, YBounds.y);

        //Convert clamped position into grid coordinates
        int XIndex = Mathf.FloorToInt((XClamped - XBounds.x) / CellSize);
        int YIndex = Mathf.FloorToInt((YClamped - YBounds.x) / CellSize);

        //Clamp indices so we dont go out of bounds
        XIndex = Mathf.Clamp(XIndex, 0, NodeGraph.Count - 1);
        YIndex = Mathf.Clamp(YIndex, 0, NodeGraph[0].Count - 1);

        //If the node is already walkable, return it
        if(NodeGraph[XIndex][YIndex].IsWalkable)
            return NodeGraph[XIndex][YIndex];

        //Otherwise, search neighbours in an expanding square radius
        int MaxRadius = Mathf.Max((int)GridSize.x, (int)GridSize.y);
        for(int Radius = 1; Radius < MaxRadius; Radius++)
        {
            //Loop through nodes in the radius
            for(int DirectionX = -Radius; DirectionX <= Radius; DirectionX++)
            {
                for(int DirectionY = -Radius; DirectionY <= Radius; DirectionY++)
                {
                    //Get the index of the node we are about to check
                    int NodeX = XIndex + DirectionX;
                    int NodeY = YIndex + DirectionY;

                    //Skip nodes which dont exist, outside bounds of the array
                    if(NodeX < 0 || NodeX >= (int)GridSize.x ||
                        NodeY < 0 || NodeY >= (int)GridSize.y)
                        continue;

                    //Grab the actual neighbour, if its walkable we return that one
                    MeshNode Neighbour = NodeGraph[NodeX][NodeY];
                    if(Neighbour.IsWalkable)
                        return Neighbour;
                }
            }
        }

        //As a fallback, we return the original node, even if its not walkable
        return NodeGraph[XIndex][YIndex];
    }

    //Takes two locations and returns a list of mesh nodes which forms a pathway from one location to the other
    public List<MeshNode> FindPathway(Vector3 Start, Vector3 End)
    {
        //Find the mesh node closest to our start and end locations
        MeshNode PathStart = GetWalkableNodeFromWorldPos(Start);
        MeshNode PathEnd = GetWalkableNodeFromWorldPos(End);

        //Initialise the open and closed lists
        List<MeshNode> OpenList = new List<MeshNode>(); //Nodes that still need to be evaluated
        List<MeshNode> ClosedList = new List<MeshNode>();   //Nodes already evaluated

        //Start by putting the start node into the open list
        PathStart.GCost = 0f;
        PathStart.HCost = ComputeHeuristic(PathStart, PathEnd);
        PathStart.FCost = PathStart.HCost;
        OpenList.Add(PathStart);

        //Loop through the open list until its empty
        while(OpenList.Count > 0)
        {
            //Take the node with the lowest F Score
            MeshNode NextNode = GetCheapestFCost(OpenList);
            
            //If this is the goal node, we have found our pathway
            if(NextNode == PathEnd)
                return(ConstructPathway(PathStart, PathEnd));

            //Otherwise, we move it to the closed list
            OpenList.Remove(NextNode);
            ClosedList.Add(NextNode);

            //Check all of the nodes neighbours
            List<MeshNode> Neighbours = GetNeighbours(NextNode);
            foreach(MeshNode Neighbour in Neighbours)
            {
                //Skip it if its already in the closed list, or its not able to be walked upon
                if(!Neighbour.IsWalkable || ClosedList.Contains(Neighbour))
                    continue;

                //Calculate cost to travel from the current node to this neighbour
                float TentativeGCost = NextNode.GCost + ComputeDistance(NextNode, Neighbour);

                //Check if this is a cheaper direction of travel
                if(TentativeGCost < Neighbour.GCost || !OpenList.Contains(Neighbour))
                {
                    //Calculate new travel costs for this node, and parent it to the current node
                    Neighbour.GCost = TentativeGCost;
                    Neighbour.HCost = ComputeHeuristic(Neighbour, PathEnd);
                    Neighbour.FCost = Neighbour.GCost + Neighbour.HCost;
                    Neighbour.Parent = NextNode;
                    //Add it to the open list if its not already there
                    if(!OpenList.Contains(Neighbour))
                        OpenList.Add(Neighbour);
                }
            }
        }

        //Unable to find any pathway, return an empty list
        return new List<MeshNode>();
    }

    //Returns a list of the given nodes neighbours
    private List<MeshNode> GetNeighbours(MeshNode Node)
    {
        //Start a list to contain this nodes neighbours
        List<MeshNode> Neighbours = new List<MeshNode>();

        //Get the nodes position in the grid
        int GridX = (int)Node.GridPosition.x;
        int GridY = (int)Node.GridPosition.y;

        //Loop through all surrounding nodes (-1, 0, +1 in each direction)
        for (int DirectionX = -1; DirectionX <= 1; DirectionX++)
        {
            for (int DirectionY = -1; DirectionY <= 1; DirectionY++)
            {
                //Skip the node itself
                if(DirectionX == 0 && DirectionY == 0)
                    continue;

                //Get the grid pos of the potential neighbour
                int NodeX = GridX + DirectionX;
                int NodeY = GridY + DirectionY;

                //Make sure this neighbour exists, before adding it into the list of neighbours
                if(NodeX >= 0 && NodeX < (int)GridSize.x &&
                    NodeY >= 0 && NodeY < (int)GridSize.y)
                    Neighbours.Add(NodeGraph[NodeX][NodeY]);
            }
        }

        return Neighbours;
    }

    //Constructs the pathway that was found and returns it as a list
    private List<MeshNode> ConstructPathway(MeshNode Start, MeshNode End)
    {
        List<MeshNode> Pathway = new List<MeshNode>();
        MeshNode Current = End;

        while (Current != Start)
        {
            Pathway.Add(Current);
            Current = Current.Parent;
        }

        Pathway.Reverse();
        return Pathway;
    }

    //Returns the node in the list with the lowest FCost option
    private MeshNode GetCheapestFCost(List<MeshNode> OpenList)
    {
        MeshNode CheapestNode = OpenList[0];
        for(int i = 1; i < OpenList.Count; i++)
        {
            if(OpenList[i].FCost < CheapestNode.FCost)
                CheapestNode = OpenList[i];
        }
        return CheapestNode;
    }

    //Calculates the heuristic cost between two nodes
    //h = \sqrt{(x_1-x_2)^2 + (y_1-y_2)^2}
    private float ComputeHeuristic(MeshNode Start, MeshNode Target)
    {
        float XDistance = Mathf.Abs(Start.NodePos.x - Target.NodePos.x);
        float YDistance = Mathf.Abs(Start.NodePos.y - Target.NodePos.y);
        float StraightTravelCost = 10f;
        float DiagonalTravelCost = 14f;
        return StraightTravelCost * (XDistance + YDistance) + (DiagonalTravelCost - 2 * StraightTravelCost) * Mathf.Min(XDistance, YDistance);
    }

    //Distance between two nodes (straight:10, diagonal:14)
    private float ComputeDistance(MeshNode Start, MeshNode End)
    {
        float DirectionX = Mathf.Abs(Start.GridPosition.x - End.GridPosition.x);
        float DirectionY = Mathf.Abs(Start.GridPosition.y - End.GridPosition.y);

        if(DirectionX > DirectionY)
            return 14 * DirectionY + 10 * (DirectionX - DirectionY);
        return 14 * DirectionX + 10 * (DirectionY - DirectionX);
    }
}