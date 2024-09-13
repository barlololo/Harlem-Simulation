using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TerrainTools;

public class ContainerForSignals : MonoBehaviour
{
    private TrackCheckpoints trackCheckpoints;
    private bool isInOperation;
    public bool hasSignals = false;

    public void StoreSignal(TrackCheckpoints trackSignal, bool operationSignal)
    {
        trackCheckpoints=trackSignal;
        isInOperation=operationSignal;
        hasSignals=true;
    }

    public TrackCheckpoints TransferTrackSignal()
    {
        return trackCheckpoints;
    }

    public bool TransferOperationSignal()
    {
        return isInOperation;
    }

    private void Start()
    {
        //for Heuristic mode
        if(!hasSignals) transform.GetChild(0).gameObject.SetActive(true);        //enabled mlagents
    }
}
