using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

public class BuildingStatus : MonoBehaviour
{
    private int maximumOccupancy=20;
    private int maximumVisiting;

    private Dictionary<string,bool> visitorStatus;
    private Dictionary<string, bool> residentStatus;
    private Dictionary<string ,Transform> visitorOfResidentStatus;

    private SpawnLocationCollection spawnLocationCollection;
    private bool isOccupiedByCharacter=false;

    //statistics info: for 13 entries
    private int buildingTypeIndex;
    private UIManager uiManager;


    private ObjectPool<Transform>[] _characterPoolArray;



    private void Awake()
    {
        visitorStatus = new Dictionary<string, bool>();
        residentStatus  = new Dictionary<string, bool>();
        visitorOfResidentStatus= new Dictionary<string ,Transform>();

        maximumVisiting = maximumOccupancy;       //assuming these two are the same        

        _characterPoolArray = new ObjectPool<Transform>[6];

        buildingTypeIndex = FindBuildingTypeIndex();
    }

    private void ResidentCommute(string residentID)
    {
        residentStatus[residentID] = false;
        int race = residentID[residentID.Length - 1] - '0';

        BuildingStatus targetBuildingStatus = spawnLocationCollection.RandomChooseABuilding();
        if (targetBuildingStatus.IsVisitingAllowed()&& targetBuildingStatus!=this)    //depart to visit another building
        {
            Transform resident = _characterPoolArray[race].Get();
            CharacterAgent residentAgent = resident.GetComponentInChildren<CharacterAgent>();
            residentAgent.RegisterOrUpdateResidentInfo(GetEntrancePosition(),targetBuildingStatus.GetBuildingTransform());
            residentAgent.SetID(residentID);
            residentAgent.TrySetCharacterPool(_characterPoolArray[race]);

            targetBuildingStatus.InitializeReference(spawnLocationCollection, uiManager);
            targetBuildingStatus.RegisterOrUpdateResidentVisitor(residentID, false);
            targetBuildingStatus.UpdateVisitorHomeInfo(residentID,GetBuildingTransform());
            targetBuildingStatus.TryAddPool(_characterPoolArray[race], race);

            CheckIfBuildingEmpty();
        }
        else  //resident leave the town 
        {            
            bool toMainRoad = Random.Range(0, 2) == 0;      Transform headTo;   //spawn location for the passer-by
            if (toMainRoad) headTo = spawnLocationCollection.RandomPathwayOnMainRoad().GetPathwayPointTransform();
            else headTo = spawnLocationCollection.RandomPathwayOnSecondaryRoad().GetPathwayPointTransform();

            Transform resident = _characterPoolArray[race].Get();
            CharacterAgent characterAgent = resident.GetComponentInChildren<CharacterAgent>();
            characterAgent.RegisterOrUpdateResidentInfo(GetEntrancePosition(), headTo);
            characterAgent.SetID(residentID);
            characterAgent.TrySetCharacterPool(_characterPoolArray[race]);

            CheckIfBuildingEmpty();
        }

        uiManager.UpdateResidentActiveCount(buildingTypeIndex, 1);
    }

    private void VisitorCommute(string visitorID)
    {
        visitorStatus.Remove(visitorID);
        int race = visitorID[visitorID.Length - 1] - '0';

        if (visitorOfResidentStatus.ContainsKey(visitorID)) //resident of another building, heading back home
        {
            Transform buildingLiveIn = visitorOfResidentStatus[visitorID];
            visitorOfResidentStatus.Remove(visitorID);

            Transform residentVisitor = _characterPoolArray[race].Get();
            CharacterAgent characterAgent = residentVisitor.GetComponentInChildren<CharacterAgent>();
            characterAgent.RegisterOrUpdateResidentInfo(GetEntrancePosition(), buildingLiveIn);
            characterAgent.SetID(visitorID);
            characterAgent.TrySetCharacterPool(_characterPoolArray[race]);

            uiManager.UpdateResidentActiveCount(buildingTypeIndex, 1);
            CheckIfBuildingEmpty();
        }
        else  //visitor to this building, leave the town
        {                    
            bool toMainRoad = Random.Range(0, 2) == 0;      Transform headTo;
            if (toMainRoad) headTo = spawnLocationCollection.RandomPathwayOnMainRoad().GetPathwayPointTransform();
            else headTo = spawnLocationCollection.RandomPathwayOnSecondaryRoad().GetPathwayPointTransform();

            visitorStatus.Remove(visitorID);
            Transform visitor = _characterPoolArray[race].Get();
            CharacterAgent visitorAgent = visitor.GetComponentInChildren<CharacterAgent>();
            visitorAgent.RegisterOrUpdateNonresidentInfo(GetEntrancePosition(), headTo);
            visitorAgent.SetID(visitorID);
            visitorAgent.TrySetCharacterPool(_characterPoolArray[race]);

            uiManager.UpdateNonResidentCount(buildingTypeIndex, 0 ,1);
            CheckIfBuildingEmpty();
        }
    }

