using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;


/* One day I'll learn to document my own fucking code
 * PFManager, PFGraph, and PFGrid Pathfinding System
 * --------------------------------------------------
 * 
 * Overview:
 * This system consists of three main components - PFManager, PFGraph, and PFGrid - that work together to handle
 * pathfinding tasks in a Unity project. PFManager acts as the central hub for pathfinding operations, PFGraph handles
 * pathfinding over larger areas using Dijkstra's algorithm, and PFGrid is tailored for A* pathfinding on a tile-based
 * map.
 * 
 * PFManager:
 * - Manages the overall pathfinding process.
 * - Contains references to PFGraph and tilemaps for PFGrid.
 * - Provides static utility methods like GetNearestNode for external access.
 * 
 * PFGraph:
 * - Handles pathfinding across nodes (PFNode) using Dijkstra's algorithm.
 * - Best suited for larger, interconnected areas.
 * - Each PFNode represents a point in space, and nodes are linked to form a navigable graph.
 * - EnsureEdgeLinks feature ensures bi-directional links between adjacent nodes.
 * 
 * PFGrid:
 * - Specialized for A* pathfinding on a grid or tilemap.
 * - PFTile struct represents each tile in the grid, storing relevant pathfinding data.
 * - Useful for finer pathfinding tasks within a node of the PFGraph.
 * - Can handle walkability checks, world to grid position conversions, and more.
 * 
 * Usage:
 * 1. PFManager Initialization:
 *    - Create a PFManager object in your scene.
 *    - Assign the graphTransform to the PFManager to set up the node graph.
 * 
 * 2. PFGraph Setup:
 *    - Nodes are setup as children of graphTransform with PFNodeInterface components.
 *    - Call new PFGraph(graphTransform) to initialize the node graph.
 * 
 * 3. PFGrid Setup:
 *    - Assign the tilemap to the PFGrid for A* pathfinding on a grid.
 *    - Initialize with new PFGrid(name, tilemap) where name is a unique identifier.
 * 
 * 4. Pathfinding:
 *    - For larger area pathfinding, use PFGraph.GetDijkstraPath(startNode, endNode).
 *    - For grid-based pathfinding, use PFGrid.GetAStarPath(startWorldPos, endWorldPos).
 * 
 * 5. Utility Methods:
 *    - PFManager.GetNearestNode(position) finds the closest node to a given world position.
 * 
 * External Usage:
 * - To find a path from one point to another, first determine if the path is within a single node (use PFGrid) or spans across multiple nodes (use PFGraph).
 * - If using PFGraph, convert world positions to nodes using PFManager.GetNearestNode.
 * - For PFGrid, directly input world positions into PFGrid.GetAStarPath.
 * - Use the returned path data to guide your agents.
 * 
 * Notes:
 * - Ensure all PFNode positions are correctly set up in the Unity Editor.
 * - For PFGrid, walkable areas must be accurately represented in the tilemap.
 * - This system is designed for flexibility and can be adapted to different scales and types of environments.
 */


[BurstCompile]
public class PFGrid
{
    // Class for handling A* pathfinding for various agents.
    // In this case, the mimic will use this for PF across short distances (inter-node)
    public struct PFTile
    {
        // Yes, I named this "Tile" cause I already named PFNode for Dijkstra PF, because they're clearly vertices.
        // Call me a genius
        
        public Vector3 worldPosition;
        public bool walkable;

        public Vector3Int cameFrom;
        // G = distance from starting node
        // H = distance to end node
        // F = G + H (+ W)
        public int g, h, f;
        // W = wall proximity cost. 
        public int w;
    }

    public string name;
    public int sizeX, sizeY;
    public Tilemap tilemap;
    private BoundsInt cellBounds;
    
    private PFTile[,] tiles;

    private const int DIAGONAL = 14;
    private const int STRAIGHT = 10;

