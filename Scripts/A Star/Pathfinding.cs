using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

public class Pathfinding : MonoBehaviour
{
    GridSystem grid;

    //public Transform seeker, target;

    public List<Node> path;
    public LayerMask characterMask;
    //private float castRadius;

    //for training purposes
    public Node closestNode;
    private CharacterAgent characterAgent;

    private void Awake()
    {
        characterAgent = GetComponent<CharacterAgent>();
    }

    public void FindPath(Vector3 startPos, Vector3 targetPos)
    {
        //Stopwatch sw = new Stopwatch();
        //sw.Start();

        Node startNode= grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if(!startNode.walkable) startNode=grid.FindWalkableNeighbour(startNode);
        if(!targetNode.walkable) targetNode=grid.FindWalkableNeighbour(targetNode);

        //List<Node> openSet = new List<Node>();
        Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        int count = 0;
        while (openSet.Count > 0)
        {
            //Method: List
            //Node currentNode = openSet[0];

            //for(int i = 1; i < openSet.Count; i++)
            //{
            //    if (openSet[i].fCost<currentNode.fCost || openSet[i].fCost==currentNode.fCost && openSet[i].hCost<currentNode.hCost )         
            //        currentNode = openSet[i];
            //}

            //openSet.Remove(currentNode);

            count++;

            Node currentNode= openSet.RemoveFirst();
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                //sw.Stop();
                //print("Path found: " + sw.ElapsedMilliseconds + " ms.");
                RetracePath(startNode, targetNode);
                return;
            }               

            foreach (Node neightbour in grid.GetNeighbors(currentNode))
            {
                if (!neightbour.walkable || closedSet.Contains(neightbour)) continue;

                int newMovementCostToNeighbour= currentNode.gCost+ GetDistance(currentNode, neightbour);
                if(newMovementCostToNeighbour<  neightbour.gCost|| !openSet.Contains(neightbour))
                {
                    neightbour.gCost= newMovementCostToNeighbour;
                    neightbour.hCost=GetDistance(neightbour,targetNode);
                    neightbour.parent= currentNode;

                    if(!openSet.Contains(neightbour)) openSet.Add(neightbour);
                }
            }

            
        }
    }

    void RetracePath(Node startNode, Node endNode)
    {
        path = new List<Node>();
        Node currentNode = endNode;

        if (currentNode == startNode)       //1 node in path
        {
            path.Add(endNode);
        }
        while (currentNode != startNode)    //more than 1 node in path
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();

        if (path == null) UnityEngine.Debug.LogError("RetracePath: path is null and agent is " + transform.parent.name + transform.GetInstanceID());
        else if (path.Count == 1) UnityEngine.Debug.Log("RetracePath: path found with " + path.Count + " nodes");

        grid.path = path;
        closestNode = path[0];
        //castRadius = grid.nodeRadius * 6;      
    }

    int GetDistance(Node nodeA, Node nodeB)
    {
        int distX=Mathf.Abs(nodeA.gridX-nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if(distX>distY) return 14*  distY+ 10*(distX-distY);
        else            return 14 * distX + 10 * (distY - distX);
    }

    public void SetGrid(GridSystem gridSystem)
    {
        grid = gridSystem;
    }

    public void UpdateClosestNode()
    {
        if (closestNode == null) { UnityEngine.Debug.LogError("UpdateClosestNode: closestNode is null but path not null with" + path.Count +
            " and owner is " + transform.parent.name + transform.parent.GetInstanceID()); }
        //bool onPath = Physics.CheckSphere(grid.WorldPointFromNode(closestNode), castRadius, characterMask);
        if (path.Count > 1)
        {
            //UnityEngine.Debug.Log("UpdateClosestNode： On Path");
            path.Remove(path[0]);
            characterAgent.OnRightPath();        //give agent a reward
            closestNode = path[0];
        }
        else if (path.Count == 1) UnityEngine.Debug.Log("UpdateClosestNode： Successfully followed path!");
        else if (path == null) { UnityEngine.Debug.LogError("UpdateClosestNode: path is null and owner is " + transform.parent.name + transform.parent.GetInstanceID()); }

    }

}