    private void ActivateResidentOrVisitor()
    {
        int whichType = Random.Range(0, 2);
        if (whichType == 0 && residentStatus != null)
        {
            float StayAtHomeRatio = 0.8f;
            if (Random.Range(1, 11) / 10 < StayAtHomeRatio) return;     //keep staying at home
            else  //the resident is taking an action
            {
                foreach (KeyValuePair<string, bool> entry in residentStatus)
                {
                    if (entry.Value) ResidentCommute(entry.Key); break;
                }
            }
        }            
        if (whichType == 1 && visitorStatus != null)
        {
            foreach (KeyValuePair<string, bool> entry in visitorStatus)
            {
                if (entry.Value) VisitorCommute(entry.Key); break;
            }
        }
    }

    /// <summary>
    /// register or update a resident to BuildingStatus,
    /// the resident now is either at home or heading back home
    /// </summary>
    /// <param name="resident"></param>
    /// <param name="isAtHome"></param>
    /// <param name="infoOnAllLocations"></param>
    //public void RegisterOrUpdateResident(GameObject resident,bool isAtHome)
    //{        
    //    if(currentResidentsStatus.ContainsKey(resident)) //UPDATE
    //    {
    //        currentResidentsStatus[resident] = isAtHome;
    //        if (isAtHome) resident.SetActive(false); //hide the resident inside the building
    //        else Debug.LogError("resident" + resident + " should be at home");
    //    }
    //    else //REGISTER
    //    {
    //        currentResidentsStatus.Add(resident, isAtHome);
    //        if (isAtHome) resident.SetActive(false); //hide the resident inside the building
    //        else
    //        {
    //            //the resident is on the way back home
    //            bool fromMainRoad = Random.Range(0, 2) == 0;
    //            Transform spawnLocation;
    //            if (fromMainRoad) spawnLocation = spawnLocationCollection.RandomPathwayOnSecondaryRoad().GetPathwayPointTransform();
    //            else spawnLocation = spawnLocationCollection.RandomPathwayOnMainRoad().GetPathwayPointTransform();

    //            Transform agent=resident.transform.GetChild(0);
    //            agent.forward = spawnLocation.forward;
    //            agent.GetComponent<CharacterAgent>().SetPlaceSpawned(spawnLocation);
    //        }
    //    }
    //}


    public void RegisterOrUpdateResident(string id,bool isAtHome)
    {
        if (residentStatus.ContainsKey(id))   //UPDATE
        {
            residentStatus[id] = isAtHome;
            //Debug.Log("BUILDING: a resident just got home");
            uiManager.UpdateResidentActiveCount(buildingTypeIndex, -1);
        }
        else    //REGISTER
        {
            residentStatus.Add(id, isAtHome);
            //Debug.Log("BUILDING： register a resident");
            if (residentStatus.Count == maximumOccupancy) spawnLocationCollection.RemoveOccupiedBuilding(transform, buildingTypeIndex);
        }
    }

    /// <summary>
    /// this is for non-resident visitor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="hasArrived"></param>
    /// <param name="visitor"></param>
    public void RegisterOrUpdateVisitor(string id, bool hasArrived, CharacterAgent visitor)
    {
        if (visitorStatus.ContainsKey(id)) 
        {
            if(hasArrived)
            {
                visitorStatus[id] = hasArrived;  //UPDATE
                //Debug.Log("BUILDING： a non-resident visitor just arrived");
                uiManager.UpdateNonResidentCount(buildingTypeIndex, 0,-1);
            }                
            else    //REGISTER
            {
                while (visitorStatus.ContainsKey(id)) id=1+id;     
                Debug.Log(id+" is updated");
                visitor.SetID(id);       //update a new available id for visitor
                visitorStatus.Add(id, hasArrived);
            }
        }
        else    //REGISTER
        {
            visitorStatus.Add(id, hasArrived);
            //Debug.Log("BUILDING： register a non-resident visitor");
        }
    }