    public PFGrid(string name, Tilemap tilemap)
    {
        this.name = name;
        this.tilemap = tilemap;

        cellBounds = tilemap.cellBounds;
        sizeX = cellBounds.xMax - cellBounds.xMin;
        sizeY = cellBounds.yMax - cellBounds.yMin;
        
        tiles = new PFTile[sizeX, sizeY];
        
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                // x, y variable for indexes in array PFTile[,] tiles (=Array Position)
                // NOT tile positions in tilemap (=Tile Position)
                
                Vector3Int thisTilePos = new Vector3Int(x + cellBounds.xMin, y + cellBounds.yMin);
                if (!tilemap.HasTile(thisTilePos))
                {
                    tiles[x, y].walkable = true;
                }

                tiles[x, y].worldPosition = GetWorldPositionTile(thisTilePos, true);
            }
        }
    }
    
    public Vector3Int GetTilePositionWorld(Vector3 pos)
    {
        return tilemap.WorldToCell(pos);
    }

    public Vector3Int GetTilePositionArray(Vector3 pos)
    {
        return new Vector3Int(Mathf.RoundToInt(pos.x + cellBounds.xMin),
            Mathf.RoundToInt(pos.y + cellBounds.yMin));
    }

    public Vector3Int GetArrayPositionTile(Vector3Int pos)
    {
        return new Vector3Int(pos.x - cellBounds.xMin, pos.y - cellBounds.yMin);
    }

    public Vector3Int GetArrayPositionWorld(Vector3 pos)
    {
        return GetArrayPositionTile(GetTilePositionWorld(pos));
    }

    public Vector3 GetWorldPositionTile(Vector3Int pos, bool centerTiles)
    {
        Vector3 worldPos = tilemap.CellToWorld(pos);
        if (centerTiles)
            worldPos += tilemap.cellSize * 0.5f;
        return worldPos;
    }

    public void ResetTiles()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                tiles[x, y].f = 0;
                tiles[x, y].g = 9999;
                tiles[x, y].h = 0;
                tiles[x, y].cameFrom = Vector3Int.zero;
            }
        }
    }
    
    private Vector3Int[] GetNeighbourTiles(Vector3Int pos)
    {
        // Optimized Code feat. GPT4
        List<Vector3Int> neighbours = new List<Vector3Int>(8); // At most 8 neighbours

        for (int deltaX = -1; deltaX <= 1; deltaX++)
        {
            for (int deltaY = -1; deltaY <= 1; deltaY++)
            {
                // Skip the current tile
                if (deltaX == 0 && deltaY == 0)
                    continue;

                int newX = pos.x + deltaX;
                int newY = pos.y + deltaY;

                // Check for borders
                if (newX < 0 || newX >= sizeX || newY < 0 || newY >= sizeY)
                    continue;

                neighbours.Add(new Vector3Int(newX, newY, 0));
            }
        }

        return neighbours.ToArray();
    }

    private int GetDistanceCost(Vector3Int a, Vector3Int b)
    {
        int deltaX = Mathf.Abs(a.x - b.x);
        int deltaY = Mathf.Abs(a.y - b.y);

        return STRAIGHT * Mathf.Abs(deltaX - deltaY) + DIAGONAL * Mathf.Min(deltaX, deltaY);
    }

    private Vector3Int[] RetracePath(Vector3Int start, Vector3Int end)
    {
        List<Vector3Int> pathList = new List<Vector3Int> { end };

        Vector3Int thisPos = end;
        
        int maxStep = 9999;
        while (maxStep > 0)
        {
            maxStep--;

            thisPos = tiles[thisPos.x, thisPos.y].cameFrom;
            pathList.Add(thisPos);
            if(thisPos == start)
                break;
        }

        pathList.Reverse();

        return pathList.ToArray();
    }
    
    public Vector3[] GetAStarPath(Vector3 startWorldPos, Vector3 endWorldPos, int maxStep = 500, int wCost = 0, 
        bool preventCornerCutting = false)
    {
        Vector3Int start = GetArrayPositionWorld(startWorldPos);
        Vector3Int end = GetArrayPositionWorld(endWorldPos);

        // Start == End evaluation
        if (start == end)
        {
            // Debug.LogWarning("Start equals end");
            return Array.Empty<Vector3>();
        }
        
        // Index range evaluation 
        if (end.x >= tiles.GetLength(0) || end.y >= tiles.GetLength(1) ||
            start.x >= tiles.GetLength(0) || start.y >= tiles.GetLength(1))
        {
            Debug.LogWarning($"INDEX RANGE ERROR: {end.x}, {end.y}, {start.x}, {start.y}");
            return Array.Empty<Vector3>();
        }
        
        // Start & End position walkable evaluation
        if (!tiles[end.x, end.y].walkable || !tiles[start.x, start.y].walkable)
        {
            // Debug.LogWarning("Start or end not reachable");
            return Array.Empty<Vector3>();
        }
        
        // ----------------------------------------------------------------------------------------------------
        
        // Since we're working with arrays of structs, we should store each index for x&y instead of the tile data.
        // -> access PFGrid.tiles directly.
        // might use z for storing f costs. For now, only x and y is used in each Vector3Int.
        List<Vector3Int> openList = new List<Vector3Int>();
        HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();  // Okay
        
        ResetTiles();
        
        openList.Add(start);
        tiles[start.x, start.y].g = 0;
        tiles[start.x, start.y].h = GetDistanceCost(start, end);
        tiles[start.x, start.y].f = GetDistanceCost(start, end);
        Vector3Int currentPos = start;

        int step = 0;
        
        while (openList.Count > 0 && step < maxStep)
        {
            step++;
            
            Vector3Int[] neighbours = GetNeighbourTiles(currentPos);
            foreach (var neighbour in neighbours)
            {
                PFTile neighbourTile = tiles[neighbour.x, neighbour.y];  // value type. read only.
                
                if(closedList.Contains(neighbour)) continue;
                
                if (!neighbourTile.walkable)
                {
                    closedList.Add(neighbour);
                    continue;
                }
                
                // Calculate costs for this neighbour tile
                // -------------------------------------------------------
                PFTile currentTile = tiles[currentPos.x, currentPos.y];
                
                int g = (neighbour - currentPos).x * (neighbour - currentPos).y == 0 ? 
                    STRAIGHT + currentTile.g : DIAGONAL + currentTile.g;
                int h = GetDistanceCost(neighbour, end);
                if (tiles[neighbour.x, neighbour.y].g <= g)
                {
                    // Do not update g(and f) if it's original value smaller than new value
                    g = tiles[neighbour.x, neighbour.y].g;
                }
                else
                {
                    // Update g(and f) along with its cameFrom, which would now be currentPos
                    tiles[neighbour.x, neighbour.y].g = g;
                    tiles[neighbour.x, neighbour.y].cameFrom = currentPos;
                }

                int w = 0;
                if (wCost>0)
                {
                    Vector3Int[] neighbourNeighbours = GetNeighbourTiles(neighbour);
                    if (neighbourNeighbours.Any(n => !tiles[n.x, n.y].walkable))
                    {
                        w = wCost;
                        // tiles[neighbour.x, neighbour.y].w = 3;
                    }
                }
                
                
                int f = g + h + w;
                // -------------------------------------------------------
                
                // Not actually necessary.
                // tiles[neighbour.x, neighbour.y].h = h; 
                
                tiles[neighbour.x, neighbour.y].f = f;
                
                
                openList.Add(neighbour);

                if (neighbour == end)
                {
                    // Pathfinding Complete
                    Vector3Int[] arrayPath = RetracePath(start, end);
                    Vector3[] worldPath = arrayPath.Select(p => tiles[p.x, p.y].worldPosition).ToArray();
                    return worldPath;
                }
            }

            openList.Remove(currentPos);
            closedList.Add(currentPos);
            
            int minFCost = 9999;
            foreach (Vector3Int tilePos in openList)
            {
                if (preventCornerCutting)
                {
                    Vector3Int deltaPos = tilePos - currentPos;
                    if (deltaPos.x != 0 && deltaPos.y != 0 && 
                        (!tiles[tilePos.x, currentPos.y].walkable || !tiles[currentPos.x, tilePos.y].walkable))
                    {
                        continue;
                    }
                }
                
                if (tiles[tilePos.x, tilePos.y].f < minFCost && !closedList.Contains(tilePos))
                {
                    minFCost = tiles[tilePos.x, tilePos.y].f;
                    currentPos = tilePos;
                }
            }
        }
        
        Debug.LogWarning("Pathfinding Failed: from " + startWorldPos + " to " + endWorldPos);
        return Array.Empty<Vector3>();
    }
}

