
using UnityEngine;
using UnityEngine.UI;

public enum TileType
{
    Straight,
    Corner
}

public enum CircuitDirection
{
    Up,
    Right,
    Down,
    Left
}

public class CircuitTile : MonoBehaviour
{
    public Image image;
    public TileType type;
    public int rotation;
    public bool isPowered;

    public Color poweredColor = Color.green;
    public Color unpoweredColor = Color.white;

    private CircuitPuzzle puzzle;

    public void SetPuzzle(CircuitPuzzle circuitPuzzle)
    {
        puzzle = circuitPuzzle;
        UpdateRotation();
    }

    public void Rotate()
    {
        if (puzzle != null && puzzle.IsSolved)
            return;

        rotation = (rotation + 90) % 360;

        UpdateRotation();
        puzzle?.CheckPower();
    }

    private void UpdateRotation()
    {
        if (image != null)
            image.rectTransform.localRotation =
                Quaternion.Euler(0, 0, -rotation);
    }

    public bool[] GetConnections()
    {
        bool[] connections = new bool[4];

        if (type == TileType.Straight)
        {
            connections[0] = true;
            connections[2] = true;
        }
        else if (type == TileType.Corner)
        {
            connections[0] = true;
            connections[1] = true;
        }

        bool[] rotated = new bool[4];
        int turns = rotation / 90;

        for (int i = 0; i < 4; i++)
        {
            if (connections[i])
                rotated[(i + turns) % 4] = true;
        }

        return rotated;
    }

    public void SetPowered(bool powered)
    {
        isPowered = powered;

        if (image != null)
            image.color = powered ? poweredColor : unpoweredColor;
    }
}
