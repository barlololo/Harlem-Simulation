using System;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

/// <summary>
/// manages all checkpoints and monitors all vehicles on this route
/// </summary>
public class TrackCheckpoints : MonoBehaviour
{
    [Header("Route Ends:")]
    [SerializeField] private Transform startLocation;
    [SerializeField] private Transform endLocation;

    [Header("Time Schedule:")]
    [SerializeField] private TextAsset inputForBusSchedule;     //raw file for bus time schedule

    private List<CheckpointSingle> checkpointSingleList;        //each checkpoint
    private List<Transform> carTransformList;                   //document all cars in this route 
    private List<int> nextCheckpointSingleIndexList;            //cars' corresponding number to the track

    //two events on how the car enters the checkpoint
    public event EventHandler<OnDriverCorrectCheckpointEventArgs> OnDriverCorrectCheckpoint;
    public event EventHandler<OnDriverWrongCheckpointEventArgs> OnDriverWrongCheckpoint;
    public class OnDriverCorrectCheckpointEventArgs : EventArgs
    {
        public Transform carTransformCorrect;
    }
    public class OnDriverWrongCheckpointEventArgs : EventArgs
    {
        public Transform carTransformWrong;
    }

    private void Awake()
    {
        //initialize the list of checkpoints for this route
        checkpointSingleList = new List<CheckpointSingle>();
        foreach (Transform checkpointSingleTransform in transform)
        {            
            CheckpointSingle checkpointSingle = checkpointSingleTransform.GetComponent<CheckpointSingle>();
            checkpointSingle.SetTrackCheckpoints(this);
            checkpointSingleList.Add(checkpointSingle);
        }
        nextCheckpointSingleIndexList = new List<int>();

        //is in Heuristic mode or freeplay
        if (carTransformList != null)
            foreach (Transform car in carTransformList) nextCheckpointSingleIndexList.Add(0);
        else carTransformList = new List<Transform>();
    }

    /// <summary>
    /// test if the car passes through the correct checkpoint,
    /// invoke either one of two events and let the agent know
    /// </summary>
    /// <param name="checkpointSingle"></param>
    /// <param name="carTransform"></param>
    public void CarThroughCheckpoint(CheckpointSingle checkpointSingle, Transform carTransform)
    {        
        //index for Car NO.xx
        int nextCheckpointSingleIndex = nextCheckpointSingleIndexList[carTransformList.IndexOf(carTransform)];

        if (checkpointSingleList.IndexOf(checkpointSingle) == nextCheckpointSingleIndex)
        {
            //Debug.Log("Correct " );
            if (nextCheckpointSingleIndex < checkpointSingleList.Count-1)      //keep last CheckpointSingle as reference till the end
            {
                nextCheckpointSingleIndexList[carTransformList.IndexOf(carTransform)]
                = (nextCheckpointSingleIndex + 1) % checkpointSingleList.Count;     //avoid the overflow of index number
            }            
            OnDriverCorrectCheckpoint?.Invoke(this, new OnDriverCorrectCheckpointEventArgs { carTransformCorrect = carTransform });

            UpdateTagForPreviousAndNextCheckpoint();
            carTransform.GetComponentInChildren<CarDriverAgent>().UpdateNextCheckpointSingle(GetNextCheckpoint(carTransform));
        }
        else
        {
            //Debug.Log("Wrong ");
            OnDriverWrongCheckpoint?.Invoke(this, new OnDriverWrongCheckpointEventArgs { carTransformWrong = carTransform });

            UpdateTagInEndepisode(nextCheckpointSingleIndex);
            //carTransform.GetComponentInChildren<CarDriverAgent>().UpdateNextCheckpointSingle(GetNextCheckpoint(carTransform));
        }
    }

    public Transform GetNextCheckpoint(Transform carTransform)
    {
        int nextCheckpointSingleIndex = nextCheckpointSingleIndexList[carTransformList.IndexOf(carTransform)];
        return checkpointSingleList[nextCheckpointSingleIndex].transform;
    }