//--------------------------------------------------------------------------------------------------------------------
[BurstCompile]
public class PFGraph
{
    [SerializeField] private Transform graphTransform;
    public PFNode[] nodes;              // Determines PFNode index
    [SerializeField] private bool ensureEdgeLinks;        // Checks if adjacent vertices both have each other in their respective adjacentNodes array 
    private Dictionary<PFNode, int> nodeIndexes;
    private int[] prevNodeIndexes;      // Previous PFNode the search algorithm came from (Dijkstra's)
    private float[] minDistanceSum;     // Shortest distance found so far for each vertex
    private bool[] nodeChecked;         // True if node has been checked
    private float[,] distanceMatrix;    // Stores distances between PFNodes
    private int nodeCount;

    public PFGraph(Transform graphTransform, bool ensureEdgeLinks = default)
    {
        this.graphTransform = graphTransform;
        this.ensureEdgeLinks = ensureEdgeLinks;
        nodeCount = graphTransform.childCount;
        nodes = new PFNode[nodeCount];
        SetupNodes();
    }

    void ClearCache()
    {
        prevNodeIndexes = new int[nodeCount];
        minDistanceSum = new float[nodeCount];
        nodeChecked = new bool[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            prevNodeIndexes[i] = -1;
            minDistanceSum[i] = Mathf.Infinity;
        }
    }
    
    
    void SetupNodes()
    {
        nodeIndexes = new Dictionary<PFNode, int>();
        distanceMatrix = new float[nodeCount, nodeCount];

        ClearCache();
        
        // #1 Setup nodes array
        for (int i = 0; i < nodeCount; i++)
        {
            Transform nodeTransform = graphTransform.GetChild(i);
            PFNodeInterface nodeInterface = nodeTransform.GetComponent<PFNodeInterface>();
            nodes[i] = nodeInterface.node;
            nodeInterface.index = i;
            nodeIndexes.Add(nodes[i], i);
            nodeTransform.GetComponent<SpriteRenderer>().color = Color.grey;
        }

        for (int i = 0; i < nodeCount; i++)
        {
            PFNode thisNode = nodes[i];
            PFNode[] adjacentNodes = nodes[i].adjacentNodes;
            
            // print(thisNode.position);
            
            for (int j = 0; j < adjacentNodes.Length; j++)
            {
                // Ensure nodes are connected both ways
                if (ensureEdgeLinks && !adjacentNodes[j].adjacentNodes.Contains(thisNode))
                {
                    adjacentNodes[j].adjacentNodes = adjacentNodes[j].adjacentNodes.Concat(new[] { thisNode }).ToArray();
                    // Update distanceMatrix accordingly
                    distanceMatrix[nodeIndexes[adjacentNodes[j]], i] = Vector2.Distance(thisNode.position, adjacentNodes[j].position);
                }
                
                distanceMatrix[i, nodeIndexes[adjacentNodes[j]]] =
                    Vector2.Distance(thisNode.position, adjacentNodes[j].position);
            }
        }
    }
    
