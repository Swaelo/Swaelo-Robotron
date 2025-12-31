// ================================================================================================================================
// File:        SpawningCommands.cs
// Description:	Defines commands executable from the terminal which allow the player to spawn objects into the game
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class SpawningCommands : MonoBehaviour
{
    //Registers all the commands for spawning
    public void RegisterCommands(T Terminal)
    {
        // //Give you the list of enemy names that can be spawned into the game
        Terminal.RegisterNewCommand("SpawnList", "Outputs a list of entity types that can be spawned in through the terminal", Arguments =>
        {
            string EntityList = "Grunt, Spheroid, Enforcer, Quark, Tank, Brain, Mummy / MummyProg, Daddy / DaddyProg, Mikey / MikeyProg, ElectodeA-D";
            Terminal.Print("Entities: " + EntityList);
        });

        //Allows the player to spawn entities into the game world
        Terminal.RegisterNewCommand("spawn", "Spawns entities into the game world, use command <SpawnList> to see the full list.", Arguments =>
        {
            //Show the correct usage of the command if no arguments passed in
            if(Arguments.Length < 1)
            {
                Terminal.Print("Usage: spawn <EntityName>, <EntityCount>");
                return;
            }

            //Get the number of entities we want to be spawning in
            int SpawnCount = 1;
            //If a number was passed in we spawn that number, otherwise we spawn one
            if(Arguments.Length < 2)
                SpawnCount = 1;
            else
            {
                //Make sure an integer value has been passed in
                if(!int.TryParse(Arguments[1], out SpawnCount) || SpawnCount < 1)
                {
                    Terminal.Print("Invalid entity count. Using default value of 1.");
                    SpawnCount = 1;
                }
            }

            //Print to the console what we are trying to do
            string EntityType = Arguments[0];
            T.Log("Spawning in " + SpawnCount + EntityType);

            //Grab the entity and a list of locations to spawn them in, surrounding the player
            Game.I.Prefabs.SpawnEntities(EntityType, SpawnCount);
        });
    }
}