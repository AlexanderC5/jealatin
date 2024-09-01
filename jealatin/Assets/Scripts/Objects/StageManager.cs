using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StageManager : MonoBehaviour
{
    [SerializeField] private Vector3 CameraOffset; // Manual camera offset in inspector (probably unnecessary)
    [SerializeField] private Tilemap colliderTilemap;
    [SerializeField] private Tilemap stageTilemap;

    void Start()
    {
        colliderTilemap.CompressBounds();
        stageTilemap.CompressBounds();

        if (colliderTilemap != null) colliderTilemap.color = new Color(1f, 1f, 1f, 0f); // Hide the collider tilemap
        if (stageTilemap != null && colliderTilemap != null)
        {
             BoundsInt bound = colliderTilemap.cellBounds;
             
             if (stageTilemap.cellBounds.xMin < bound.xMin) bound.xMin = stageTilemap.cellBounds.xMin;
             if (stageTilemap.cellBounds.xMax > bound.xMax) bound.xMax = stageTilemap.cellBounds.xMax;
             if (stageTilemap.cellBounds.yMin < bound.yMin) bound.yMin = stageTilemap.cellBounds.yMin;
             if (stageTilemap.cellBounds.yMax > bound.yMax) bound.yMax = stageTilemap.cellBounds.yMax;

             GameManager.Instance.StageSize = bound;
        }
        try
        {
            GameObject.FindGameObjectWithTag("MainCamera").GetComponent<CameraPosition>().UpdateCamera();
        }
        catch
        {
            Debug.Log("No MainCamera found in scene!");
        }
    }
}
