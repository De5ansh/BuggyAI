using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GridCoverageTracker : MonoBehaviour
{
    public Vector3 worldMin = new Vector3(-100f, 0f, -100f);
    public Vector3 worldMax = new Vector3(100f, 0f, 100f);
    public float cellSize = 4f;

    private HashSet<Vector2Int> episodeVisited = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> globalVisited = new HashSet<Vector2Int>();

    private List<Vector2Int> bugCells = new List<Vector2Int>();

    private List<string> bugLogs = new List<string>();

    public void ResetEpisodeCoverage()
    {
        episodeVisited.Clear();
        Debug.Log("[TRACKER] Episode Coverage Reset");
    }

    public bool RegisterAgentPosition(Vector3 pos)
    {
        if (!IsInsideGrid(pos)) return false;

        Vector2Int cell = WorldToCell(pos);

        globalVisited.Add(cell);

        if (!episodeVisited.Contains(cell))
        {
            episodeVisited.Add(cell);

            Debug.Log($"[TRACKER] New Cell {cell} | Episode: {episodeVisited.Count} | Global: {globalVisited.Count}");

            return true;
        }

        return false;
    }

    public void RegisterBug(Vector3 pos, string type)
    {
        Vector2Int cell = WorldToCell(pos);
        bugCells.Add(cell);

        Debug.Log($"[TRACKER] BUG Registered | Type: {type} | Cell: {cell}");
    }

    public Vector2Int WorldToCell(Vector3 pos)
    {
        int x = Mathf.FloorToInt((pos.x - worldMin.x) / cellSize);
        int z = Mathf.FloorToInt((pos.z - worldMin.z) / cellSize);
        return new Vector2Int(x, z);
    }

    public Vector2 GetNormalizedPosition(Vector3 pos)
    {
        float x = Mathf.InverseLerp(worldMin.x, worldMax.x, pos.x);
        float z = Mathf.InverseLerp(worldMin.z, worldMax.z, pos.z);
        return new Vector2(x, z);
    }

    public bool IsInsideGrid(Vector3 pos)
    {
        return pos.x >= worldMin.x && pos.x <= worldMax.x &&
               pos.z >= worldMin.z && pos.z <= worldMax.z;
    }



    public void SaveCoverageToFile()
    {
        string path = Application.dataPath + "/../coverage.txt";

        using (StreamWriter writer = new StreamWriter(path))
        {
            foreach (var cell in globalVisited)
            {
                writer.WriteLine($"{cell.x},{cell.y},visited");
            }

            foreach (var bug in bugCells)
            {
                writer.WriteLine($"{bug.x},{bug.y},bug");
            }
        }

        Debug.Log("Coverage + Bugs saved!");
    }

    public bool IsCellVisited(Vector2Int cell)
    {
        return globalVisited.Contains(cell);
    }


    public int GetGlobalVisitedCount() => globalVisited.Count;
}