    /// <summary>
    ///  this is for resident visitor
    /// </summary>
    /// <param name="id"></param>
    /// <param name="hasArrived"></param>
    public void RegisterOrUpdateResidentVisitor(string id, bool hasArrived)
    {
        if (visitorStatus.ContainsKey(id))
        {
            visitorStatus[id] = hasArrived;  //UPDATE
            //Debug.Log("BUILDING： a resident visitor just arrived");
            if(uiManager!=null)
            {
                uiManager.UpdateResidentActiveCount(buildingTypeIndex, -1);
            }
            
        }
        else    //REGISTER
        {
            visitorStatus.Add(id, hasArrived);
            //Debug.Log("BUILDING： register a resident visitor");
        }
    }

    public void UpdateVisitorHomeInfo(string id, Transform resideIn)
    {
        visitorOfResidentStatus.Add(id,resideIn);
    }
    public bool IsVisitingAllowed()
    {
        return visitorStatus.Count < maximumVisiting;
    }

    public bool IsResideInAllowed()
    {
        return residentStatus.Count < maximumOccupancy;
    }

    public Vector3 GetEntrancePosition()
    {
        int entranceCount = transform.childCount-1;
        Transform entrancePoint;
        if (entranceCount > 1)  entrancePoint= transform.GetChild(Random.Range(0, entranceCount));
        else                    entrancePoint= transform.GetChild(0);

        if (residentStatus == null && visitorStatus == null)
        {
            Debug.Log("entrance  is " + transform.name + " and " + entrancePoint.name + " position is " + entrancePoint.GetComponent<MeshFilter>().sharedMesh.bounds.center
           + " or " + entrancePoint.GetComponent<MeshFilter>().mesh.bounds.center);
        }

        return entrancePoint.GetComponent<MeshFilter>().sharedMesh.bounds.center + new Vector3(0, -0.5f, 0);
        //return entrancePoint.GetComponent<MeshFilter>().mesh.bounds.center;
    }

    public Transform GetBuildingTransform()
    {
        return transform.GetChild(transform.childCount-1);
    }

    public void InitializeBuildingCapacity(int capacity)
    {
        maximumOccupancy = capacity;
    }


    public void TryAddPool(ObjectPool<Transform> pool, int race)
    {
        if(_characterPoolArray.Contains(pool)) return;
        else _characterPoolArray[race] = pool; 
    }

    public int CountOnCurrentResidents()
    {
        return residentStatus.Count;
    }

    public int CountOnCurrentVisitors()
    {
        return visitorStatus.Count;
    }

    /// <summary>
    /// initialize a BuildingStatus to invoke ActivateResidentOrVisitor method if it's not empty
    /// </summary>
    public void InitializeReference(SpawnLocationCollection spawnLocationCollection, UIManager uiManager)
    {
        this.spawnLocationCollection = spawnLocationCollection;
        this.uiManager = uiManager;
    }

    public void InitializeInvokeMethod()
    {
        if (!isOccupiedByCharacter && (residentStatus != null || visitorStatus != null))
        {
            InvokeRepeating(nameof(ActivateResidentOrVisitor), 5, 5);
            isOccupiedByCharacter = true;
        }
    }

    private int FindBuildingTypeIndex()
    {
        string name = transform.name.Split(" ")[0];
        
        if (name.Equals("apartment")) return 0;
        else if (name.Equals("church")) return 1;
        else if (name.Equals("commercial")) return 2;
        else if (name.Equals("community")) return 3;
        else if (name.Equals("firehouse")) return 4;
        else if (name.Equals("hospital")) return 5;
        else if (name.Equals("house")) return 6;
        else if (name.Equals("mixed")) return 7;
        else if (name.Equals("parking")) return 8;
        else if (name.Equals("police")) return 9;
        else if (name.Equals("postal")) return 10;
        else if (name.Equals("school")) return 11;
        else return -1;
    }

    public int GetBuildingTypeIndex()
    {
        return buildingTypeIndex;
    }

    private void CheckIfBuildingEmpty()
    {
        if(!residentStatus.ContainsValue(true) && !visitorStatus.ContainsValue(true))
        {
            isOccupiedByCharacter = false;
            CancelInvoke();
        }            
    }
}