    /// <summary>
    /// reset checkpoint index for this car in this route
    /// </summary>
    /// <param name="carTransform"></param>
    public void ResetCheckpointIndex(Transform carTransform)
    {
        //reset to default
        nextCheckpointSingleIndexList[carTransformList.IndexOf(carTransform)] = 0;  

        //int index = carTransformList.IndexOf(carTransform);
        //if (index != -1)
        //{
        //    nextCheckpointSingleIndexList[index] = 0;
        //    UpdateTagForPreviousAndNextCheckpoint();
        //}
        //else
        //{
        //    Debug.LogError("Car transform not found in the list.");
        //}

        UpdateTagForPreviousAndNextCheckpoint();
    }

    public Transform GetSpawnLocationTransform()
    {
        return startLocation;
    }

    public Transform GetFinalLocationTransform()
    {
        return endLocation;
    }

    /// <summary>
    /// add a car list to run on this route and track each car in an index list
    /// </summary>
    /// <param name="carTransform"></param>
    public void AddACar(Transform carTransform)
    {
        carTransformList.Add(carTransform);
        nextCheckpointSingleIndexList.Add(0);
        //Debug.Log(carTransform.name + " is added ");
        UpdateTagForPreviousAndNextCheckpoint();
    }

    /// <summary>
    /// remove the car from this route and update
    /// </summary>
    /// <param name="carTransform"></param>
    public void RemoveACar(Transform carTransform)
    {
        nextCheckpointSingleIndexList.RemoveAt(carTransformList.IndexOf(carTransform));
        carTransformList.Remove(carTransform);
    }


    private void UpdateTagForPreviousAndNextCheckpoint()
    {
        foreach(var entry in nextCheckpointSingleIndexList)
        {
            if(entry-1>=0) checkpointSingleList[entry - 1].gameObject.layer = LayerMask.NameToLayer("TransparentFX");
            else checkpointSingleList[0].gameObject.layer = LayerMask.NameToLayer("TransparentFX");
        }

        foreach (var entry in nextCheckpointSingleIndexList)
        {
            checkpointSingleList[entry].gameObject.layer = LayerMask.NameToLayer("Checkpoint");
        }
    }


    public void UpdateTagInEndepisode(Transform carTransform)
    {
        int next = nextCheckpointSingleIndexList[carTransformList.IndexOf(carTransform)];
        checkpointSingleList[next].gameObject.layer = LayerMask.NameToLayer("TransparentFX");
    }

    public void UpdateTagInEndepisode(int index)
    {
        checkpointSingleList[index].gameObject.layer = LayerMask.NameToLayer("TransparentFX");
    }

    public bool IsListInitialized()
    {
        return carTransformList != null;
    }

    /// <summary>
    /// read raw file data into string lists for each route
    /// </summary>
    /// <returns></returns>
    public List<string> ReadFileToStrings()
    {
        List<string> processedInputForBusSchedule = new List<string>();
        var splitFile = new string[] { "\r\n", "\r", "\n" };
        var Lines = inputForBusSchedule.text.Split(splitFile, StringSplitOptions.RemoveEmptyEntries);

        if (inputForBusSchedule.name == "M1_S")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[line.Length - 1]);
            }
        }

        if (inputForBusSchedule.name == "M1_N")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                if (line[0] == "LTD") processedInputForBusSchedule.Add(line[1]);
                else processedInputForBusSchedule.Add(line[0]);
            }
        }

        if (inputForBusSchedule.name == "BX33_W")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[0]);
            }
        }

        if (inputForBusSchedule.name == "BX33_E")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[3]);
            }
        }

        if (inputForBusSchedule.name == "M2_S")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                if (line[0] == "LTD") processedInputForBusSchedule.Add(line[6]);
                else processedInputForBusSchedule.Add(line[5]);
            }
        }

        if (inputForBusSchedule.name == "M2_N")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                if (line[0] == "LTD") processedInputForBusSchedule.Add(line[2]);
                else processedInputForBusSchedule.Add(line[1]);
            }
        }

        if (inputForBusSchedule.name == "M7_S")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[5]);
            }
        }

        if (inputForBusSchedule.name == "M7_N")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[0]);
            }
        }

        if (inputForBusSchedule.name == "M10_S")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[4]);
            }
        }

        if (inputForBusSchedule.name == "M10_N")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[0]);
            }
        }

        if (inputForBusSchedule.name == "M102_S")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[5]);
            }
        }

        if (inputForBusSchedule.name == "M102_N")
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                var line = Lines[i].Split(" ");
                processedInputForBusSchedule.Add(line[0]);
            }
        }

        return processedInputForBusSchedule;
    }

}
