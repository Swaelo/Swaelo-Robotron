// ================================================================================================================================
// File:        GruntAI.cs
// Description:	Grunts slowly move toward the player like a magnet
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public class GruntAI : MonoBehaviour
{
    public float MoveSpeed = 1.25f;
    private GameObject PlayerCharacter;

    private void Awake()
    {
        PlayerCharacter = GameObject.FindGameObjectWithTag("Player").gameObject;
    }

    private void Update()
    {
        //Get the direction from this grunt to the player character
        Vector3 PlayerDirection = Vector3.Normalize(PlayerCharacter.transform.position - transform.position);

        //Move the Grunt towards the player character
        transform.position += PlayerDirection * MoveSpeed * Time.deltaTime;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //Kill the player if we collide with them
        if (collision.transform.CompareTag("Player"))
            Debug.Log("Kill Player!");
    }
}
