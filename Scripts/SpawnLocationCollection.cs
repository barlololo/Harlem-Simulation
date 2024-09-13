using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnLocationCollection : MonoBehaviour
{
    [Header("Road Intersects")]
    [SerializeField] private Transform mainRoad;
    [SerializeField] private Transform secondaryRoad;
    
    [Header("Buildings By Type")]
    [SerializeField] private Transform[] buildingTypeCollections= new Transform[12];
    
    //private List<Dictionary<Transform,bool>> buildingSpawnLocations;    //bool indicates if the capacity is met
    private int[] buildingSpawnLocationCountByType;
    private List<Transform>[] arrayOfAvailableBuildingLists;    //dynamically store available buildings

    [SerializeField] private DailyActivities dailyActivities;


    /// <summary>
    /// initialize each spawn location
    /// </summary>
    private void Awake()
    {        
        int typeCount= buildingTypeCollections.Length;               
        buildingSpawnLocationCountByType = new int[typeCount];
        bool hasADayBegun = dailyActivities.IsInOperation();
        if (hasADayBegun)   arrayOfAvailableBuildingLists = new List<Transform>[typeCount];

        for (int i=0;i< buildingTypeCollections.Length;i++)
        {
            int childCountByType = buildingTypeCollections[i].childCount;
            buildingSpawnLocationCountByType[i] = childCountByType;

            //used to provide available buildings and initializa building capacity
            if (hasADayBegun)  
            {
                int capacityByType = dailyActivities.GetBuildingCapacity(i);
                Transform parentByType= buildingTypeCollections[i];
                arrayOfAvailableBuildingLists[i] = new List<Transform>();
                for (int j = 0; j < childCountByType; j++)
                {
                    Transform building = parentByType.GetChild(j);
                    building.GetComponent<BuildingStatus>().InitializeBuildingCapacity(capacityByType);
                    arrayOfAvailableBuildingLists[i].Add(building);                       
                }
            }
            //int index = buildings.IndexOf(buildingType);
            //buildingSpawnLocations.Add(new Dictionary<Transform, bool>());
            //int count = buildingType.childCount;
            ////Debug.Log("dic count is " + count + ", index is " + index+", dic is "+ buildingSpawnLocations[index]);
            //for(int i = 0; i < count; i++)
            //{
            //    bool occupancy = false;
            //    buildingSpawnLocations[index].Add(buildingType.GetChild(i), occupancy);
            //    //Debug.Log("index "+index+" "+ i+" "+  buildingType.GetChild(i));
            //}
        }            
    }

    public SpawnLocationIndividual RandomPathwayOnMainRoad()
    {
        return mainRoad.GetChild(Random.Range(0, mainRoad.childCount)).GetComponent<SpawnLocationIndividual>();
    }

    public SpawnLocationIndividual RandomPathwayOnSecondaryRoad()
    {
        return secondaryRoad.GetChild(Random.Range(0, secondaryRoad.childCount)).GetComponent< SpawnLocationIndividual>();
    }

    public BuildingStatus RandomChooseABuilding()
    {
        int firstIndex = Random.Range(0, buildingTypeCollections.Length);
        int secondIndex = Random.Range(0, buildingSpawnLocationCountByType[firstIndex]);
        if (!buildingTypeCollections[firstIndex].GetChild(secondIndex).TryGetComponent<BuildingStatus>(out BuildingStatus buildingStatus)) 
            Debug.LogError("index are " + firstIndex + " and " + secondIndex);
        return buildingStatus;
    }

    /// <summary>
    /// get a list of available building of a specific type and each building has a BuildingStatus
    /// </summary>
    /// <param name="typeIndex"></param>
    /// <returns></returns>
    public List<Transform> ListOfAvailableBuildingsByType(int typeIndex)
    {        
        return arrayOfAvailableBuildingLists[typeIndex];
    }

    public void RemoveOccupiedBuilding(Transform building, int typeIndex)
    {
        arrayOfAvailableBuildingLists[typeIndex].Remove(building);
    }

    //public Dictionary<Transform, bool> GetBuildingListDictionaryByType(int index)
    //{
    //    return buildingSpawnLocations[index];
    //}
}
