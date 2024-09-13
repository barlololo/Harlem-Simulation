using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;
using TextAsset = UnityEngine.TextAsset;

public class DailyActivities : MonoBehaviour
{
    [Header("Raw Data Files")]
    [SerializeField] private TextAsset dataForDependency;
    [SerializeField] private TextAsset dataForAge;
    [SerializeField] private TextAsset dataForOccupancy;
    [SerializeField] private TextAsset dataForRace;
    [SerializeField] private TextAsset dataForBuildingCapacity;
    [SerializeField] private TextAsset dataForTimeDuration;
    [SerializeField] private TextAsset dataForResidentsAndPassersby;
    private List<int> populationTotalAndByRace;
    private List<int> eachBuildingCapacity;
    private List<int> timeDurationAllday;
    private float ageDependencyPercent;     //percent of people who are not life-independent

    [Header("Characters And Locations")]
    //[SerializeField] private List<GameObject> characterPrefab;
    [SerializeField] private SpawnLocationCollection spawnLocationCollection;

    [SerializeField] private bool hasADayBegun = false;         //used for Heuristic mode and freeplay
    [SerializeField] public Transform residentParent;
    [SerializeField] public Transform nonResidentParent;
    private Dictionary<string, Transform> residentsOutOfTown;

    //statistics info: for 13 entries
    private int[] residentCount;            
    private int[] residentCountActive;
    private int[] nonResidentCount;
    private int[] nonResidentCountActive;
    [SerializeField] private UIManager uiManager;

    enum Race
    {
        Hispanic = 0,
        White=1,
        Black = 2,
        Asian = 3,
        OtherMinors=4,
        MoreThanOne=5
    }

    private ObjectPool<Transform>[] _characterPoolArray;       

    private void Awake()
    {
        if (hasADayBegun)    //initialize all .txt data into properties
        {
            Dictionary<string, int> populationDic = ReadFileToData(dataForRace);
            populationTotalAndByRace = new List<int>();
            foreach (KeyValuePair<string, int> entry in populationDic) populationTotalAndByRace.Add(entry.Value);

            Dictionary<string, int> capacityDic = ReadFileToData(dataForBuildingCapacity);
            eachBuildingCapacity = new List<int>();
            foreach (KeyValuePair<string, int> entry in capacityDic) eachBuildingCapacity.Add(entry.Value);

            Dictionary<string, int> dependencyDic = ReadFileToData(dataForDependency);
            ageDependencyPercent = (float)dependencyDic.ElementAt(0).Value / 100;

            Dictionary<string, int> durationDic = ReadFileToData(dataForTimeDuration);
            timeDurationAllday = new List<int>();
            foreach (KeyValuePair<string, int> entry in durationDic) timeDurationAllday.Add(entry.Value);

            residentsOutOfTown = new Dictionary<string, Transform>();

            residentCount = new int[13];
            nonResidentCount = new int[13];
            residentCountActive = new int[13];
            nonResidentCountActive = new int[13];

            _characterPoolArray = new ObjectPool<Transform>[6];

            //send parameters to each pool generator
            GetComponent<CharacterPool_Asian>().SetMaxSizeAndRace((int)(populationTotalAndByRace[1]/200) +10,0);
            GetComponent<CharacterPool_Black>().SetMaxSizeAndRace((int)(populationTotalAndByRace[2] / 200) + 10,1);
            GetComponent<CharacterPool_Hispanic>().SetMaxSizeAndRace((int)(populationTotalAndByRace[3] / 200) + 10,2);
            GetComponent<CharacterPool_MoreThanOne>().SetMaxSizeAndRace((int)(populationTotalAndByRace[4] / 200) + 10,3);
            GetComponent<CharacterPool_OtherMinors>().SetMaxSizeAndRace((int)(populationTotalAndByRace[5] / 200) + 10,4);
            GetComponent<CharacterPool_White>().SetMaxSizeAndRace((int)(populationTotalAndByRace[6] / 200) + 10,5);    
        }
    }

    private void Start()
    {
        if (hasADayBegun)
        {
            InitializeCharactersForAllLocations();
            InvokeRepeating(nameof(SendPasserbyToNeighborhood), timeDurationAllday[0], timeDurationAllday[0]);
        }
    }


