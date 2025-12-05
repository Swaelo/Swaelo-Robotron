// ================================================================================================================================
// File:        MeshNode.cs
// Description:	Stores information for one node on the levels navmesh
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class MeshNode
{
    public Vector3 NodePos = Vector3.zero;  //This nodes position in the world
    public bool IsWalkable = true;  //Can units walk over this node
    public float GCost = float.MaxValue; //Distance from the start of the pathway
    public float HCost = 0f;    //Heuristic estimate of cost from current node to goal
    public float FCost = 0f;    //G+H, total estimated cost
    public MeshNode Parent = null;  //Pointer to previous node, used for path reconstruction

    public Vector2 GridPosition = Vector2.zero; //This nodes positions in the 2d array

    //Used to make the node visible during gameplay for debugging purposes
    public GameObject RenderObject;
    public void InitRenderer(string Name)
    {
        //Create a game object which makes this node visible
        RenderObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        RenderObject.name = Name;
        RenderObject.transform.position = NodePos;

        //Scale it down
        RenderObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        //Set a color / material to make it visible
        var Material = new Material(Shader.Find("Unlit/Color"));
        Material.color = Color.blue;
        RenderObject.GetComponent<Renderer>().material = Material;

        //I dont know why this is automatically added but get rid of it
        Object.Destroy(RenderObject.GetComponent<BoxCollider>());
    }

    public void SetColor(Color NewColor)
    {
        RenderObject.GetComponent<Renderer>().material.color = NewColor;
    }
}
