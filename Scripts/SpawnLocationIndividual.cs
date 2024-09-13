using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnLocationIndividual : MonoBehaviour
{
    [SerializeField] private Transform roadPoint;
    [SerializeField] private Transform[] pathwayPoint= new Transform[2];

    public Transform GetRoadPointTransform()
    {
        return roadPoint;
    }

    public Transform GetPathwayPointTransform() 
    {      
        return pathwayPoint[Random.Range(0, 2)];
    }
}
