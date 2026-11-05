using UnityEngine;
using System.Collections.Generic;

public class CircuitPuzzle : MonoBehaviour
{
    [Header("Objetos")]
    public Transform grid;
    public Transform startPoint;
    public Transform endPoint;
    public GameObject victoryPanel;

    [Header("Grelha")]
    public int columns = 7;
    public int rows = 7;

    [Header("Ligacoes externas")]
    public int startTileNumber = 1;
    public int endTileNumber = 28;

    [Tooltip("Se ligado, a direcao do Start/End e calculada pela posicao dos pontos em relacao as pecas.")]
    public bool autoDetectDirections = true;

    // Usados apenas se autoDetectDirections estiver desligado.
    public CircuitDirection startDirection = CircuitDirection.Left;
    public CircuitDirection endDirection = CircuitDirection.Right;

    [Header("Orientacao dos sprites com rotation = 0")]
    [Tooltip("Reta: 0 = horizontal, 90 = vertical (180 e 270 repetem)")]
    public int straightSpriteRotation = 0;

    [Tooltip("Curva com rotation = 0: 0 = ┌ (Dir+Baixo), 90 = ┐ (Esq+Baixo), 180 = ┘ (Esq+Cima), 270 = └ (Cima+Dir)")]
    public int cornerSpriteRotation = 90;

    [Header("Debug")]
    public bool debugLog = false;

    private CircuitTile[] tiles;

    public bool IsSolved { get; private set; }

    // Disparado uma vez quando o puzzle e resolvido.
    public event System.Action OnSolved;

    // Oposto de cada direcao: Up<->Down, Right<->Left
    private readonly int[] opposite = { 2, 3, 0, 1 };

    private void Start()
    {
        if (grid == null || startPoint == null || endPoint == null)
        {
            Debug.LogError("Falta atribuir a Grid, Start Point ou End Point!");
            return;
        }

        if (grid.childCount != columns * rows)
        {
            Debug.LogError("A grelha não tem " + (columns * rows) + " peças!");
            return;
        }

        if (startTileNumber < 1 || startTileNumber > columns * rows ||
            endTileNumber < 1 || endTileNumber > columns * rows)
        {
            Debug.LogError("Número de peça inicial ou final inválido!");
            return;
        }

        CircuitTile[] found = new CircuitTile[columns * rows];

        for (int i = 0; i < found.Length; i++)
        {
            found[i] = grid.GetChild(i).GetComponent<CircuitTile>();

            if (found[i] == null)
            {
                Debug.LogError("Falta CircuitTile na peça " + (i + 1));
                return;
            }
        }

        tiles = found;

        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i].SetPuzzle(this);
            tiles[i].SetPowered(false);
        }

        if (autoDetectDirections)
        {
            startDirection = DirectionFromTo(tiles[startTileNumber - 1].transform, startPoint);
            endDirection = DirectionFromTo(tiles[endTileNumber - 1].transform, endPoint);
        }

        if (debugLog || autoDetectDirections)
            Debug.Log("Start Direction = " + startDirection + " | End Direction = " + endDirection);

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        CheckPower();
    }

    // Lado da peça que está virado para o ponto (Start ou End).
    // Usa o espaço LOCAL da Grid, por isso funciona mesmo que o Canvas esteja
    // rodado ou espelhado no mundo (World Space).
    private CircuitDirection DirectionFromTo(Transform tile, Transform point)
    {
        Vector3 tileLocal = grid.InverseTransformPoint(tile.position);
        Vector3 pointLocal = grid.InverseTransformPoint(point.position);
        Vector3 delta = pointLocal - tileLocal;

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            return delta.x >= 0 ? CircuitDirection.Right : CircuitDirection.Left;

        return delta.y >= 0 ? CircuitDirection.Up : CircuitDirection.Down;
    }

    public void CheckPower()
    {
        if (tiles == null || IsSolved)
            return;

        bool[] powered = new bool[tiles.Length];
        Queue<int> queue = new Queue<int>();

        int startIndex = startTileNumber - 1;
        int endIndex = endTileNumber - 1;

        // A primeira peça tem de estar ligada à fonte.
        if (!tiles[startIndex].GetConnections()[(int)startDirection])
        {
            if (debugLog)
                Debug.Log("Peça inicial NÃO liga à fonte (direção " + startDirection + ").");

            UpdatePoweredTiles(powered);
            return;
        }

        powered[startIndex] = true;
        queue.Enqueue(startIndex);

        // BFS completo: percorre todo o circuito ligado.
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
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
                    case CircuitDirection.Up:    newRow--; break;
                    case CircuitDirection.Right: newCol++; break;
                    case CircuitDirection.Down:  newRow++; break;
                    case CircuitDirection.Left:  newCol--; break;
                }

                if (newRow < 0 || newRow >= rows ||
                    newCol < 0 || newCol >= columns)
                    continue;

                int next = newRow * columns + newCol;

                if (powered[next])
                    continue;

                // As duas peças têm de ter ligações correspondentes.
                if (tiles[next].GetConnections()[opposite[d]])
                {
                    powered[next] = true;
                    queue.Enqueue(next);

                    if (debugLog)
                        Debug.Log("Peça " + (current + 1) + " -> peça " + (next + 1) + " (" + (CircuitDirection)d + ")");
                }
            }
        }

        UpdatePoweredTiles(powered);

        // Vitória: a peça final está alimentada E liga ao End Point.
        if (powered[endIndex] &&
            tiles[endIndex].GetConnections()[(int)endDirection])
        {
            IsSolved = true;

            if (victoryPanel != null)
                victoryPanel.SetActive(true);

            Debug.Log("Circuit Puzzle concluído!");

            OnSolved?.Invoke();
        }
    }

    private void UpdatePoweredTiles(bool[] powered)
    {
        for (int i = 0; i < tiles.Length; i++)
            tiles[i].SetPowered(powered[i]);
    }
}