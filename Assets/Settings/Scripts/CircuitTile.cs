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

    public Color poweredColor = new Color(0.2f, 1f, 0.4f);
    public Color unpoweredColor = Color.white;

    public bool isPowered;

    private CircuitPuzzle puzzle;
    private Outline glow;

    private int initialRotation;
    private bool initialSaved;

    private void Awake()
    {
        // Guarda a rotacao inicial UMA so vez (nunca e sobrescrita).
        if (!initialSaved)
        {
            rotation = NormalizeRotation(rotation);
            initialRotation = rotation;
            initialSaved = true;
        }

        if (image == null)
            image = GetComponent<Image>();

        if (image != null)
        {
            glow = image.GetComponent<Outline>();

            if (glow == null)
                glow = image.gameObject.AddComponent<Outline>();

            glow.effectColor = poweredColor;
            glow.effectDistance = new Vector2(5, 5);
            glow.enabled = false;
        }
    }

    public void SetPuzzle(CircuitPuzzle circuitPuzzle)
    {
        puzzle = circuitPuzzle;

        // Ja nao mexe no initialRotation.
        rotation = NormalizeRotation(rotation);
        UpdateRotation();
    }

    public void Rotate()
    {
        if (puzzle != null && puzzle.IsSolved)
            return;

        rotation = NormalizeRotation(rotation + 90);

        UpdateRotation();
        puzzle?.CheckPower();
    }

    private int NormalizeRotation(int value)
    {
        return ((value % 360) + 360) % 360;
    }

    private void UpdateRotation()
    {
        if (image != null)
        {
            image.rectTransform.localRotation =
                Quaternion.Euler(0, 0, -rotation);
        }
    }

    public bool[] GetConnections()
    {
        // Indices: 0 = Cima, 1 = Direita, 2 = Baixo, 3 = Esquerda
        bool[] connections = new bool[4];

        // Orientacao base (rotation = 0):
        //   Reta  -> Esquerda + Direita
        //   Curva -> Esquerda + Baixo
        if (type == TileType.Straight)
        {
            connections[3] = true;
            connections[1] = true;
        }
        else
        {
            connections[3] = true;
            connections[2] = true;
        }

        bool[] rotated = new bool[4];

        // Rotation em sentido horario, igual ao visual (Euler 0,0,-rotation).
        int turns = (NormalizeRotation(rotation) / 90) % 4;

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

        if (glow != null)
            glow.enabled = powered;
    }

    public void ResetTile()
    {
        rotation = initialRotation;

        UpdateRotation();
        SetPowered(false);
    }
}