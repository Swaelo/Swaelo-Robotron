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
    }
}
