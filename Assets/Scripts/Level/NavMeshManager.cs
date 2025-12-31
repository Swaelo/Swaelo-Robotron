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
    public static NavMeshManager Instance = null; //Singleton instance
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

    //Properties of the position, size of the navmesh
    float MeshWidth = 0f;
    float MeshHeight = 0f;
    float HalfMeshWidth = 0f;
    float HalfMeshHeight = 0f;

    //Setups up the navmesh, doesnt pay attention to the size of the level bounds, allowing the playable area to be expanded outside the original bounds
    public void GenerateNavMesh()
    {
        //Find the size of the nav mesh and find the spawning offsets so the center is at 0,0
        MeshWidth = GridSize.x * CellSize;
        HalfMeshWidth = MeshWidth * 0.5f;
        MeshHeight = GridSize.y * CellSize;
        HalfMeshHeight = MeshHeight * 0.5f;
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
                Vector3 NodePos = new Vector3(
                XOffset + MeshX * CellSize,
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

    //Sets the walkability toggle for nodes on the nav mesh grid
    public void MarkNodesUnderBox(BoxCollider2D Box, bool Walkable)
    {
        //Get the bounds of the collider
        Bounds BoxBounds = Box.bounds;

        //Expand the box outwards to catch edge nodes + safety margin
        float BufferSize = CellSize * 0.75f;
        BoxBounds.min -= new Vector3(BufferSize, BufferSize, 0f);
        BoxBounds.max += new Vector3(BufferSize, BufferSize, 0f);

        //Calculate the total navmesh world extents
        Vector2 NavMeshMin = new Vector2(-HalfMeshWidth, -HalfMeshHeight);
        Vector2 NavMeshMax = new Vector2(HalfMeshWidth, HalfMeshHeight);

        //Overlapping rect in world space
        float OverlapMinX = Mathf.Max(BoxBounds.min.x, NavMeshMin.x);
        float OverlapMinY = Mathf.Max(BoxBounds.min.y, NavMeshMin.y);
        float OverlapMaxX = Mathf.Min(BoxBounds.max.x, NavMeshMax.x);
        float OverlapMaxY = Mathf.Min(BoxBounds.max.y, NavMeshMax.y);
        
        //If there is no overlap with the navmesh at all, exit out
        if(OverlapMinX >= OverlapMaxX || OverlapMinY >= OverlapMaxY)
            return;

        //Convert to grid indices
        //Since node (x,y) covers [x*CellSize - HalfMeshWidth, (x+1)*CellSize - HalfMeshWidth]
        //We add HalfMeshWidth/Height to shift into positive space before dividing
        int MinGridX = Mathf.FloorToInt((OverlapMinX + HalfMeshWidth) / CellSize);
        int MinGridY = Mathf.FloorToInt((OverlapMinY + HalfMeshHeight) / CellSize);
        int MaxGridX = Mathf.FloorToInt((OverlapMaxX + HalfMeshWidth - 0.0001f) / CellSize);
        int MaxGridY = Mathf.FloorToInt((OverlapMaxY + HalfMeshHeight - 0.0001f) / CellSize);

        //Clamp to grid bounds
        MinGridX = Mathf.Max(0, MinGridX);
        MinGridY = Mathf.Max(0, MinGridY);
        MaxGridX = Mathf.Min((int)GridSize.x - 1, MaxGridX);
        MaxGridY = Mathf.Min((int)GridSize.y - 1, MaxGridY);

        //Safety check in case clamping inverted the range
        if(MinGridX > MaxGridX || MinGridY > MaxGridY)
            return;

        //Now mark the nodes
        for(int MeshX = MinGridX; MeshX <= MaxGridX; MeshX++)
        {
            for(int MeshY = MinGridY; MeshY <= MaxGridY; MeshY++)
            {
                MeshNode Node = NodeGraph[MeshX][MeshY];
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

    //Finds a pathway from the start to end while treating nodes near the player as unwalkable
    public List<MeshNode> FindPathwayAvoidingPlayer(Vector3 StartPos, Vector3 EndPos, float AvoidanceRadius = 2f)
    {
        //Get the start and end nodes
        MeshNode StartNode = GetWalkableNodeFromWorldPos(StartPos);
        MeshNode EndNode = GetWalkableNodeFromWorldPos(EndPos);

        //Return empty if no pathway is possible
        if(!StartNode.IsWalkable || !EndNode.IsWalkable)
            return new List<MeshNode>();

        //Find all the players information
        Vector3 PlayerPos = GameState.Instance.Player.transform.position;
        MeshNode PlayerNode = GetNodeFromWorldPos(PlayerPos);
        int PlayerGridX = (int)PlayerNode.GridPosition.x;
        int PlayerGridY = (int)PlayerNode.GridPosition.y;

        //Calculate an avoidance area around the player
        int SearchRadius = Mathf.CeilToInt((AvoidanceRadius * 1.5f) / CellSize);

        //We will temporarily mark nodes near the player as unwalkable, we need to store their states so we can set them back to normal after we are finish
        Dictionary<MeshNode, bool> OriginalStates = new Dictionary<MeshNode, bool>();

        //Mark nearby nodes as unwalkable (skipping start and target nodes to ensure valid endpoints)
        for(int NodeX = -SearchRadius; NodeX <= SearchRadius; NodeX++)
        {
            for(int NodeY = -SearchRadius; NodeY <= SearchRadius; NodeY++)
            {
                //Get the nodes position
                int NodePosX = PlayerGridX + NodeX;
                int NodePosY = PlayerGridY + NodeY;

                //Make sure they are valid nodes
                if(NodePosX >= 0 && NodePosX < (int)GridSize.x && NodePosY >= 0 && NodePosY < (int)GridSize.y)
                {
                    //Grab this node
                    MeshNode Node = NodeGraph[NodePosX][NodePosY];

                    //Skip the target / target nodes
                    if(Node == StartNode || Node == EndNode)
                        continue;

                    //Check if this node lies within the unwalkable area
                    if (Vector3.Distance(Node.NodePos, PlayerPos) <= AvoidanceRadius)
                    {
                        //This node is too close to the player
                        //Store its original position and mark it was unwalkable
                        OriginalStates[Node] = Node.IsWalkable;
                        Node.SetWalkable(false);
                    }
                }
            }
        }

        //Now all nodes near the player have been marked unwalkable, lets find a path to our hiding spot
        List<MeshNode> NodePathway = FindPathway(StartPos, EndPos);

        //Restore original states of all nodes
        foreach(var Node in OriginalStates)
            Node.Key.SetWalkable(Node.Value);

        return NodePathway;
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

    //Checks if there is a walkable path from the start node to the target node
    public bool IsReachable(MeshNode Start, MeshNode End)
    {
        //Early out cases
        if(Start == null || End == null) return false;
        if(!Start.IsWalkable || !End.IsWalkable) return false;
        if(Start == End) return true;

        //Get through to find a path
        HashSet<MeshNode> NodesVisited = new HashSet<MeshNode>();
        Queue<MeshNode> NodeQueue = new Queue<MeshNode>();
        NodeQueue.Enqueue(Start);
        NodesVisited.Add(Start);

        while(NodeQueue.Count > 0)
        {
            //Get the next node to check along the pathway
            MeshNode CurrentNode = NodeQueue.Dequeue();

            //Check if we have found a path to the location
            if(CurrentNode == End)
                return true;
            
            //This isnt the path, add all its neighbours so we can search them instead
            foreach(MeshNode Neighbour in GetNeighbours(CurrentNode))
            {
                //Only add walkable neighbours that we haven't checked yet
                if(Neighbour.IsWalkable && !NodesVisited.Contains(Neighbour))
                {
                    NodesVisited.Add(Neighbour);
                    NodeQueue.Enqueue(Neighbour);
                }
            }
        }

        //Not pathway could be found
        return false;
    }
}