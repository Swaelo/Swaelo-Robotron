// ================================================================================================================================
// File:        ProjectileMovement.cs
// Description:	Applys movement to the projectile every frame
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public float MoveSpeed = 3.5f;  //How fast the projectile moves across the screen
    private Vector3 MovementDirection;  //Which direction this projectile should be moving
    private bool DirectionSet = false;  //Flagged once the direction of movement has been set by the player
    private Animator AnimationController;   //Used to changed the color of the projectile

    private void Awake()
    {
        //Find and store reference to the animation controller component
        AnimationController = GetComponent<Animator>();
    }

    //Provides the projectile with all the information it needs to travel after the player has fired it
    public void InitializeProjectile(Vector3 Direction, ProjectileColor Color)
    {
        DirectionSet = true;
        MovementDirection = Direction;
        switch (Color)
        {
            case (ProjectileColor.Blue):
                AnimationController.SetTrigger("Blue");
                break;
            case (ProjectileColor.Green):
                AnimationController.SetTrigger("Green");
                break;
            case (ProjectileColor.Red):
                AnimationController.SetTrigger("Red");
                break;
            case (ProjectileColor.White):
                AnimationController.SetTrigger("White");
                break;
            case (ProjectileColor.Yellow):
                AnimationController.SetTrigger("Yellow");
                break;
        }
    }

    //Returns the direction vector which the projectile is moving in
    public Vector3 GetDirectionVector()
    {
        return MovementDirection;
    }

    private void Update()
    {
        //All game logic and AI should be paused at certain times
        if (!GameState.Instance.ShouldAdvanceGame())
            return;

        if (DirectionSet)
        {
            Vector3 NewPosition = transform.position + MovementDirection * MoveSpeed * Time.deltaTime;
            NewPosition.z = 0f;
            transform.position = NewPosition;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //The projectile is destroyed if it hits the boundaries of the screen
        if (collision.transform.CompareTag("Wall"))
            Destroy(this.gameObject);
    }

    //Removes the projectile from the players tracking list then destroys it
    public void OnDestroy()
    {
        GameState.Instance.Player.GetComponent<PlayerShooting>().ActiveProjectiles.Remove(this.gameObject);
    }
}