    //-----------------------------------------------------------------------------------------------------------------

    public PFNode[] GetDijkstraPath(PFNode start, PFNode end, int maxStep=9999, bool debug = false)
    {
        void DebugDijkstra(List<PFNode> path)
        {
            for (int i = 0; i < nodeCount; i++)
            {
                graphTransform.GetChild(i).GetComponent<SpriteRenderer>().color = Color.grey;
            }
            
            foreach (var node in path)
            {
                graphTransform.GetChild(nodeIndexes[node]).GetComponent<SpriteRenderer>().color = Color.blue;
            }
            graphTransform.GetChild(nodeIndexes[start]).GetComponent<SpriteRenderer>().color = Color.green;
            graphTransform.GetChild(nodeIndexes[end]).GetComponent<SpriteRenderer>().color = Color.red;
            
        }
        
        if (start == end)
        {
            // Debug.LogWarning("Starting node must not equal end node");
            return Array.Empty<PFNode>();
        }

        ClearCache();
        
        int step = 0;
        PFNode currentNode = start;
        minDistanceSum[nodeIndexes[start]] = 0f;
        int startNodeIndex = nodeIndexes[start];
        
        while (step < maxStep)
        {
            step++;;
            int currentNodeIndex = nodeIndexes[currentNode];
            
            foreach (var adjacentNode in currentNode.adjacentNodes)
            {
                // # 1. Update MDS for this adjacent node
                // What the [fuck] [kind [of code] is this]
                int adjacentNodeIndex = nodeIndexes[adjacentNode];
                
                if(nodeChecked[adjacentNodeIndex]) continue;
                
                if (minDistanceSum[currentNodeIndex] +
                    distanceMatrix[currentNodeIndex, adjacentNodeIndex] < minDistanceSum[adjacentNodeIndex])
                {
                    minDistanceSum[adjacentNodeIndex] = minDistanceSum[currentNodeIndex] +
                                                distanceMatrix[currentNodeIndex,
                                                    adjacentNodeIndex];
                    prevNodeIndexes[adjacentNodeIndex] = currentNodeIndex;
                }
                
            }
            nodeChecked[currentNodeIndex] = true;

            float shortestMDS = Mathf.Infinity;
            for (int i = 0; i < nodeCount; i++)
            {
                if(nodeChecked[i]) continue;

                if (minDistanceSum[i] < shortestMDS)
                {
                    shortestMDS = minDistanceSum[i];
                    currentNode = nodes[i];
                }
            }
                
                
            // print("- " + nodeIndexes[currentNode]);
            if (currentNode == end)
            {
                // Destination Reached!
                List<PFNode> path = new List<PFNode>();
                path.Add(end);
                int prevNodeIndex = prevNodeIndexes[nodeIndexes[currentNode]];
                while (prevNodeIndex != startNodeIndex)
                {
                    path.Add(nodes[prevNodeIndex]);
                    prevNodeIndex = prevNodeIndexes[prevNodeIndex];
                }
                path.Add(start);
                
                path.Reverse();
                
                if(debug)
                    DebugDijkstra(path);

                return path.ToArray();
            }
        }

        // No path found within max step.
        Debug.LogWarning("No path found :(");
        Debug.Log(start.position + " -> " + end.position);
        return Array.Empty<PFNode>();
    }
}

//--------------------------------------------------------------------------------------------------------------------

public class PFManager : MonoBehaviour
{
    public PFGraph pfGraph;
    public Transform graphTransform;
    public LayerMask wallLayers;
    
    private void Start()
    {
        pfGraph = new PFGraph(graphTransform, true);
    }
    
    public static PFNode GetNearestNode(Vector3 position)
    {
        PFNode[] nodes = GM.GetPFManager().pfGraph.nodes;
        PFNode nearestNode = nodes[0];
        float nearestNodeDistSqr = Mathf.Infinity;
        LayerMask wallLayers = GM.GetPFManager().wallLayers;
        
        foreach (var node in nodes)
        {
            Vector2 delta = new Vector2(node.position.x - position.x, node.position.y - position.y);
            float dSqr = delta.sqrMagnitude;
            if (dSqr < nearestNodeDistSqr)
            {
                if(Physics2D.Raycast(position, node.position - (Vector2)position,
                       delta.magnitude, wallLayers).collider)
                    continue;
                nearestNode = node;
                nearestNodeDistSqr = dSqr;
            }
        }

        return nearestNode;
    }
}
