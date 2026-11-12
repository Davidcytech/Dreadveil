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

    // Tem de ser múltiplo de 90 (0, 90, 180, 270)
    public int rotation;

    public Color poweredColor = new Color(0.2f, 1f, 0.4f);
    public Color unpoweredColor = Color.white;

    public bool isPowered;

    private CircuitPuzzle puzzle;
    private Outline glow;

    private void Awake()
    {
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
        rotation = NormalizeRotation(rotation);
        UpdateRotation();
    }

    // Ligar a um Button: OnClick -> CircuitTile.Rotate
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

    // Índices: 0 = Cima, 1 = Direita, 2 = Baixo, 3 = Esquerda
    // Base da reta: horizontal (Direita + Esquerda)
    // Base da curva: ┌ (Direita + Baixo)
    // O CircuitPuzzle tem "Straight/Corner Sprite Rotation" para acertar com o sprite.
    // Sprite novo da curva com rotation = 0: ┐ (Esquerda + Baixo) = 90 sobre a base ┌.
    public bool[] GetConnections()
    {
        bool[] connections = new bool[4];
        int baseRotation = 0;

        if (type == TileType.Straight)
        {
            connections[1] = true;
            connections[3] = true;

            if (puzzle != null)
                baseRotation = puzzle.straightSpriteRotation;
        }
        else
        {
            connections[1] = true;
            connections[2] = true;

            // Sem puzzle ligado, assume o sprite novo: ┐ (Esquerda + Baixo)
            baseRotation = puzzle != null ? puzzle.cornerSpriteRotation : 90;
        }

        bool[] rotated = new bool[4];
        int turns = ((NormalizeRotation(baseRotation) + NormalizeRotation(rotation)) / 90) % 4;

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
}