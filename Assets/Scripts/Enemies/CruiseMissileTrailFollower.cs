// ================================================================================================================================
// File:        CruiseMissileTrailFollower.cs
// Description:	Controls the movement of the Cruise Missile tail sections
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class CruiseMissileTrailFollower : MonoBehaviour
{
    public CruiseMissileAI ParentSection;   //Tail sections must alert the main parent section when they detect collisions with something important

    public GameObject FollowTarget; //Which section this section is following
    private float IdealDistance = 0.1f; //Amount of distance we want to tail behind the target
    private float MaxDistance = 0.25f;    //Maximum distance we want to fall behind the target

    private void Update()
    {
        //Move toward the target whenever it gets too far away
        float TargetDistance = Vector3.Distance(transform.position, FollowTarget.transform.position);
        if(TargetDistance >= MaxDistance)
        {
            //Move straight towards the target until were back at the ideal following distance
            Vector3 TargetDirection = Vector3.Normalize(FollowTarget.transform.position - transform.position);
            Vector3 IdealPosition = FollowTarget.transform.position - TargetDirection * IdealDistance;
            transform.position = IdealPosition;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Any tail sections which are struct by player projectiles instruct the main body part to destroy the entire missile
        if (collision.transform.CompareTag("PlayerProjectile"))
        {
            Destroy(collision.gameObject);
            ParentSection.DestroyProjectile();
        }
        //Player is also killed by colliding with tail section parts
        else if (collision.transform.CompareTag("Player"))
            GameState.Instance.KillPlayer();
    }
}