    /// <summary>
    /// initialize the status for each location,
    /// and each building could begin the day based on the initial data
    /// </summary>
    private void InitializeCharactersForAllLocations()
    {
        Dictionary<string, int> quantitiesDic = ReadFileToData(dataForResidentsAndPassersby);
        float quantitiesControlRatio = quantitiesDic.Values.First() / 100f;

        //Part I    initialize residents        
        int hispanicActivePopulation = (int)(populationTotalAndByRace[1] * quantitiesControlRatio);
        int WhiteActivePopulation = (int)(populationTotalAndByRace[2] * quantitiesControlRatio);
        int BlackActivePopulation = (int)(populationTotalAndByRace[3] * quantitiesControlRatio);
        int AsianActivePopulation = (int)(populationTotalAndByRace[4] * quantitiesControlRatio);
        int OtherMinorsActivePopulation = (int)(populationTotalAndByRace[5] * quantitiesControlRatio);
        int MoreThanOneActivePopulation = (int)(populationTotalAndByRace[6] * quantitiesControlRatio);

        int race = (int)Race.Hispanic;
        for (int i = 0; i < hispanicActivePopulation; i++) SendResidentToBuilding(race);
        race = (int)Race.White;
        for (int i = 0; i < WhiteActivePopulation; i++) SendResidentToBuilding(race);
        race = (int)Race.Black;
        for (int i = 0; i < BlackActivePopulation; i++) SendResidentToBuilding(race);
        race = (int)Race.Asian;
        for (int i = 0; i < AsianActivePopulation; i++) SendResidentToBuilding(race);
        race = (int)Race.OtherMinors;
        for (int i = 0; i < OtherMinorsActivePopulation; i++) SendResidentToBuilding(race);
        race = (int)Race.MoreThanOne;
        for (int i = 0; i < MoreThanOneActivePopulation; i++) SendResidentToBuilding(race);

        //update statistics
        uiManager.InitializeResidentCount(0, residentCount[0]);
        uiManager.InitializeResidentCount(7, residentCount[7]);
        uiManager.InitializeResidentCount(6, residentCount[6]);
        uiManager.InitializeResidentCount(5, residentCount[5]);
        uiManager.UpdateResidentActiveCount(0, residentCountActive[0]);
        uiManager.UpdateResidentActiveCount(7, residentCountActive[7]);
        uiManager.UpdateResidentActiveCount(6, residentCountActive[6]);
        uiManager.UpdateResidentActiveCount(5, residentCountActive[5]);


        //Part II   initialize passers-by        
        int initialPasserbyCount = 20;   //initial number of people hanging around in the area
        for (int i = 0; i < initialPasserbyCount; i++) SendPasserbyToNeighborhood();

        for (int i = 0; i < 12; i++)
        {
            uiManager.UpdateNonResidentCount(0, nonResidentCount[i], nonResidentCountActive[i]);
        }
    }


    /// <summary>
    /// choose a random available building of type xxxx,
    /// return the parent transform with BuildingStatus component
    /// </summary>
    /// <param name="availableBuildingsOfType"></param>
    /// <returns></returns>
    private Transform RandomAvailableBuildingForResident(List<Transform> availableBuildingsOfType)
    {
        int choosenIndex = Random.Range(0, availableBuildingsOfType.Count);
        Transform choosenBuilding = availableBuildingsOfType[choosenIndex];

        if (choosenBuilding.GetComponent<BuildingStatus>().IsResideInAllowed())     return choosenBuilding;
        else     //this building is occupied full
        {
            availableBuildingsOfType.Remove(choosenBuilding);   //remove from the available list
            
            //find next available building and break loop
            foreach (Transform building in availableBuildingsOfType)
            {
                if (building.GetComponent<BuildingStatus>().IsResideInAllowed())
                    choosenBuilding= building; break;
            }

            if (choosenBuilding == null)    //send to hospital
            {
                availableBuildingsOfType = spawnLocationCollection.ListOfAvailableBuildingsByType(5);

                foreach (Transform  entry in availableBuildingsOfType)
                {
                    if (entry.GetComponent<BuildingStatus>().IsResideInAllowed())
                        choosenBuilding = entry; break;
                }
                if (choosenBuilding== null) Debug.LogError("hospital is full");
                return choosenBuilding;
            }
            else return choosenBuilding;
        }
    }

