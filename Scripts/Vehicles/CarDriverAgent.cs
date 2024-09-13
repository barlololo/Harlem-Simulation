using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.Pool;
using System.Collections;
using UnityEngine.TerrainTools;

/// <summary>
/// an agent is driving the car and receiving orders from the bus station office.
/// attached transform is the child of car prefab
/// </summary>
public class CarDriverAgent : Agent
{
    [Header("Route Info:")]
    [SerializeField] private TrackCheckpoints trackCheckpoints;
    private bool isBusCenterInOperation = false;      //to decide whether prefabs should be destroyed

    private readonly float carHeight = 2f;           //only used in GroundCheck, might be deleted if better options found
    private CarDriver carDriver;
    //private GameInput gameInput;

    //Training Configs
    private bool finishedTrainingOnThisRoute=false; //for traiing on all routes
    private int trainingRouteIndex=0;
    private Transform nextCheckpointSingle;         //updated by signal from checkpoint
    //private bool trainingSucceed = false;

    private ObjectPool<Transform> _driverPool;

    private void TrackCheckpoints_OnDriverCorrectCheckpoint(object sender, TrackCheckpoints.OnDriverCorrectCheckpointEventArgs eventArgs)
    {
        if (eventArgs.carTransformCorrect == transform.parent)
        {           
            AddReward(1f);
            InvokeRepeating(nameof(OffTrackCheck), 3,2);       //check if it's going off the track
        }            
    }
    private void TrackCheckpoints_OnDriverWrongCheckpoint(object sender, TrackCheckpoints.OnDriverWrongCheckpointEventArgs eventArgs)
    {
        if (eventArgs.carTransformWrong == transform.parent) AddReward(-1f);
        CancelInvoke();
        EndEpisode(); 
        UpdateNextCheckpointSingle(trackCheckpoints.GetNextCheckpoint(transform.parent));
    }


    public override void Initialize()
    {
        carDriver = GetComponent<CarDriver>();

        //trying to get signals from BusSchedule        
        ContainerForSignals container = transform.parent.GetComponent<ContainerForSignals>();
        if(container.hasSignals)
        {
            trackCheckpoints = container.TransferTrackSignal();
            isBusCenterInOperation = container.TransferOperationSignal();   
        }

        //in training mode, randomly select a TrackCheckpoints
        if (!isBusCenterInOperation)
        {
            if(trackCheckpoints == null)
            {
                //Debug.Log("Heuristic or Freeplay Mode: No TrackCheckpoints");

                Transform busRoutesCollection = GameObject.FindGameObjectWithTag("BusRoutesCollection").transform;
                //routeIndex = Random.Range(0, busRoutesCollection.childCount);
                //trackCheckpoints = busRoutesCollection.GetChild(routeIndex).GetComponent<TrackCheckpoints>();
                trackCheckpoints = busRoutesCollection.GetChild(0).GetComponent<TrackCheckpoints>();
                trackCheckpoints.AddACar(transform.parent);

                nextCheckpointSingle = trackCheckpoints.GetNextCheckpoint(transform.parent);
            }
            else Debug.LogError("Heuristic or Freeplay Mode: should have no TrackCheckpoints but not");
        }
        //in simulation mode, get siganls from BusSchedule
        else
        {
            if (trackCheckpoints != null) Debug.Log("BusSchedule Operation Mode: everything is all set");
            else Debug.LogError("BusSchedule Operation Mode: signals not received in Initialize()");
        }

        //subscribe to 2 events
        trackCheckpoints.OnDriverCorrectCheckpoint += TrackCheckpoints_OnDriverCorrectCheckpoint;
        trackCheckpoints.OnDriverWrongCheckpoint += TrackCheckpoints_OnDriverWrongCheckpoint;
    }

