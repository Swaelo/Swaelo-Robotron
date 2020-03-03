// ================================================================================================================================
// File:        ProjectileMovement.cs
// Description:	Applys movement to the projectile every frame
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    public float MoveSpeed = 3.5f;
    private Vector3 MovementDirection;
    private bool DirectionSet = false;

    public void SetMovement(Vector3 Movement)
    {
        DirectionSet = true;
        MovementDirection = Movement;
    }

    private void Update()
    {
        if (DirectionSet)
            transform.position += MovementDirection * MoveSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Simply delete the projectile when it hits on of the border walls
        if(collision.transform.CompareTag("Wall"))
        {
            GameObject.Destroy(this.gameObject);
            return;
        }

        //Delete projectile and enemy when hitting a Grunt enemy
        if(collision.transform.CompareTag("Grunt"))
        {
            GameObject.Destroy(collision.gameObject);
            GameObject.Destroy(this.gameObject);
            return;
        }
    }
}
