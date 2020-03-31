// ================================================================================================================================
// File:        PlayerMovement.cs
// Description:	Allows the player to move around the screen
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float MoveSpeed = 1.5f;  //How fast the player can move
    private Vector3 PreviousPosition = Vector3.zero;   //Players position last frame
    public Vector3 MovementVelocity = Vector3.zero;    //Velocity at which the player is currently travelling

    private void Awake()
    {
        PreviousPosition = transform.position;
        MovementVelocity = Vector3.zero;
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        //Create a new movement vector based on input from the keyboard and controller
        float HorizontalMovementInput = Mathf.Clamp(Input.GetAxis("KeyboardHorizontalMovement") + Input.GetAxis("ControllerHorizontalMovement"), -1f, 1f);
        float VerticalMovementInput = Mathf.Clamp(Input.GetAxis("KeyboardVerticalMovement") + Input.GetAxis("ControllerVerticalMovement"), -1f, 1f);
        Vector3 MovementVector = new Vector3(HorizontalMovementInput, VerticalMovementInput, 0f);

        //Use the movement vector to update the players position
        transform.position += MovementVector * MoveSpeed * Time.deltaTime;

        //Calculate movement velocity between now and the previous frame, then store current position for next frames calculation
        MovementVelocity = transform.position - PreviousPosition;
        PreviousPosition = transform.position;
    }
    
    //Resets the player back to the middle of the level
    public void ResetPosition()
    {
        transform.position = Vector3.zero;
    }
}