    /// <summary>
    /// initialize each resident to either apartment, mixed or house
    /// </summary>
    /// <param name="characterPrefab"></param>
    private void SendResidentToBuilding(int race)
    {
        int whichType = Random.Range(0, 3);         int typeIndex;        
        if (whichType == 0)         typeIndex = 0;     //apartment
        else if (whichType == 1)    typeIndex = 7;     //mixed
        else                        typeIndex = 6;     //house

        BuildingStatus choosenBuildingStatus = RandomAvailableBuildingForResident(spawnLocationCollection.ListOfAvailableBuildingsByType(typeIndex)).GetComponent<BuildingStatus>();
        string personalID = choosenBuildingStatus.GetInstanceID().ToString() + choosenBuildingStatus.CountOnCurrentResidents() + race;
        
        int type= choosenBuildingStatus.GetBuildingTypeIndex();
        residentCount[type] += 1;

        bool isAtHome = (Random.Range(0f, 1.0f) < ageDependencyPercent * 2);
        if (!isAtHome)   //on the way back home, instantiated
        {
            bool fromMainRoad = Random.Range(0, 2) == 0;
            Transform spawnLocation;
            if (fromMainRoad) spawnLocation = spawnLocationCollection.RandomPathwayOnSecondaryRoad().GetPathwayPointTransform();
            else spawnLocation = spawnLocationCollection.RandomPathwayOnMainRoad().GetPathwayPointTransform();

            Transform resident = _characterPoolArray[race].Get();
            CharacterAgent characterAgent = resident.GetComponentInChildren<CharacterAgent>();
            characterAgent.RegisterOrUpdateResidentInfo(spawnLocation.position, choosenBuildingStatus.GetBuildingTransform());
            characterAgent.SetID(personalID);
            characterAgent.TrySetCharacterPool(_characterPoolArray[race]);

            residentCountActive[type] += 1;
        }
        else    choosenBuildingStatus.InitializeInvokeMethod();     //at home, could be activated soon

        choosenBuildingStatus.InitializeReference(spawnLocationCollection, uiManager);
        choosenBuildingStatus.RegisterOrUpdateResident(personalID, isAtHome);
        choosenBuildingStatus.TryAddPool(_characterPoolArray[race], race);

        personalID.Remove(0);
        //Debug.Log(choosenBuildingStatus.GetInstanceID().ToString());
        //Debug.Log(personalID);
    }

    /// <summary>
    /// initialize passers-by in one spawn location of the area,
    /// head to another spawn location and leave the area
    /// </summary>
    /// <param name="passerbyPrefab"></param>
    /// <param name="destination"></param>
    private void PasserbyWalkThrough(Transform spawnLocation, int race)
    {    
        bool toMainRoad = Random.Range(0, 2) == 0;      Transform endLocation;
        if (toMainRoad) endLocation = spawnLocationCollection.RandomPathwayOnMainRoad().GetPathwayPointTransform();
        else endLocation = spawnLocationCollection.RandomPathwayOnSecondaryRoad().GetPathwayPointTransform();

        Transform passerby = _characterPoolArray[race].Get();

        CharacterAgent passerbyAgent =passerby.GetComponentInChildren<CharacterAgent>();
        passerbyAgent.RegisterOrUpdateNonresidentInfo(spawnLocation.position, endLocation);
        passerbyAgent.TrySetCharacterPool(_characterPoolArray[race]);
        //passerbyPrefab.transform.GetChild(0).gameObject.SetActive(true);      //enable mlagents

        nonResidentCount[nonResidentCount.Length - 1] += 1;
        nonResidentCountActive[nonResidentCountActive.Length - 1] += 1;
    }

