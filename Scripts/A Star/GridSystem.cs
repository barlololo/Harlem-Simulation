using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridSystem : MonoBehaviour
{
    public LayerMask unwalkableMask;
    public Vector2 gridWorldSize;
    public float nodeRadius;
    Node[,] grid;
    //public Transform player;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    public List<Node> path;

    private void Awake()
    {
        nodeDiameter= nodeRadius*2;
        gridSizeX=  Mathf.RoundToInt(gridWorldSize.x/nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y/nodeDiameter);
        CreateGird();
    }
    
    public int MaxSize
    {
        get { return gridSizeX * gridSizeY; }
    }

    private void CreateGird()
    {
        grid= new Node[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft= transform.position- Vector3.right* gridWorldSize.x/2- Vector3.forward*gridWorldSize.y/2 ;

        for(int x = 0; x < gridSizeX; x++)
        {
            for(int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint= worldBottomLeft+ Vector3.right*(x*nodeDiameter+ nodeRadius)+ Vector3.forward*(y*nodeDiameter+ nodeRadius);
                bool walkable = !(Physics.CheckSphere(worldPoint, nodeRadius,unwalkableMask));
                grid[x,y]= new Node(walkable, worldPoint,x,y);
            }
        }

    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) neighbors.Add(grid[checkX, checkY]);
            }
        }

        return neighbors;
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x= Mathf.RoundToInt((gridSizeX-1)* percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x,y] ;
    }

    public Vector3 WorldPointFromNode(Node node)
    {
        if (node != null)
        {
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.forward * gridWorldSize.y / 2;
            Vector3 currentPos = worldBottomLeft + Vector3.right * (node.gridX * nodeDiameter + nodeRadius) + Vector3.forward * (node.gridY * nodeDiameter + nodeRadius);
            return currentPos;
        }
        else
        {
            Debug.LogError("WorldPointFromNode: input node is null");
            return transform.position;
        }         
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

    //    if (grid != null)
    //    {
    //        //Node playerNode = NodeFromWorldPoint(WorldCoordToGridCoord(player.position));

    //        foreach (Node node in grid)
    //        {
    //            Gizmos.color = (node.walkable) ? Color.white : Color.red;
    //            if (path != null)
    //                if (path.Contains(node))
    //                {
    //                    Gizmos.color = Color.black;
    //                    Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (nodeDiameter - .5f));
    //                }
    //            //if (playerNode == node) Gizmos.color = Color.cyan;
    //            //Gizmos.DrawWireCube(node.worldPosition, Vector3.one * (nodeDiameter - .5f));
    //            //Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - .5f));
    //        }
    //    }
    //}

    public Vector3 WorldCoordToGridCoord(Vector3 worldPosition)
    {
        return worldPosition- transform.position;
    }

    public Node FindWalkableNeighbour(Node node)
    {
        List<Node> neighbors = GetNeighbors(node);
        Node temp = node;
        foreach( Node neighbor in neighbors )
        {
            if (neighbor.walkable)
            {
                node = neighbor;                
                break;
            }                
        }

        if (temp == node) Debug.LogError("FindWalkableNeighbour: didn't find a walkable neighbour");
        return node;
    }
}
