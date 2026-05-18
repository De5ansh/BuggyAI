using System.Collections.Generic;
using UnityEngine;

public class HeatmapVisualizer : MonoBehaviour
{
    public GridCoverageTracker tracker;
    public GameObject tilePrefab;

    private Dictionary<Vector2Int, GameObject> tiles = new();

    void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (float x = tracker.worldMin.x; x < tracker.worldMax.x; x += tracker.cellSize)
        {
            for (float z = tracker.worldMin.z; z < tracker.worldMax.z; z += tracker.cellSize)
            {
                Vector3 pos = new Vector3(x + tracker.cellSize / 2f, 0.5f, z + tracker.cellSize / 2f);
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);

                Vector2Int cell = tracker.WorldToCell(pos);
                tiles[cell] = tile;

                tile.GetComponent<Renderer>().material.color = Color.black;
            }
        }
    }

    void Update()
    {
        UpdateHeatmap();
    }

    void UpdateHeatmap()
    {
        foreach (var cell in tiles.Keys)
        {
            if (tracker.IsCellVisited(cell))
            {
                tiles[cell].GetComponent<Renderer>().material.color =
    Color.Lerp(Color.black, new Color(0.2f, 0.8f, 0.2f), 0.7f);
            }
        }
    }
}