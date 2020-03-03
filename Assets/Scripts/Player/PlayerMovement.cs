// ================================================================================================================================
// File:        PlayerMovement.cs
// Description:	Allows the player to move around the screen
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float MoveSpeed = 1.5f;

    private void Update()
    {
        //Create a new movement vector based on users keyboard input
        Vector3 MovementVector = new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f);

        //Use the movement vector to update the players position
        transform.position += MovementVector * MoveSpeed * Time.deltaTime;
    }
}