    /// <summary>
    /// initialize passer-by in one spawn location of the area,
    /// visit one random building in the area.
    /// if BuildingStatus does not allow visiting, 
    /// then leave the area
    /// </summary>
    /// <param name="passerbyPrefab"></param>
    private void PasserbyVisitBuilding(Transform spawnLocation, int race)
    {
        BuildingStatus visitingBuildingStatus = spawnLocationCollection.RandomChooseABuilding();
        if (visitingBuildingStatus.IsVisitingAllowed())     //visit a random building
        {
            string personalID = visitingBuildingStatus.GetInstanceID().ToString() + visitingBuildingStatus.CountOnCurrentVisitors() + race;
            bool hasArrived = false;

            Transform passerby = _characterPoolArray[race].Get();

            CharacterAgent passerbyAgent = passerby.GetComponentInChildren<CharacterAgent>();
            passerbyAgent.RegisterOrUpdateNonresidentInfo(spawnLocation.position, visitingBuildingStatus.GetBuildingTransform());  //load info into CharacterAgent
            passerbyAgent.SetID(personalID);
            passerbyAgent.TrySetCharacterPool(_characterPoolArray[race]);

            visitingBuildingStatus.InitializeReference(spawnLocationCollection, uiManager);
            visitingBuildingStatus.RegisterOrUpdateVisitor(personalID, hasArrived, passerbyAgent);
            visitingBuildingStatus.TryAddPool(_characterPoolArray[race], race);

            int type = visitingBuildingStatus.GetBuildingTypeIndex();
            nonResidentCount[type] += 1;
            nonResidentCountActive[type] += 1;
        }
        else PasserbyWalkThrough(spawnLocation,race);    //visiting not allowed and leave the area        

        //passerbyPrefab.transform.GetChild(0).gameObject.SetActive(true);      //enable mlagents
    }


    /// <summary>
    /// send passer-by to the area,
    /// either to plan to visit a building or to walk through the area
    /// </summary>
    private void SendPasserbyToNeighborhood()
    {
        bool fromMainRoad = Random.Range(0, 2) == 0;    Transform startLocation;  //spawn location for the passer-by
        if (fromMainRoad) startLocation = spawnLocationCollection.RandomPathwayOnMainRoad().GetPathwayPointTransform();
        else startLocation = spawnLocationCollection.RandomPathwayOnSecondaryRoad().GetPathwayPointTransform();

        int race = Random.Range(0, Enum.GetValues(typeof(Race)).Length);

        bool isVisiting = Random.Range(0, 2) == 0;
        if (isVisiting) PasserbyVisitBuilding(startLocation, race);    //plan to visit a building in the area
        else PasserbyWalkThrough(startLocation, race);                 //pass by the area    
    }


    public void UpdateResidentsOutsideNeighborhood(string residentID,Transform buildingResideIn)
    {
        residentsOutOfTown.Add(residentID,buildingResideIn);
        int type= buildingResideIn.parent.GetComponent<BuildingStatus>().GetBuildingTypeIndex();
        uiManager.UpdateResidentActiveCount(type, -1);
    }

    /// <summary>
    /// get census and building data as a real world input for the simulation,
    /// return a dictionary with item-number pairs
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private Dictionary<string, int> ReadFileToData(TextAsset file)
    {
        Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();

        var splitFile = new string[] { "\r\n", "\r", "\n" };
        var Lines = file.text.Split(splitFile, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < Lines.Length; i++)
        {
            string line =Lines[i];
            int lastLetterIndex = line.LastIndexOf(line.LastOrDefault(char.IsLetter));
            if(lastLetterIndex <= line.Length-1)
            {
                string key = line.Substring(0, lastLetterIndex + 1);
                string substr = line.Substring(lastLetterIndex + 1);
                var leftWords = substr.Trim().Split(" ");
                int value = (int)float.Parse(leftWords[0]);
                keyValuePairs.Add(key, value);
            }
        }
        return keyValuePairs;
    }

    public bool IsInOperation()
    {
        return hasADayBegun;
    }

    public int GetBuildingCapacity(int typeIndex)
    {
        return eachBuildingCapacity[typeIndex];
    }
    
    public Transform GetTransformParent(bool isResident)
    {
        if (isResident) return residentParent;
        else return nonResidentParent;
    }

    
    /// <summary>
    /// total of 5 periods for updates
    /// </summary>
    /// <param name="period"></param>
    public void UpdateInvokingMethod(int period)
    {
        if(period<timeDurationAllday.Count)
        {
            CancelInvoke();
            InvokeRepeating(nameof(SendPasserbyToNeighborhood), timeDurationAllday[period], timeDurationAllday[period]);
        }
    }

    public void TryAddPool(ObjectPool<Transform> pool, int race)
    {
        if (_characterPoolArray.Contains(pool)) return;
        else _characterPoolArray[race] = pool;
    }
}
