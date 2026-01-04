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

    //Parent of all level geometry objects which needs to be marked on the navmesh
    public GameObject LevelGeometry = null;

    //Setups up the navmesh, doesnt pay attention to the size of the level bounds, allowing the playable area to be expanded outside the original bounds
    public void GenerateNavMesh()
    {
        //Reset / clear any existing mesh before we try generating again
        NavMeshReady = false;
        NodeGraph.Clear();

        //Calculate grid dimentions
        MeshWidth = GridSize.x * CellSize;
        HalfMeshWidth = MeshWidth * 0.5f;
        MeshHeight = GridSize.y * CellSize;
        HalfMeshHeight = MeshHeight * 0.5f;

        //Grid is centered around (0,0). These are the world-space mins (bottom-left corner of the grid)
        float XOffset = -HalfMeshWidth;
        float YOffset = -HalfMeshHeight;

        //Build the grid
        for(int MeshX = 0; MeshX < GridSize.x; MeshX++)
        {
            //Initialize each row of the nav mesh
            NodeGraph.Add(new List<MeshNode>());

            for(float MeshY = 0f; MeshY < GridSize.y; MeshY++)
            {
                //Initialize each mesh node in the column
                MeshNode NewNode = new MeshNode();

                //Place nodes at the center of each cell
                float WorldX = XOffset + (MeshX + 0.5f) * CellSize;
                float WorldY = YOffset + (MeshY + 0.5f) * CellSize;
                Vector3 NodePos = new Vector3(WorldX, WorldY, 0f);

                //Set its world position
                Vector2 GridPos = new Vector2(MeshX, MeshY);
                NewNode.SetPosition(NodePos, GridPos);

                //Add it to the grid
                NodeGraph[MeshX].Add(NewNode);

                //Set it to visible if needed
                if(VisibleNavMesh)
                    NewNode.InitRenderer("NavMeshNode: " + MeshX + ", " + MeshY, transform);
            }
        }

        //Mark all nodes under level geometry as unwalkable
        MarkGeometryUnwalkable();

        NavMeshReady = true;
    }

    //Sets all nodes underneath any level geometry as unwalkable
    private void MarkGeometryUnwalkable()
    {
        //Exit out if we have no level geometry to iterate over
        if(LevelGeometry == null)
            return;

        //Grab all the colliders and mark them all unwalkable
        BoxCollider2D[] LevelColliders = LevelGeometry.GetComponentsInChildren<BoxCollider2D>();
        foreach(BoxCollider2D Collider in LevelColliders)
            MarkNodesUnderBox(Collider, false);
    }

    //Reset all nodes to walkable
    public void SetAllNodesWalkable(bool Walkable)
    {
        foreach(MeshNode Node in GetAllNodes())
            Node.SetWalkable(Walkable);
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
            T.Log("Entity has no box collider I can find.");
            return;
        }

        //Get the bounds of the collider in world space
        Bounds EntityBounds = EntityCollider.bounds;

        //Loop through all the nodes to find which ones are under the entity
        foreach(MeshNode Node in GetAllNodes())
        {
            //Calculate the nodes world space bounds
            Bounds NodeBounds = new Bounds(Node.NodePos, new Vector3(CellSize * 2, CellSize * 2, 0f));
            //Mark it as unwalkable it the entity bounds intersect
            if(NodeBounds.Intersects(EntityBounds))
                Node.SetWalkable(Walkable);
        }
    }

    //Toggles the walkability of any navmesh nodes under the given box collider object
    public void MarkNodesUnderBox(BoxCollider2D Box, bool Walkable)
    {
        //Get the AABB bounds of the box, used to quickly figure out which nodes on the navmesh to check
        //instead of having to scan the entire grid
        Bounds BoxBounds = Box.bounds;

        //Expand the bounds slightly so we still catch nodes that lie very close to the edges
        float ColliderBuffer = CellSize * 0.75f;
        BoxBounds.Expand(new Vector3(ColliderBuffer * 2f, ColliderBuffer * 2f, 0f));

        //Define the navmesh world extends
        Vector2 NavMin = new Vector2(-HalfMeshWidth, -HalfMeshHeight);
        Vector2 NavMax = new Vector2(HalfMeshWidth, HalfMeshHeight);

        //Compute the overlapping region between the collider bounds and the navmesh world area
        float OverlapMinX = Mathf.Max(BoxBounds.min.x, NavMin.x);
        float OverlapMinY = Mathf.Max(BoxBounds.min.y, NavMin.y);
        float OverlapMaxX = Mathf.Min(BoxBounds.max.x, NavMax.x);
        float OverlapMaxY = Mathf.Min(BoxBounds.max.y, NavMax.y);

        //If there is no overlap at all nothing needs to be done
        if(OverlapMinX >= OverlapMaxX || OverlapMinY >= OverlapMaxY)
            return;

        //Convert world-space overlap coordinates into grid indices
        int MinX = Mathf.FloorToInt((OverlapMinX + HalfMeshWidth) / CellSize);
        int MinY = Mathf.FloorToInt((OverlapMinY + HalfMeshHeight) / CellSize);
        int MaxX = Mathf.FloorToInt((OverlapMaxX + HalfMeshWidth - 0.0001f) / CellSize);
        int MaxY = Mathf.FloorToInt((OverlapMaxY + HalfMeshHeight - 0.0001f) / CellSize);

        //Clamp indices to ensure they stay within navmesh array bounds
        MinX = Mathf.Clamp(MinX, 0, (int)GridSize.x -1);
        MinY = Mathf.Clamp(MinY, 0, (int)GridSize.y - 1);
        MaxX = Mathf.Clamp(MaxX, 0, (int)GridSize.x - 1);
        MaxY = Mathf.Clamp(MaxY, 0, (int)GridSize.y - 1);

        //Safety check in case clamping inverted the range
        if(MinX > MaxX || MinY > MaxY)
            return;
        
        //Iterate over candidate nodes
        for(int CheckX = MinX; CheckX <= MaxX; CheckX++)
        {
            for(int CheckY = MinY; CheckY <= MaxY; CheckY++)
            {
                //Check if the center of each mesh cell lies within the box collider
                MeshNode CheckNode = NodeGraph[CheckX][CheckY];
                if(Box.OverlapPoint(CheckNode.NodePos))
                    CheckNode.SetWalkable(Walkable);
            }
        }
    }

    //Returns the MeshNode in the grid closest to the given world position
    public MeshNode GetNodeFromWorldPos(Vector3 WorldPos)
    {
        //Break out if the navmesh doesnt exist for some reason
        if(NodeGraph == null || NodeGraph.Count == 0 || NodeGraph[0].Count == 0)
            return null;

        //Calculate offset as navmesh is centered in the middle of the game world
        float XOffset = -MeshWidth * 0.5f;
        float YOffset = -MeshHeight * 0.5f;

        //Convert world -> grid indices (shift into positive space first)
        int XIndex = Mathf.FloorToInt((WorldPos.x - XOffset) / CellSize);
        int YIndex = Mathf.FloorToInt((WorldPos.y - YOffset) / CellSize);

        //Clamp to array bounds
        XIndex = Mathf.Clamp(XIndex, 0, NodeGraph.Count -1);
        YIndex = Mathf.Clamp(YIndex, 0, NodeGraph[0].Count -1);

        return NodeGraph[XIndex][YIndex];
    }

    //Returns the first walkable node encountered in this ring-walk order
    public MeshNode GetNearestWalkableNode(Vector3 TargetPos, float MaxRadius = 5f)
    {
        //Get the node closest to our target location, this is our search center
        MeshNode TargetNode = GetNodeFromWorldPos(TargetPos);

        //If this starting node is already walkable, we're done
        if(TargetNode.IsWalkable)
            return TargetNode;
        
        //Quick out if node starting node could be found
        if(TargetNode == null)
        {
            T.Log("GetNearestWalkableNode could not find a starting node from " + TargetPos);
            return null;
        }

        //Cache the integer grid coordinates of the target node for quicker math
        int NodeX = (int)TargetNode.GridPosition.x;
        int NodeY = (int)TargetNode.GridPosition.y;

        //Expand an outward ring to search for a walkable node
        //Each radius form a square ring around the TargetNode, bigger each iteration
        for (int Radius = 1; Radius <= MaxRadius; Radius++)
        {
            //Check the top and bottom edges along this ring
            for(int RingX = -Radius; RingX <= Radius; RingX++)
            {
                //Coordinates to search along this time
                int CheckX = NodeX + RingX;
                int YTop = NodeY + Radius;
                int YBot = NodeY - Radius;

                //Bounds check X, if its outside the grid skip both top and bottom checks
                if(CheckX >= 0 && CheckX < (int)GridSize.x)
                {
                    //Check the top edge cell if top is in bounds
                    if(YTop >= 0 && YTop < (int)GridSize.y && NodeGraph[CheckX][YTop].IsWalkable)
                        return NodeGraph[CheckX][YTop];

                    //Check the bottom edge cell if yBot is in bounds
                    if(YBot >= 0 && YBot < (int)GridSize.y && NodeGraph[CheckX][YBot].IsWalkable)
                        return NodeGraph[CheckX][YBot];
                }
            }

            //Check the left and right edges for this ring
            for (int RingY = -Radius + 1; RingY <= Radius - 1; RingY++)
            {
                //Coordinates to check along this time
                int CheckY = NodeY + RingY;
                int XLeft = NodeX - Radius;
                int XRight = NodeX + Radius;

                //If y is outside bounds, skip both left and right checks
                if(CheckY >= 0 && CheckY < (int)GridSize.y)
                {
                    //Check the left edge cell
                    if(XLeft >= 0 && XLeft < (int)GridSize.x && NodeGraph[XLeft][CheckY].IsWalkable)
                        return NodeGraph[XLeft][CheckY];
                    //Check the right edge cell
                    if(XRight >= 0 && XRight < (int)GridSize.x && NodeGraph[XRight][CheckY].IsWalkable)
                        return NodeGraph[XRight][CheckY];
                }
            }
            //If we reach here, this entire ring radius has no walkable nodes, continue on to the next one
        }
        //If this has completely failed and we couldn't find any node, we will try instead the more expensive and powerful version of this function
        return GetWalkableNodeFromWorldPos(TargetPos);
    }

    //Returns the walkable MeshNode in the grid closest to the given world position
    public MeshNode GetWalkableNodeFromWorldPos(Vector3 WorldPos)
    {
        T.Log("Initial walkable node search failed.");

        //Break out if the navmesh doesnt exist for some reason
        if(NodeGraph == null || NodeGraph.Count == 0 || NodeGraph[0].Count == 0)
            return null;

        //Calculate offset as navmesh is centered in the middle of the game world
        float XOffset = -MeshWidth * 0.5f;
        float YOffset = -MeshHeight * 0.5f;

        //Convert world -> grid indices (shift into positive space first)
        int XIndex = Mathf.FloorToInt((WorldPos.x - XOffset) / CellSize);
        int YIndex = Mathf.FloorToInt((WorldPos.y - YOffset) / CellSize);

        //Clamp to array bounds
        XIndex = Mathf.Clamp(XIndex, 0, NodeGraph.Count -1);
        YIndex = Mathf.Clamp(YIndex, 0, NodeGraph[0].Count -1);

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
        MeshNode StartNode = GetNearestWalkableNode(StartPos);
        MeshNode EndNode = GetNearestWalkableNode(EndPos);

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
        MeshNode PathStart = GetNearestWalkableNode(Start);
        MeshNode PathEnd = GetNearestWalkableNode(End);

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
            List<MeshNode> Neighbours = GetNeighbours(NextNode, false);
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
    private List<MeshNode> GetNeighbours(MeshNode Node, bool IncludeDiagonal = false)
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

                //If diagonals are not allowed, skip any offset that changes both axes
                if(!IncludeDiagonal && DirectionX != 0 && DirectionY != 0)
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
            foreach(MeshNode Neighbour in GetNeighbours(CurrentNode, false))
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

    //Returns a list of Vector3 locations surrounding the target location
    public List<Vector3> GetRingPositions(Vector3 RingMiddle, int PositionCount, float MinDistance = .5f, float MaxDistance = 5f)
    {
        //Find a list of mesh nodes in here
        List<MeshNode> RingNodes = FindSpawnNodes(RingMiddle, PositionCount, MinDistance, MaxDistance);

        //Create a list of vectors to hold their positions instead, then return that
        List<Vector3> RingPositions = new List<Vector3>();
        foreach(MeshNode Node in RingNodes)
            RingPositions.Add(Node.NodePos);
        return RingPositions;
    }

    //Returns a list of MeshNodes that match the following conditions
    //1. Each postition must be walkable on the navmesh
    //2. Each postition lies within the mix/max distance from the target location
    //3. Each postition must be reachable over the navmesh from the target location
    //4. The locations are attempted to be spread around the target location evenly in a circle
    //5. Final list is ordered by angle from the target location, so they appear in a circle order when being spawned in
    private List<MeshNode> FindSpawnNodes(Vector3 TargetLocation, int LocationCount, float MinDistance = .5f, float MaxDistance = 5f)
    {
        //List of found locations on the nav mesh
        List<MeshNode> SpawnLocations = new List<MeshNode>();

        //Figure out how many more locations we need to find
        int LocationsNeeded() { return LocationCount - SpawnLocations.Count; }

        //Return an empty list if we have been asked for 0 locations for some reasoin
        if(LocationCount == 0)
            return SpawnLocations;
            
        //Return an empty list if the navmesh isn't currently setup
        if(NodeGraph == null || NodeGraph.Count == 0 || NodeGraph[0].Count == 0)
            return SpawnLocations;

        //Ensure the min/max positions are valid
        MinDistance = Mathf.Max(0f, MinDistance);
        MaxDistance = Mathf.Max(MinDistance, MaxDistance);

        //Enforce a minimum band thickness in world units
        //This will help prevent radial samples collapsing on the same nodes
        float MinBand = CellSize * 2f;
        if (MaxDistance - MinDistance < MinBand)
        {
            float Middle = (MinDistance + MaxDistance) * 0.5f;
            MinDistance = Mathf.Max(0f, Middle - MinBand * 0.5f);
            MaxDistance = Middle + MinBand * 0.5f;
        }

        //Start from the closest walkable node near our target position
        MeshNode TargetNode = GetNearestWalkableNode(TargetLocation);
        //If the target position is in an unreachable location just exit out and return an empty list
        if(TargetNode == null || !TargetNode.IsWalkable)
            return SpawnLocations;
        
        //Track used nodes so we dont select the same ones twice
        HashSet<MeshNode> UsedNodes = new HashSet<MeshNode>();

        //Search along a ring between min and max distance
        float MidDistance = (MinDistance + MaxDistance) * 0.5f; //Middle of the allowed distance band
        float RingHalfWidth = (MaxDistance - MinDistance) * 0.5f; //Half-width of the ring

        //Set up angles and steps for walking through and searching for positions around the ring
        float AngleStep = 360f / LocationCount; //How many degrees we want to have between each location to place them in a nice ring
        float AngleSearchStepDegrees = 12f;  //How far to rotate when the exactle angle we are trying is blocked
        int RadialSteps = 24; //How many radial samples to take across the ring thickness
        float RadialStepSize = (RingHalfWidth * 2f) / Mathf.Max(1, RadialSteps - 1); //Distance between each radial sample

        //=============
        //===FIRST PASS
        //First pass, try to place spawn points evenly around the target
        for(int SpawnCounter = 0; SpawnCounter < LocationCount; SpawnCounter++)
        {
            //Find a node for each location
            float DesiredAngle = SpawnCounter * AngleStep;
            MeshNode Candidate = FindCandidateForAngle(DesiredAngle);

            //Add it to the list if its found to be valid
            if(Candidate != null)
            {
                UsedNodes.Add(Candidate);
                SpawnLocations.Add(Candidate);
            }
        }

        //=============
        //==SECOND PASS
        //Second pass, scan outward in a square/spiral around the target node in grid space
        if(SpawnLocations.Count < LocationCount)
        {
            //Get the grid limits we will start searching through instead
            int MaxGridRadius = Mathf.Max((int)GridSize.x, (int)GridSize.y);
            int TargetX = (int)TargetNode.GridPosition.x;
            int TargetY = (int)TargetNode.GridPosition.y;

            void TryAddingNode(int GridX, int GridY)
            {
                //Exit out if we don't need to find anymore
                if(LocationsNeeded() <= 0)
                    return;
                
                //Dont try searching outside the grid
                if(GridX < 0 || GridX >= (int)GridSize.x)
                    return;
                if(GridY < 0 || GridY >= (int)GridSize.y)
                    return;

                //Find a candidate, exit out if its invalid
                MeshNode Candidate = NodeGraph[GridX][GridY];
                if(!IsValidCandidate(Candidate))
                    return;

                //Otherwise we add it to our list
                UsedNodes.Add(Candidate);
                SpawnLocations.Add(Candidate);
            }

            //Walk square rings expanding outward from the target node
            for(int Radius = 1; Radius <= MaxGridRadius && LocationsNeeded() > 0; Radius++)
            {
                //Top and bottom edges
                for(int DegreesX = -Radius; DegreesX <= Radius && LocationsNeeded() > 0; DegreesX++)
                {
                    TryAddingNode(TargetX + DegreesX, TargetY + Radius);
                    TryAddingNode(TargetX + DegreesX, TargetY - Radius);
                }

                //Left and right edges
                for(int DirectionY = -Radius + 1; DirectionY <= Radius && LocationsNeeded() > 0; DirectionY++)
                {
                    TryAddingNode(TargetX - Radius, TargetY + DirectionY);
                    TryAddingNode(TargetX + Radius, TargetY + DirectionY);
                }
            }
        }

        //=============
        //===THIRD PASS
        //Third pass, expand max distance outward in steps and try again
        float OriginalMax = MaxDistance;
        float OriginalMin = MinDistance;
        float ExpandStep = 0.75f; //How far we will move outwards after each step
        float ExpandCap = OriginalMax + 15f;

        //Keep searching until we find some more locations
        while(LocationsNeeded() > 0 && MaxDistance < ExpandCap)
        {
            //Increase the search radius
            MaxDistance += ExpandStep;
            MinDistance = OriginalMin;

            //Setup search ring size parameters
            float BandSize = MaxDistance - MinDistance;
            MinDistance = (MinDistance + MaxDistance) * 0.5f;
            RingHalfWidth = BandSize / Mathf.Max(1, RadialSteps - 1);
            RadialStepSize = BandSize / Mathf.Max(1, RadialSteps - 1);

            //Check the locations in these directions with the new distance limitations
            for(int FarCheck = 0; FarCheck < LocationCount && LocationsNeeded() > 0; FarCheck++)
            {
                float DesiredAngle = FarCheck * AngleStep;
                MeshNode Candidate = FindCandidateForAngle(DesiredAngle, false, true);

                //Add this if its valid
                if(Candidate != null)
                    SpawnLocations.Add(Candidate);
            }
        }

        //=============
        //==FOURTH PASS
        //Fourth pass, allow duplicate locations
        if(SpawnLocations.Count < LocationCount)
        {
            //Try again, allowing duplicates but still respecting distance band + reachability
            for(int NonUniqueCheck = 0; NonUniqueCheck < LocationCount && LocationsNeeded() > 0; NonUniqueCheck++)
            {
                //Find a new desired node at the new angle, ignoring uniqueness constraints
                float DesiredAngle = NonUniqueCheck * AngleStep;
                MeshNode Candidate = FindCandidateForAngle(DesiredAngle, false, true);

                //Ignore any invalid / non found nodes
                if(Candidate != null)
                {
                    //Don't add to UsedNodes because we're explicitly allowing duplicates
                    SpawnLocations.Add(Candidate);
                }
            }
        }

        //=============
        //===FIFTH PASS
        //Fifth pass, ignore distance limitations
        if(LocationsNeeded() > 0)
        {
            //Do a global scan for any walkable+reachable nodes (ignoring distance)
            List<MeshNode> GlobalCandidates = new List<MeshNode>();
            for(int NodeX = 0; NodeX < NodeGraph.Count; NodeX++)
            {
                for(int NodeY = 0; NodeY < NodeGraph[NodeX].Count; NodeY++)
                {
                    //Check every node without limitations
                    MeshNode GlobalNode = NodeGraph[NodeX][NodeY];
                    if(IsValidCandidate(GlobalNode, false, false))
                        GlobalCandidates.Add(GlobalNode);
                }
            }

            //Sort them by distance to prefer all the closer nodes
            GlobalCandidates.Sort((NodeA, NodeB) =>
                Vector3.Distance(NodeA.NodePos, TargetLocation).CompareTo(Vector3.Distance(NodeB.NodePos, TargetLocation)));

            //If any have been found, add as many of them onto the list as possible until its full or we run out
            int CandidateCount = 0;
            while(LocationsNeeded() > 0 && GlobalCandidates.Count > 0)
            {
                SpawnLocations.Add(GlobalCandidates[CandidateCount % GlobalCandidates.Count]);
                CandidateCount++;
            }
            
        }

        //=============
        //===FINAL PASS
        //Final pass, duplicate the target node until the list is full
        while(SpawnLocations.Count < LocationCount)
            SpawnLocations.Add(TargetNode);

        //Sort the spawn positions by angle around the target location
        SpawnLocations.Sort((SpawnA, SpawnB) =>
        {
            Vector3 SpawnAPos = SpawnA.NodePos;
            Vector3 SpawnBPos = SpawnB.NodePos;
            float AngleA = Mathf.Atan2(SpawnAPos.y - TargetLocation.y, SpawnAPos.x - TargetLocation.x);
            float AngleB = Mathf.Atan2(SpawnBPos.y - TargetLocation.y, SpawnBPos.x - TargetLocation.x);
            return AngleA.CompareTo(AngleB);
        });

        //Ensure we never exceed the request amount of requested locations
        if(SpawnLocations.Count > LocationCount)
            SpawnLocations.RemoveRange(LocationCount, SpawnLocations.Count - LocationCount);

        //Return the finalised list
        return SpawnLocations;

        //Checks whether a mesh node can be used as a spawn location
        bool IsValidCandidate(MeshNode Candidate, bool EnforceUniqueLocations = true, bool EnforceDistanceParameters = true)
        {
            //Must exist
            if(Candidate == null)
                return false;
            
            //Must be walkable
            if(!Candidate.IsWalkable)
                return false;

            //Must not already be used
            if(EnforceUniqueLocations && UsedNodes.Contains(Candidate))
                return false;

            //Must be within distance bounds
            if(EnforceDistanceParameters)
            {
                float Distance = Vector3.Distance(Candidate.NodePos, TargetLocation);
                if(Distance < MinDistance || Distance > MaxDistance)
                    return false;
            }

            //Must be reachable from the taget over the navmesh
            if(!IsReachable(TargetNode, Candidate))
                return false;

            //All tests have passed, this node is valid
            return true;
        }

        //Attempts to find a valid node near a desired angle by:
            //Rotating slightly left/right if blocked
            //Sampling inward/outward across the ring thicness
        MeshNode FindCandidateForAngle(float DesiredAngleDegrees, bool EnforceUniqueLocations = true, bool EnforceDistanceParameters = true)
        {
            //Maximum number of angular offest to try (up to half a circle)
            int MaxAngleSteps = 8; //Mathf.Max(1, Mathf.CeilToInt(180f / Mathf.Max(0.1f, AngleSearchStepDegrees)));

            //Angular search: 0, +step, -step, +2step, -2step, ...
            for(int AngleStep = 0; AngleStep < MaxAngleSteps; AngleStep++)
            {
                //Get the search step we are currently going to use
                int SignedIndex =
                    (AngleStep == 0) ? 0 :
                    ((AngleStep % 2 == 1) ? (AngleStep + 1) / 2 : -AngleStep / 2);

                //Get the new angle we are going to check for and its direction vector
                float AngleOffset = SignedIndex * AngleSearchStepDegrees;
                float AngleRad = (DesiredAngleDegrees + AngleOffset) * Mathf.Deg2Rad;
                Vector2 Direction = new Vector2(Mathf.Cos(AngleRad), Mathf.Sin(AngleRad));

                //Radial search: Start at mid-distance, then move inward/outward
                //0, +1, -1, +2, -2, ...
                for(int RadialStep = 0; RadialStep < RadialSteps; RadialStep++)
                {
                    //Get the search step we are currently going to use
                    int SingedRadian =
                        (RadialStep == 0) ? 0 :
                        ((RadialStep % 2 == 1) ? (RadialStep + 1) / 2 : -RadialStep / 2);

                    //Get the distance in the direction but clamp it in range
                    float Distance = MidDistance + SingedRadian * RadialStepSize;
                    Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);

                    //Find a new position in this direction and find the walkable node
                    Vector3 CheckPos = TargetLocation + (Vector3)(Direction * Distance);
                    MeshNode Candidate = GetNearestWalkableNode(CheckPos);

                    //If it satifies all conditions, use it
                    if(IsValidCandidate(Candidate, EnforceUniqueLocations, EnforceDistanceParameters))
                        return Candidate;
                }
            }

            //No valid node found for this angle
            return null;
        }
    }

    //Returns a flattened view of the navmesh, to prevent having to do nested for loops and iterate through both dimensions of the array
    private IEnumerable<MeshNode> GetAllNodes()
    {
        for(int NodeX = 0; NodeX < NodeGraph.Count; NodeX++)
            for(int NodeY = 0; NodeY < NodeGraph[NodeX].Count; NodeY++)
                yield return NodeGraph[NodeX][NodeY];
    }
}