// ================================================================================================================================
// File:        BaseEntity.cs
// Description:	Abstract class which all entities, hostile and friendly must extend from
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;

public enum EntityType
{
    Electrode = 1,
    Grunt = 2,
    Hulk = 3,
    Enforcer = 4,
    Brain = 5,
    Tank = 6,
    Spheroid = 7,
    Quark = 8,
    Mummy = 9,
    Daddy = 10,
    Mikey = 11,
    DaddyProg = 12,
    MummyProg = 13,
    MikeyProg = 14
}

public abstract class BaseEntity : MonoBehaviour
{
    public EntityType Type;
}
