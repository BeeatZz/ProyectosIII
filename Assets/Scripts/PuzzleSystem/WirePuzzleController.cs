using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class WirePuzzleController : NetworkBehaviour
{
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 6;
    [SerializeField] private WireTile[] tiles;
    [SerializeField] private int startTileIndex = 0;
    [SerializeField] private int endTileIndex = 29;
    [SerializeField] private bool randomiseOnStart = true;
    [SerializeField] private UnityEvent onPuzzleSolved;

    [SyncVar(hook = nameof(OnSolvedChanged))]
    private bool isSolved = false;

    [SyncVar] private bool isPuzzleActive = true;
    [SyncVar] public bool isUnlocked = false;

    public bool IsSolved => isSolved;
    public bool IsPuzzleActive => isPuzzleActive;

    public override void OnStartServer()
    {
        base.OnStartServer();

        for (int i = 0; i < tiles.Length; i++)
        {
            int startRot = randomiseOnStart ? Random.Range(0, 4) : 0;
            tiles[i].Init(this, startRot);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isServer) return; 

       
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].Init(this);
        }
    }

    [Server]
    public void CheckSolution()
    {
        if (isSolved) return;
        if (IsPathConnected()) isSolved = true;
    }

    private bool IsPathConnected()
    {
        var visited = new bool[tiles.Length];
        var queue = new Queue<int>();
        queue.Enqueue(startTileIndex);
        visited[startTileIndex] = true;

        // 0:N, 1:E, 2:S, 3:W
        int[] dRow = { -1, 0, 1, 0 };
        int[] dCol = { 0, 1, 0, -1 };
        int[] opposite = { 2, 3, 0, 1 };

        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            if (idx == endTileIndex) return true;

            int row = idx / gridWidth;
            int col = idx % gridWidth;

            for (int dir = 0; dir < 4; dir++)
            {
                if (!tiles[idx].ConnectsIn(dir)) continue;

                int nRow = row + dRow[dir];
                int nCol = col + dCol[dir];

                if (nRow < 0 || nRow >= gridHeight || nCol < 0 || nCol >= gridWidth) continue;

                int nIdx = nRow * gridWidth + nCol;
                if (visited[nIdx] || nIdx >= tiles.Length) continue;

                if (tiles[nIdx].ConnectsIn(opposite[dir]))
                {
                    visited[nIdx] = true;
                    queue.Enqueue(nIdx);
                }
            }
        }

        return false;
    }

    private void OnSolvedChanged(bool oldVal, bool newVal)
    {
        if (newVal) onPuzzleSolved?.Invoke();
    }
}