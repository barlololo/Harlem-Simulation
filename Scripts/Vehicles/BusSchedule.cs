using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// bus center that manages time schedule for all routes, 
/// also sends orders to all employee drivers
/// </summary>
public class BusSchedule : MonoBehaviour
{
    [Header("Routes And Buses:")]
    [SerializeField] private List<Transform> busPrefab;
    [SerializeField] private List<Transform> busRoutes;

    private List<List<int>> timerForAllRoutes;
    private int[] firstTimerForEachRoute ;
    private float timer;

    [SerializeField] private bool isSendingSignals=false;        //used for Heuristic mode and freeplay
    [SerializeField] private Transform busParent;


    private ObjectPool<Transform> _driverPool;
    private int currentRouteIndex;

    [SerializeField] private UIManager uiManager;

    private void Awake()
    {
        if (isSendingSignals)
        {
            //initialize time schedule for each route in integer number values
            timerForAllRoutes = new List<List<int>>();
            InitializeTimerForAllRoutes();

            //set the time list for when to depart a car for each route
            firstTimerForEachRoute = new int[busRoutes.Count];
            foreach (List<int> route in timerForAllRoutes)   firstTimerForEachRoute[timerForAllRoutes.IndexOf(route)] = route[0];

            //set the starting time, starting from midnight
            timer = 64f;
        }
    }

    private void Update()
    {        
        //start the simulation if not in training mode
        if (isSendingSignals)   
        {
            timer += Time.deltaTime;

            //check each list whether a bus shall depart
            for (int i = 0; i < firstTimerForEachRoute.Length; i++)
            {
                if (timer > firstTimerForEachRoute[i])
                {
                    //send a bus to this track once the scheduled time arrives                    
                    //Transform roadPointTransform = busRoutes[i].GetComponent<TrackCheckpoints>().GetSpawnLocationTransform().GetComponent<SpawnLocationIndividual>().GetRoadPointTransform();
                    //Transform newBus = Instantiate(busPrefab[i], roadPointTransform.position, roadPointTransform.rotation, busParent);
                    currentRouteIndex = i;
                    Transform newBus = _driverPool.Get();
                    SetBusAndRoute(newBus, busRoutes[i]);

                    //and update the most recent scheduled time for each route
                    int nextIndex = timerForAllRoutes[i].IndexOf(firstTimerForEachRoute[i]) + 1;
                    if (nextIndex < timerForAllRoutes[i].Count) firstTimerForEachRoute[i] = timerForAllRoutes[i][nextIndex];

                    uiManager.AddBusStatus(i, 1, firstTimerForEachRoute[i]);
                }
            }

            //set timer to 0 if one day has passed
            if (timer > 24 * 60) timer = 0f;
        }
    }

    /// <summary>
    /// send an agent driver to drive on this route
    /// </summary>
    /// <param name="busTransform"></param>
    /// <param name="routeTransform"></param>
    private void SetBusAndRoute(Transform busTransform,Transform routeTransform)
    {
        TrackCheckpoints trackCheckpoints = routeTransform.GetComponent<TrackCheckpoints>();
        trackCheckpoints.AddACar(busTransform);

        busTransform.GetComponent<ContainerForSignals>().StoreSignal(trackCheckpoints,isSendingSignals);
        busTransform.GetChild(0).gameObject.SetActive(true);        //enabled mlagents

        CarDriverAgent agentDriver = busTransform.GetComponentInChildren<CarDriverAgent>();
        agentDriver.SetTrackcheckpointsToAgent(trackCheckpoints);
        agentDriver.SetDriverPool(_driverPool);

        //Debug.Log("CarDriverAgent id " + agentDriver.GetInstanceID());
    }

    /// <summary>
    /// initialize the time schedule for each route,
    /// </summary>
    private void InitializeTimerForAllRoutes()
    {
        //initialize the bus schedule into a timer
        foreach (Transform route in busRoutes)
        {
            //get the schedule in string format for this route
            List<string> processedBusSchedule = route.GetComponent<TrackCheckpoints>().ReadFileToStrings();

            //initialize a list to store scheduler time in integer format
            List<int> timerForOneRoute = new List<int>();

            int amOrPm = 0;
            foreach (var time in processedBusSchedule)
            {
                int index = time.IndexOf(":");
                int hour = Int16.Parse(time.Substring(0, index));
                int minute = Int16.Parse(time.Substring(index + 1));
                if (hour >= amOrPm)
                {
                    amOrPm = hour;
                    timerForOneRoute.Add(hour * 60 + minute);
                }
                else
                {
                    amOrPm = 24;
                    timerForOneRoute.Add(hour * 60 + minute + 12 * 60);
                }
            }            
            timerForAllRoutes.Add(timerForOneRoute);
        }
    }


    public bool isBusInOperation()
    {
        return isSendingSignals;
    }

    public void SetPool(ObjectPool<Transform> pool)
    {
        _driverPool = pool;
    }

    public Transform GetTransformParent()
    {
        return busParent;
    }

    public Transform BusSpawnLocation()
    {
        Transform roadPointTransform = busRoutes[currentRouteIndex].GetComponent<TrackCheckpoints>().GetSpawnLocationTransform().GetComponent<SpawnLocationIndividual>().GetRoadPointTransform();
        return roadPointTransform;
    }

    public int GetRouteIndex(TrackCheckpoints trackCheckpoints)
    {
        Transform currentRoute= trackCheckpoints.transform;
        int routeIndex = 0;
        foreach (var route in busRoutes)
        {
            if (route == currentRoute) routeIndex= busRoutes.IndexOf(route); 
        }
        return routeIndex;
    }
}
