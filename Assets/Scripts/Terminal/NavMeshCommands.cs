// ================================================================================================================================
// File:        NavMeshCommands.cs
// Description:	Allows access to configure the navigation mesh during gameplay
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using UnityEngine.DedicatedServer;

public class NavMeshCommands : MonoBehaviour
{
    public void RegisterCommands(T Terminal)
    {
        //Sets all the nodes on the navmesh as walkable or not
        Terminal.RegisterNewCommand("SetNavMesh", "Sets all nodes on the navmesh to walkable", Arguments =>
        {
            //Show the correct usage of the command if no arguments were passed in
            if(Arguments.Length < 1)
            {
                Terminal.Print("Usage: SetNavMesh <True/False>");
                return;
            }

            //Figure out if they have said true or false
            string MeshStateInput = Arguments[0];
            MeshStateInput.ToLower();
            
            //If the input is invalid exit out
            if(MeshStateInput != "true" && MeshStateInput != "false")
            {
                Terminal.Print("'" + MeshStateInput + "', invalid argument for the SetNavMesh command, please enter True or False.");
                return;
            }

            bool MeshState = MeshStateInput == "true";

            Terminal.Print("All nodes in the navmesh have set their walkable status to " + MeshStateInput + ".");

            Terminal.Print("All nodes in the navmesh have been reset back to walkable status.");
            Game.I.NavMesh.SetAllNodesWalkable(MeshState);
        });
    }
}
//         //Allows the player to spawn entities into the game world
//         Terminal.RegisterNewCommand("spawn", "Spawns entities into the game world, use command <SpawnList> to see the full list.", Arguments =>
//         {
//             //Show the correct usage of the command if no arguments passed in
//             if(Arguments.Length < 1)
//             {
//                 Terminal.Print("Usage: spawn <EntityName>, <EntityCount>");
//                 return;
//             }

//             //Get the number of entities we want to be spawning in
//             int SpawnCount = 1;
//             //If a number was passed in we spawn that number, otherwise we spawn one
//             if(Arguments.Length < 2)
//                 SpawnCount = 1;
//             else
//             {
//                 //Make sure an integer value has been passed in
//                 if(!int.TryParse(Arguments[1], out SpawnCount) || SpawnCount < 1)
//                 {
//                     Terminal.Print("Invalid entity count. Using default value of 1.");
//                     SpawnCount = 1;
//                 }
//             }

//             //Print to the console what we are trying to do
//             string EntityType = Arguments[0];
            
//             Terminal.Print("Attempting to spawn in " + SpawnCount + EntityType);
//             Game.Instance.Prefabs.SpawnEntities(EntityType, SpawnCount);
//         });
//     }