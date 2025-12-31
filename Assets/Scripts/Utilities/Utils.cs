// ================================================================================================================================
// File:        Utils.cs
// Description:	Contains various useful functions
// Author:	    Harley Laurie https://www.github.com/Swaelo/
// ================================================================================================================================

using UnityEngine;
using System.Collections.Generic;

public static class Utils
{
    //Returns the world location where the mouse cursor is current hovering over
    public static Vector3 GetMouseWorldPos()
    {
        //Break out if we cant find the camera
        if(Camera.main == null)
        {
            Debug.Log("cannot find camera");
            return Vector3.zero;
        }

        return Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }

    // Generates a set of points within a rectangle (XBounds, YBounds) with minimum distance 'radius'
    public static List<Vector2> GeneratePoints(float radius, Vector2 XBounds, Vector2 YBounds, int k = 30)
    {
        float cellSize = radius / Mathf.Sqrt(2);

        int gridWidth = Mathf.CeilToInt((XBounds.y - XBounds.x) / cellSize);
        int gridHeight = Mathf.CeilToInt((YBounds.y - YBounds.x) / cellSize);

        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        for (int i = 0; i < gridWidth; i++)
            for (int j = 0; j < gridHeight; j++)
                grid[i, j] = Vector2.negativeInfinity; // Empty cell marker

        List<Vector2> points = new List<Vector2>();
        List<Vector2> activeList = new List<Vector2>();

        // Start with a random initial point
        Vector2 firstPoint = new Vector2(
            Random.Range(XBounds.x, XBounds.y),
            Random.Range(YBounds.x, YBounds.y)
        );
        points.Add(firstPoint);
        activeList.Add(firstPoint);

        int xIdx = (int)((firstPoint.x - XBounds.x) / cellSize);
        int yIdx = (int)((firstPoint.y - YBounds.x) / cellSize);
        grid[xIdx, yIdx] = firstPoint;

        while (activeList.Count > 0)
        {
            int idx = Random.Range(0, activeList.Count);
            Vector2 point = activeList[idx];
            bool found = false;

            for (int i = 0; i < k; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float mag = Random.Range(radius, 2 * radius);
                Vector2 candidate = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * mag;

                if (candidate.x < XBounds.x || candidate.x > XBounds.y ||
                    candidate.y < YBounds.x || candidate.y > YBounds.y ||
                    candidate.magnitude < 1.5f) // optional center exclusion
                    continue;

                int cx = (int)((candidate.x - XBounds.x) / cellSize);
                int cy = (int)((candidate.y - YBounds.x) / cellSize);

                bool valid = true;
                // Check neighboring cells for minimum distance
                for (int iX = Mathf.Max(0, cx - 2); iX <= Mathf.Min(gridWidth - 1, cx + 2); iX++)
                {
                    for (int iY = Mathf.Max(0, cy - 2); iY <= Mathf.Min(gridHeight - 1, cy + 2); iY++)
                    {
                        if (grid[iX, iY] != Vector2.negativeInfinity &&
                            Vector2.Distance(candidate, grid[iX, iY]) < radius)
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (!valid) break;
                }

                if (valid)
                {
                    points.Add(candidate);
                    activeList.Add(candidate);
                    grid[cx, cy] = candidate;
                    found = true;
                    break;
                }
            }

            if (!found)
                activeList.RemoveAt(idx);
        }

        return points;
    }
}