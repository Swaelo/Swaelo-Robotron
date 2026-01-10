// ================================================================================================================================
// File:        PlayerMovement.cs
// Description:	Allows the player to move around the screen
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //Movement
    public float MoveSpeed = 1.5f;  //How fast the player can move

    //Track nav mesh nodes the player is walking over
    private List<MeshNode> WalkingNodes = new List<MeshNode>();

    private void Update()
    {
        //Allow the user to control the player
        if(Game.I.ShouldControlPlayer())
            MovePlayer();

        //Keep navmesh nodes underneath the player as unwalkable for enemies
        SetMeshNodes();
    }

    //Updates the players position based on player input
    private void MovePlayer()
    {
        //Read the players current movement inputs
        float HorizontalInput = Mathf.Clamp(Input.GetAxis("KeyboardHorizontalMovement") + Input.GetAxis("ControllerHorizontalMovement"), -1f, 1f);
        float VerticalInput = Mathf.Clamp(Input.GetAxis("KeyboardVerticalMovement") + Input.GetAxis("ControllerVerticalMovement"), -1f, 1f);

        //Create a new movement vector based on that input
        Vector3 MovementVector = new Vector3(HorizontalInput, VerticalInput, 0f);

        //Apply this vector to move the player to their new position
        transform.position += MovementVector * MoveSpeed * Time.deltaTime;
    }

    //Keeps track of which mesh nodes the player is walking on, and sets them as unwalkable so enemies pathfinding doesnt travel in that direction
    private void SetMeshNodes()
    {
        //Loop through the current nodes and set them all back to walkable
        foreach(MeshNode Node in WalkingNodes)
            Node.SetWalkable(true);

        //Now get the new list of nodes the player is currently walking over and set those as unwalkable
        Game.I.NavMesh.MarkNodesUnderBox(GetComponent<BoxCollider2D>(), false);
    }

    //Resets the player back to the middle of the level
    public void ResetPosition()
    {
        transform.position = Vector3.zero;
    }
}
