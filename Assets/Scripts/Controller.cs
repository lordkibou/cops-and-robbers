using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
                    
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        // Matriz de adyacencia
        int[,] adjacencyMatrix = new int[Constants.NumTiles, Constants.NumTiles];

        // Paso 1: Inicializar matriz a 0's
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                adjacencyMatrix[i, j] = 0;
            }
        }

        // Paso 2: Rellenar la matriz con 1's para las casillas adyacentes
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            // Calcular fila y columna de la casilla actual
            int row = i / Constants.TilesPerRow;
            int col = i % Constants.TilesPerRow;

            // Casilla de arriba
            if (row > 0)
            {
                int adjacentIndex = i - Constants.TilesPerRow;
                adjacencyMatrix[i, adjacentIndex] = 1;
                tiles[i].adjacency.Add(adjacentIndex);
            }
            // Casilla de abajo
            if (row < Constants.TilesPerRow - 1)
            {
                int adjacentIndex = i + Constants.TilesPerRow;
                adjacencyMatrix[i, adjacentIndex] = 1;
                tiles[i].adjacency.Add(adjacentIndex);
            }
            // Casilla de la izquierda
            if (col > 0)
            {
                int adjacentIndex = i - 1;
                adjacencyMatrix[i, adjacentIndex] = 1;
                tiles[i].adjacency.Add(adjacentIndex);
            }
            // Casilla de la derecha
            if (col < Constants.TilesPerRow - 1)
            {
                int adjacentIndex = i + 1;
                adjacencyMatrix[i, adjacentIndex] = 1;
                tiles[i].adjacency.Add(adjacentIndex);
            }
        }
    }


    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        // Obtener la casilla actual del ladrón
        int currentTileIndex = robber.GetComponent<RobberMove>().currentTile;
        
        // Marcar la casilla actual como visitada
        tiles[currentTileIndex].current = true;
        
        // Calcular las casillas alcanzables desde la casilla actual del ladrón
        FindSelectableTiles(false);
        
        // Obtener las casillas alcanzables
        List<int> selectableTiles = new List<int>();
        foreach (Tile tile in tiles)
        {
            if (tile.selectable)
            {
                selectableTiles.Add(tile.numTile);
            }
        }
        
        // Elegir aleatoriamente una casilla alcanzable como destino del movimiento del ladrón
        int randomIndex = Random.Range(0, selectableTiles.Count);
        int destinationTileIndex = selectableTiles[randomIndex];
        
        // Mover al ladrón a la casilla elegida
        robber.GetComponent<RobberMove>().MoveToTile(tiles[destinationTileIndex]);
        
        // Actualizar la variable currentTile del ladrón a la nueva casilla
        robber.GetComponent<RobberMove>().currentTile = destinationTileIndex;
    }


    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool isCop)
    {
        // Obtener el índice de la casilla actual del ladrón o del policía según corresponda
        int currentIndex;
        if (isCop)
        {
            currentIndex = cops[clickedCop].GetComponent<CopMove>().currentTile;
        }
        else
        {
            currentIndex = robber.GetComponent<RobberMove>().currentTile;
        }

        // Marcar la casilla actual como visitada
        tiles[currentIndex].current = true;

        // Cola para el BFS
        Queue<Tile> queue = new Queue<Tile>();
        queue.Enqueue(tiles[currentIndex]);
        tiles[currentIndex].visited = true;

        // BFS para encontrar las casillas alcanzables hasta una distancia de 2 movimientos
        while (queue.Count > 0)
        {
            Tile currentTile = queue.Dequeue();
            int currentDistance = currentTile.distance;

            // Si la distancia actual es mayor o igual a 2, no necesitamos explorar más
            if (currentDistance < 2)
            {
                // Obtener las casillas adyacentes de la casilla actual
                foreach (int adjacentIndex in currentTile.adjacency)
                {
                    Tile adjacentTile = tiles[adjacentIndex];

                    if (!IsAdjacentTileVisitable(adjacentTile, currentIndex, isCop)) continue;

                    if (!adjacentTile.visited)
                    {
                        MarkUnvisitedAdjacentTile(adjacentTile, currentTile, currentDistance);
                        queue.Enqueue(adjacentTile);
                    }
                }
            }
        }
        ResetTilesBFS();
    }

    private void ResetTilesBFS(){
        foreach (Tile tile in tiles)
        {
            tile.visited = false;
            tile.distance = 0;
        }
    }

    private bool IsAdjacentTileVisitable(Tile adjacentTile, int currentIndex, bool isCop){
        // Regla 1: Las fichas no pueden moverse a su propia casilla
        if (adjacentTile.numTile == currentIndex)
            return false;
        // Regla 2: Un policía no puede moverse a la casilla ocupada por el ladrón
        if (isCop && (adjacentTile.numTile == robber.GetComponent<RobberMove>().currentTile))
            return false;
        // Regla 3: Un policía no puede moverse a una casilla que requiera pasar por la casilla ocupada por el otro policía
        if (isCop && (adjacentTile.numTile == cops[1 - clickedCop].GetComponent<CopMove>().currentTile))
            return false;
        return true;
    }

    private void MarkUnvisitedAdjacentTile(Tile adjacentTile, Tile currentTile, int currentDistance){
        adjacentTile.visited = true;
        adjacentTile.parent = currentTile;
        adjacentTile.distance = currentDistance + 1;
        adjacentTile.selectable = true;
    }
}

