
using UnityEngine;
using System.Collections.Generic;

public class CircuitPuzzle : MonoBehaviour
{
    public Transform grid;

    public CircuitTile startTile;
    public CircuitTile endTile;

    public int columns = 7;
    public int rows = 7;

    public CircuitDirection startDirection = CircuitDirection.Up;
    public CircuitDirection endDirection = CircuitDirection.Down;

    private CircuitTile[] tiles;

    public bool IsSolved { get; private set; }

    private readonly int[] opposite = { 2, 3, 0, 1 };

    private void Start()
    {
        tiles = new CircuitTile[columns * rows];

        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = grid.GetChild(i).GetComponent<CircuitTile>();
            tiles[i].SetPuzzle(this);
        }

        CheckPower();
    }

    public void CheckPower()
    {
        if (tiles == null || IsSolved)
            return;

        bool[] powered = new bool[tiles.Length];
        Queue<int> queue = new Queue<int>();

        int startIndex = System.Array.IndexOf(tiles, startTile);
        int endIndex = System.Array.IndexOf(tiles, endTile);

        if (startIndex < 0 || endIndex < 0)
            return;

        bool[] startConnections = startTile.GetConnections();

        if (!startConnections[(int)startDirection])
        {
            UpdatePoweredTiles(powered);
            return;
        }

        powered[startIndex] = true;
        queue.Enqueue(startIndex);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == endIndex)
            {
                bool[] endConnections = tiles[current].GetConnections();

                if (endConnections[(int)endDirection])
                {
                    IsSolved = true;
                    break;
                }
            }

            bool[] connections = tiles[current].GetConnections();

            int row = current / columns;
            int col = current % columns;

            for (int d = 0; d < 4; d++)
            {
                if (!connections[d])
                    continue;

                int newRow = row;
                int newCol = col;

                switch ((CircuitDirection)d)
                {
                    case CircuitDirection.Up:
                        newRow--;
                        break;

                    case CircuitDirection.Right:
                        newCol++;
                        break;

                    case CircuitDirection.Down:
                        newRow++;
                        break;

                    case CircuitDirection.Left:
                        newCol--;
                        break;
                }

                if (newRow < 0 || newRow >= rows ||
                    newCol < 0 || newCol >= columns)
                    continue;

                int next = newRow * columns + newCol;

                if (powered[next])
                    continue;

                bool[] nextConnections = tiles[next].GetConnections();

                if (nextConnections[opposite[d]])
                {
                    powered[next] = true;
                    queue.Enqueue(next);
                }
            }
        }

        UpdatePoweredTiles(powered);
    }

    private void UpdatePoweredTiles(bool[] powered)
    {
        for (int i = 0; i < tiles.Length; i++)
            tiles[i].SetPowered(powered[i]);
    }
}