    public override void OnEpisodeBegin()
    {
        if (!isBusCenterInOperation && finishedTrainingOnThisRoute)
        {
            //Debug.Log("Heuristic: new track for training");

            RemoveFromRoute();
            Transform busRoutesCollection = GameObject.FindGameObjectWithTag("BusRoutesCollection").transform;

            trackCheckpoints = busRoutesCollection.GetChild(trainingRouteIndex).GetComponent<TrackCheckpoints>();
            //trackCheckpoints = busRoutesCollection.GetChild(0).GetComponent<TrackCheckpoints>();
            trackCheckpoints.AddACar(transform.parent);
            nextCheckpointSingle = trackCheckpoints.GetNextCheckpoint(transform.parent);

            //subscribe to 2 events
            trackCheckpoints.OnDriverCorrectCheckpoint += TrackCheckpoints_OnDriverCorrectCheckpoint;
            trackCheckpoints.OnDriverWrongCheckpoint += TrackCheckpoints_OnDriverWrongCheckpoint;

            finishedTrainingOnThisRoute = false;
        }

        if (trackCheckpoints != null)
        {
            //Debug.Log("Training in " + isBusCenterInOperation + " Bus Center Operation");

            Transform roadPoint = trackCheckpoints.GetSpawnLocationTransform().GetComponent<SpawnLocationIndividual>().GetRoadPointTransform();
            transform.position = roadPoint.position;
            transform.forward = roadPoint.forward;
            trackCheckpoints.ResetCheckpointIndex(transform.parent);
            nextCheckpointSingle = trackCheckpoints.GetNextCheckpoint(transform.parent);      //reset 
            carDriver.StopAndReset();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        AddReward(-.0001f);     //longer the driver drives the car, more penalty comes
        
        sensor.AddObservation(Vector3.Dot(transform.forward, nextCheckpointSingle.forward));    //orientation observation

        sensor.AddObservation(carDriver.RotationSpeedRatio());  //control the speed

        //Space size: 2
        //sensor.AddObservation(directionDot);
        //sensor.AddObservation(carDriver.RotationSpeedRatio());

        //Space size: 10
        //sensor.AddObservation(transform.position);
        //sensor.AddObservation(checkpointForwardFirst.position);
        //sensor.AddObservation(carDriver.VelocityVector());
        //sensor.AddObservation(directionDot);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float acceleration = 0f;
        float turn = 0f;
        float brake= 0f;

        switch(actions.DiscreteActions[0]) {
            case 0: acceleration = 0f; break;
            case 1: acceleration = 1f; break;
            case 2: acceleration = -1f; break;
        }

        switch(actions.DiscreteActions[1])
        {
            case 0: turn = 0f; break;
            case 1: turn = 1f; break;
            case 2: turn = -1f; break;
        }

        switch (actions.DiscreteActions[2])
        {
            case 0: brake = 0f; break;
            case 1: brake = 1f; break;
        }

        carDriver.SetInput(acceleration, turn, brake);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        int accelerationAction = 0;
        if (Input.GetKey(KeyCode.UpArrow)) accelerationAction = 1;
        if (Input.GetKey(KeyCode.DownArrow)) accelerationAction = 2;

        int turnAction = 0; 
        if (Input.GetKey(KeyCode.RightArrow)) turnAction = 1;
        if (Input.GetKey(KeyCode.LeftArrow)) turnAction = 2;

        int brakeAction = 0;
        if (Input.GetKey(KeyCode.Space)) brakeAction = 1;

        ActionSegment<int> discreteActions= actionsOut.DiscreteActions;
        discreteActions[0]= accelerationAction;
        discreteActions[1]= turnAction;
        discreteActions[2]= brakeAction;
      
        carDriver.SetInput(accelerationAction, turnAction, brakeAction);
    }

    private void OffTrackCheck()
    {
        //make sure the bus doesn't go off the track
        if (Vector3.Distance(transform.position, nextCheckpointSingle.position) >40f)
        {
            AddReward(-1f);
            Debug.Log("OffTrackCheck： the bus goes off the track");
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);
            CancelInvoke();
            EndEpisode();

            if (isBusCenterInOperation) RemoveFromRouteAndDestroy();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //punish if hitting the  training area boundary
        if (other.CompareTag("TrainingArea"))
        {            
            Debug.Log("RESTART! hit the training area boundary");
            AddReward(-1f);
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);

            if (isBusCenterInOperation)
            {
                //RemoveFromRouteAndDestroy();
                CancelInvoke();
                EndEpisode();
                //trainingSucceed = true;
                RemoveFromRouteAndDestroy();
            }
            else
            {
                CancelInvoke();
                EndEpisode();
            } 
                
        }

        //reach the destination
        if (other.transform==trackCheckpoints.GetFinalLocationTransform())
        {
            Debug.Log("REACHED DESTINATION.");
            AddReward(1f);
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);
                       
            //if (isBusCenterInOperation) RemoveFromRouteAndDestroy();
            if (isBusCenterInOperation)
            {                
                EndEpisode();
                RemoveFromRouteAndDestroy();
                //trainingSucceed = true;
            }
            else
            {
                finishedTrainingOnThisRoute = true;
                RotateRouteIndex();     //be ready to go to next route
                CancelInvoke();
                EndEpisode();
            }   
                
        }
    }




    /// <summary>
    /// check if the agent drives onto the pathway
    /// </summary>
    /// <returns></returns>
    private bool GroundCheck()
    {
        Physics.Raycast(transform.position, Vector3.down,out RaycastHit raycastHit,carHeight);        
        if(raycastHit.collider!= null && raycastHit.collider.CompareTag("Pathway")) return true;
        return false;
    }

    private void FixedUpdate()
    {
        if (GroundCheck())
        {
            Debug.Log("stay on the road");
            AddReward(-0.5f);
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);
            CancelInvoke();
            EndEpisode();

            if (isBusCenterInOperation) RemoveFromRouteAndDestroy();
        }

        if (StepCount > MaxStep - 5)
        {
            Debug.Log("run out of steps");
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);
            CancelInvoke();
            EndEpisode();

            if (isBusCenterInOperation) RemoveFromRouteAndDestroy();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.CompareTag("Car"))
        {
            Debug.Log("Hit another car"+ nameof(CheckCarAccident));

            Invoke(nameof(CheckCarAccident),2f);
        }
    }

    private void CheckCarAccident()
    {
        //car roll over
        if (Mathf.Abs(transform.localEulerAngles.x) > 10 || Mathf.Abs(transform.localEulerAngles.z) > 10)
        {
            Debug.Log("Car Accident: Roll Over!");

            AddReward(-1f);
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);
            if (isBusCenterInOperation)
            {
                //RemoveFromRouteAndDestroy();
                CancelInvoke();
                EndEpisode();
                //trainingSucceed = true;
                RemoveFromRouteAndDestroy();
            }
            else
            {
                CancelInvoke();
                EndEpisode();
            }
        }
        else if (transform.position.y > 10)
        {
            Debug.Log("Car Accident: Fly High!");

            AddReward(-1f);
            trackCheckpoints.UpdateTagInEndepisode(transform.parent);
            if (isBusCenterInOperation)
            {
                //RemoveFromRouteAndDestroy();
                CancelInvoke();
                EndEpisode();
                //trainingSucceed = true;
                RemoveFromRouteAndDestroy();
            }
            else
            {
                CancelInvoke();
                EndEpisode();
            }
        }
        //if (transform.parent!=null)
        //{
            
        //}

    }

    /// <summary>
    /// agent is assigned a route from BusSchedule if not choosing one manually in Heuristic mode
    /// </summary>
    /// <param name="assignedTrackCheckpoints"></param>
    public void SetTrackcheckpointsToAgent(TrackCheckpoints assignedTrackCheckpoints)
    {
        trackCheckpoints = assignedTrackCheckpoints;
        isBusCenterInOperation = true;
        //Debug.Log(assignedTrackCheckpoints.ToString() + " route is added" + isBusCenterInOperation);
    }

    /// <summary>
    /// retrieve the assigned TrackCheckpoints this agent is dealing with
    /// </summary>
    /// <returns></returns>
    public TrackCheckpoints GetAssignedTrackCheckpoints()
    {
        return trackCheckpoints;
    }

    /// <summary>
    /// the agent leaves the city and clears all used objects
    /// </summary>
    private void RemoveFromRouteAndDestroy()
    {
        //trainingSucceed = true;
        trackCheckpoints.OnDriverCorrectCheckpoint -= TrackCheckpoints_OnDriverCorrectCheckpoint;
        trackCheckpoints.OnDriverWrongCheckpoint -= TrackCheckpoints_OnDriverWrongCheckpoint;
        trackCheckpoints.RemoveACar(transform.parent);

        FindAnyObjectByType<UIManager>().RemoveBusFromRoute(FindRouteIndex());

        //Destroy(transform.parent.gameObject);
        gameObject.SetActive(false);        //disabled mlagents
        _driverPool.Release(transform.parent);
    }

    /// <summary>
    /// the agent finishes this round of training
    /// </summary>
    private void RemoveFromRoute()
    {
        trackCheckpoints.OnDriverCorrectCheckpoint -= TrackCheckpoints_OnDriverCorrectCheckpoint;
        trackCheckpoints.OnDriverWrongCheckpoint -= TrackCheckpoints_OnDriverWrongCheckpoint;

        trackCheckpoints.RemoveACar(transform.parent);
    }

    private void RotateRouteIndex()
    {
        trainingRouteIndex = (trainingRouteIndex + 1) % 12;
    }

    public void UpdateNextCheckpointSingle(Transform transform)
    {
        nextCheckpointSingle = transform;
    }

    public void SetDriverPool(ObjectPool<Transform> pool)
    {
        _driverPool = pool;
    }

    private int FindRouteIndex()
    {
        return FindFirstObjectByType<BusSchedule>().GetRouteIndex(trackCheckpoints);
    }

    public TrackCheckpoints GetRouteTrack()
    {
        return trackCheckpoints;
    }

}
