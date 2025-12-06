// ================================================================================================================================
// File:        LevelBorders.cs
// Description:	Allows the size of the level to be changed from the inspector which is then
// generated at run time and placed down
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using System.Collections.Generic;
using UnityEngine;

public class LevelBorders : MonoBehaviour
{
    //Singleton Instance
    public static LevelBorders Instance;
    private void Awake() { Instance = this; }

    [Header("Level Size")]
    public float LevelWidth = 15f;
    public float LevelHeight = 8f;

    public float GetUseableLevelWidth() { return LevelWidth - BorderThickness * 2; }
    public float GetUseableLevelHeight() { return LevelHeight - BorderThickness * 2; }

    [Header("Border Settings")]
    public float BorderThickness = 1f;

    //Bounds of the level, grabbed by other scripts to ensure their behaviour remains inside the level
    public Vector2 XBounds;
    public Vector2 YBounds;

    //Corner positions of the level (or positions nearby to the corners), used by spheroids are target locations to flee to
    private List<Vector3> CornerPositions;
    public List<Vector3> GetCornerPositions() { return CornerPositions; }

    public void InitLevelBorders()
    {
        //Setup the level boundaries etc
        SetBounds();
        DefineCornerLocations();

        //Once the level has been setup, get the nav mesh generated
        NavMeshManager.Instance.GenerateNavMesh();

        GenerateBorders();
    }

    //Sets down all the level borders into place
    private void GenerateBorders()
    {
        //Calculate half extents for placement
        float halfWidth = LevelWidth / 2f;
        float halfHeight = LevelHeight / 2f;
        
        CreateBorder("TopBorder",
            new Vector2(0, -halfHeight),
            new Vector2(LevelWidth + BorderThickness, BorderThickness));

        CreateBorder("BottomBorder",
            new Vector2(0, halfHeight),
            new Vector2(LevelWidth + BorderThickness, BorderThickness));

        CreateBorder("LeftBorder",
            new Vector2(-halfWidth, 0),
            new Vector2(BorderThickness, LevelHeight + BorderThickness));

        CreateBorder("RightBorder",
            new Vector2(halfWidth, 0),
            new Vector2(BorderThickness, LevelHeight + BorderThickness));
    }

    private void CreateBorder(string BorderName, Vector2 BorderPos, Vector2 BorderSize)
    {
        //Create the new border and set its location
        GameObject NewBorder = new GameObject(BorderName);
        NewBorder.transform.parent = transform;
        NewBorder.transform.localPosition = BorderPos;

        //Add the 2D box collider component
        BoxCollider2D BorderCollider = NewBorder.AddComponent<BoxCollider2D>();
        BorderCollider.size = BorderSize;

        //Add a sprite component which will make the border visible
        SpriteRenderer BorderRenderer = NewBorder.AddComponent<SpriteRenderer>();
        Texture2D SpriteTexture = new Texture2D(1, 1);

        //Generate a 1x1 white texture and apply that to the sprite
        SpriteTexture.SetPixel(0, 0, Color.white);
        SpriteTexture.Apply();
        BorderRenderer.sprite = Sprite.Create(SpriteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        BorderRenderer.drawMode = SpriteDrawMode.Sliced;

        //Set the size
        BorderRenderer.size = BorderSize;

        //Add the seizure trigger
        SpriteColorCycle ColorCycle = NewBorder.AddComponent<SpriteColorCycle>();
        ColorCycle.ColorSpeed = .5f;
    }

    //Define the level bounds which will be grabbed from this script by AI scripts
    private void SetBounds()
    {
        float HalfWidth = LevelWidth * 0.5f;
        float XMin = -HalfWidth + BorderThickness;
        float XMax = HalfWidth - BorderThickness;
        XBounds = new Vector2(XMin, XMax);

        float HalfHeight = LevelHeight * 0.5f;
        float YMin = -HalfHeight + BorderThickness;
        float YMax = HalfHeight - BorderThickness;
        YBounds = new Vector2(YMin, YMax);
    }

    public Vector3 ClampPositionInsideBounds(Vector3 Position)
    {
        Vector3 ClampedPos = Position;

        ClampedPos.x = Mathf.Clamp(ClampedPos.x, XBounds.x, XBounds.y);
        ClampedPos.y = Mathf.Clamp(ClampedPos.y, YBounds.x, YBounds.y);

        return ClampedPos;
    }

    //Define the corner locations which will be grabbed by spheroid scripts
    private void DefineCornerLocations()
    {
        Vector3 TopRight = new Vector3(XBounds.y, YBounds.y, 0f);
        Vector3 BottomRight = new Vector3(XBounds.y, YBounds.x, 0f);
        Vector3 BottomLeft = new Vector3(XBounds.x, YBounds.x, 0f);
        Vector3 TopLeft = new Vector3(XBounds.x, YBounds.y, 0f);

        CornerPositions = new List<Vector3>();
        CornerPositions.Add(TopRight);
        CornerPositions.Add(BottomRight);
        CornerPositions.Add(BottomLeft);
        CornerPositions.Add(TopLeft);
    }
}