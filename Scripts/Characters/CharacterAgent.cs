using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using UnityEngine.UIElements;
using UnityEngine.Pool;
using UnityEngine.AI;

public class CharacterAgent : Agent
{
    //basic working settings    
    [SerializeField] private Character playerController;
    [SerializeField] private GameInput gameInput;
    private Vector3 bornWorldPos;
    private Vector3 targetWorldPos;
    private bool isDailyActivitiesInOperation = false;

    //Personal Identity
    private bool isResident = false;
    private Transform buildingResideIn;         //building without BuildingStatus
    private VisualElement rootUI;
    private string personalID;

    [Header("Target and Marker")]
    [SerializeField] private Transform targetTransform;     //not the parent with BuildingStatus
    [SerializeField] private Material targetMaterial;
    [SerializeField] private Material markerMaterial;
    [SerializeField] private Transform markerTransform;
    private Material tempTargetMaterialBackup;
    private Material tempMarkerMaterialBackup;

    //for measuring rewards 
    //private Collider previousBuildingCollider;
    private float previousDistance;

    //object pooling
    private ObjectPool<Transform> _characterPool;

    //Method I: NavMesh Agent
    private NavMeshAgent navMeshAgent;

    //Method II: A* Pathfinding
    private Pathfinding pathFinding;
    private GridSystem girdSystem;
    [SerializeField] private Transform nextGoal;


    public override void Initialize()
    {        
        rootUI = GetComponentInChildren<UIDocument>().rootVisualElement;    //ui display content 

        tempMarkerMaterialBackup = markerTransform.GetComponent<MeshRenderer>().material;   //document the default material

        //Method II: A* Pathfinding
        girdSystem = FindFirstObjectByType<GridSystem>();
        pathFinding = GetComponent<Pathfinding>();
        pathFinding.SetGrid(girdSystem);
    }

    public override void OnEpisodeBegin()
    {
        markerTransform.GetComponent<Renderer>(). material= markerMaterial;
        markerTransform.gameObject.layer = LayerMask.NameToLayer("Marker");

        //Method I: NavMesh Agent
        //Vector3 navMeshTarget = new Vector3();

        if (isDailyActivitiesInOperation)   //in simulation mode
        {
            transform.position = bornWorldPos;

            //mark target if it's a building
            if (targetTransform.parent.GetComponent<BuildingStatus>() != null)  //target is a building
            {
                MeshRenderer targetMeshRenderer = targetTransform.GetComponent<MeshRenderer>();
                tempTargetMaterialBackup = targetMeshRenderer.material;
                targetMeshRenderer.material = targetMaterial;
                targetTransform.gameObject.layer = LayerMask.NameToLayer("TargetUnit");
                targetTransform.tag = "TargetUnit";

                targetWorldPos = targetTransform.GetComponent<MeshFilter>().sharedMesh.bounds.center;

                //Method I: NavMesh Agent
                //navMeshTarget = targetTransform.parent.GetComponent<BuildingStatus>().GetEntrancePosition();

                //Method II: A* Pathfinding
                Vector3 entrancePos = targetTransform.parent.GetComponent<BuildingStatus>().GetEntrancePosition();
                pathFinding.FindPath(girdSystem.WorldCoordToGridCoord(transform.position), girdSystem.WorldCoordToGridCoord(entrancePos));
                if (pathFinding.closestNode == null)
                {
                    if (pathFinding.path[0] == null) Debug.Log("OnEpisode: isDailyActivitiesInOperation index 0 is null" + transform.parent.GetInstanceID());
                    else Debug.Log("OnEpisode: isDailyActivitiesInOperation closetNode is null " + transform.parent.name);
                }
                nextGoal.position = girdSystem.WorldPointFromNode(pathFinding.closestNode);
            }
            else               //otherwise, it's spawn location
            {
                targetWorldPos = targetTransform.position;

                //Method I: NavMesh Agent
                //navMeshTarget = targetWorldPos;

                //Method II: A* Pathfinding
                pathFinding.FindPath(girdSystem.WorldCoordToGridCoord(transform.position), girdSystem.WorldCoordToGridCoord(targetWorldPos));
                nextGoal.position = girdSystem.WorldPointFromNode(pathFinding.closestNode);
            }

        }
        else    //in training mode or freeplay
        {
            SpawnLocationCollection spawnManager = FindFirstObjectByType<SpawnLocationCollection>();

            //spawn from some unit
            transform.position = spawnManager.RandomChooseABuilding().GetEntrancePosition();
            ////spawn from pathway
            //transform.position = spawnManager.RandomPathwayOnMainRoad().GetPathwayPointTransform().position;

            //set the target
            targetTransform = spawnManager.RandomChooseABuilding().GetBuildingTransform();

            MeshRenderer targetMeshRenderer = targetTransform.GetComponent<MeshRenderer>();
            tempTargetMaterialBackup = targetMeshRenderer.material;
            targetMeshRenderer.material = targetMaterial;
            targetTransform.gameObject.layer = LayerMask.NameToLayer("TargetUnit");
            targetTransform.tag = "TargetUnit";

            targetWorldPos = targetTransform.GetComponent<MeshFilter>().sharedMesh.bounds.center;


            //Method II: A* Pathfinding
            Vector3 targetPos = targetTransform.parent.GetComponent<BuildingStatus>().GetEntrancePosition();
            pathFinding.FindPath(girdSystem.WorldCoordToGridCoord(transform.position), girdSystem.WorldCoordToGridCoord(targetPos));
            if (pathFinding.closestNode == null)
            {
                if (pathFinding.path[0] == null) Debug.Log("OnEpisode: index 0 is null" + transform.parent.GetInstanceID());
                else Debug.Log("OnEpisode: closetNode is null " + transform.parent.name);
            }
            nextGoal.position = girdSystem.WorldPointFromNode(pathFinding.closestNode);
        }

        //Method I: NavMesh Agent
        //if (navMeshAgent != null)
        //{
        //    if (transform.position.y < 15)
        //    {
        //        float vertical = 15.2f - transform.position.y;
        //        transform.position += new Vector3(0, vertical, 0);
        //    }
        //    navMeshAgent.enabled = true;
        //    if (!navMeshAgent.isOnNavMesh) transform.position = bornWorldPos;        //if it's not on mesh
        //    navMeshAgent.destination = targetWorldPos;
        //}
        //else if (navMeshAgent == null && NavMesh.SamplePosition(transform.position, out NavMeshHit closestHit, 25, 1))
        //{
        //    if (transform.position.y < 15)
        //    {
        //        float vertical = 15.2f - transform.position.y;
        //        transform.position += new Vector3(0, vertical, 0);
        //    }
        //    navMeshAgent = transform.gameObject.AddComponent<NavMeshAgent>();
        //    if (!navMeshAgent.isOnNavMesh) transform.position = bornWorldPos;       //if it's not on mesh
        //    navMeshAgent.speed = 7f;
        //    navMeshAgent.agentTypeID = 0;
        //    navMeshAgent.destination = targetWorldPos;
        //}
        //else Debug.LogError("NavMeshAgent: not found");
        //previousDistance = Vector3.SqrMagnitude(targetWorldPos - transform.position);
        //InvokeRepeating(nameof(IsGettingCloser), 10f, 10f);



        UpdateCharacterInfo();        
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Method I: NavMesh Agent
        sensor.AddObservation(targetWorldPos);
        sensor.AddObservation(transform.position);

        //Method II: A* Pathfinding        
        //sensor.AddObservation(nextGoal.position);      //next goal location
        //sensor.AddObservation(transform.position);
        //Debug.Log(Vector3.Distance(transform.position, nextGoal.position)-2.5f);
        //sensor.AddObservation(Vector3.Distance(transform.position, nextGoal.position) - 2.5f);

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (StepCount > MaxStep - 2)
        {
            Debug.Log("FAIL: run out of steps");
            AddReward(-1f);
            ResetMaterialAndTag();
            EndEpisode();
            //if (isDailyActivitiesInOperation) RemoveFromActive();
            //else EndEpisode();
        }

        float moveHorizontal = 0;
        float moveForward = 0;
        
        switch (actions.DiscreteActions[0])
        {
            case 0: moveHorizontal = 0f; break;
            case 1: moveHorizontal = 1f; break;
            case 2: moveHorizontal = -1f; break;
        }
        
        switch (actions.DiscreteActions[1])
        {
            case 0: moveForward = 0f; break;
            case 1: moveForward = 1f; break;
            case 2: moveForward = -1f; break;
        }
 
        playerController.SetWalkInputNormalized(new Vector2(moveHorizontal, moveForward));  
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        Vector2 input = gameInput.GetWalkVector();
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        if (input.x > 0) discreteActions[0] = 1;
        else if (input.x < 0) discreteActions[0] = 2;
        else discreteActions[0] = 0;

        if (input.y > 0) discreteActions[1] = 1;
        else if (input.y < 0) discreteActions[1] = 2;
        else discreteActions[1] = 0;

        playerController.SetWalkInputNormalized(input);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TrainingArea"))
        {
            Debug.Log("FAIL: hit the training area boundary");
            AddReward(-1f);
            ResetMaterialAndTag();
            EndEpisode();
            //if (isDailyActivitiesInOperation) RemoveFromActive();
            //else EndEpisode();
        }


        //Method II: A* Pathfinding
        //if (other.CompareTag("Goal")&& other.transform== nextGoal)
        //{
        //    //Debug.Log("Get one goal");
        //    //AddReward(.01f);
        //    pathFinding.UpdateClosestNode();
        //    nextGoal.position = girdSystem.WorldPointFromNode(pathFinding.closestNode);
        //}

        if (other.CompareTag("Water"))
        {
            Debug.Log("FAIL: fall off the city");
            AddReward(-1f);
            ResetMaterialAndTag();
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {        
        if (collision.transform==targetTransform)
        {
            Debug.Log("# FIND TARGET UNIT #");
            AddReward(5f);

            if (isDailyActivitiesInOperation)
            {               
                //int race = personalID[personalID.Length - 1] - '0';
                if (isResident && targetTransform==buildingResideIn)    //resident arrived home
                {
                    BuildingStatus target = targetTransform.parent.GetComponent<BuildingStatus>();
                    target.RegisterOrUpdateResident(personalID, true);
                    target.InitializeInvokeMethod();

                    navMeshAgent.enabled = false;
                    _characterPool.Release(transform.parent);
                }
                else if (isResident && targetTransform.parent.GetComponent<BuildingStatus>() != null)  //resident is visiting another building
                {
                    BuildingStatus target = targetTransform.parent.GetComponent<BuildingStatus>();
                    target.RegisterOrUpdateResidentVisitor(personalID,true);
                    target.InitializeInvokeMethod();

                    navMeshAgent.enabled = false;
                    _characterPool.Release(transform.parent);
                }
                else if (isResident && targetTransform.parent.GetComponent<BuildingStatus>() == null)  //resident left the area
                {
                    FindAnyObjectByType<DailyActivities>().UpdateResidentsOutsideNeighborhood(personalID, buildingResideIn);

                    navMeshAgent.enabled = false;
                    _characterPool.Release(transform.parent);
                }
                else if (!isResident && targetTransform.parent.GetComponent<BuildingStatus>()!=null) //passer-by is visiting a building
                {
                    BuildingStatus target = targetTransform.parent.GetComponent<BuildingStatus>();
                    target.RegisterOrUpdateVisitor(personalID, true, this);
                    target.InitializeInvokeMethod();

                    navMeshAgent.enabled = false;
                    _characterPool.Release(transform.parent);
                }
                else if (!isResident && targetTransform.parent.GetComponent<BuildingStatus>() == null) //passer-by walked through the area
                {
                    navMeshAgent.enabled = false;
                    _characterPool.Release(transform.parent);
                    FindAnyObjectByType<UIManager>().UpdateNonResidentCount(12, -1, -1);
                }
            }
            else    //in training mode
            {
                ResetMaterialAndTag();
                EndEpisode();
            }
        }
        else if (collision.collider.CompareTag("Building"))
        {
            Debug.Log("FAIL: enter the wrong unit");
            AddReward(-.01f);
            //ResetMaterialAndTag();
            //EndEpisode();

            //if (isDailyActivitiesInOperation) RemoveFromActive();
            //else EndEpisode();



            //AddReward(-.1f);

            //Collider currentCollider = collision.collider;
            //if (previousBuildingCollider == currentCollider)
            //{
            //    Debug.Log("collide with same building again");
            //    AddReward(-1f);
            //    ResetMaterialAndTag();
            //    EndEpisode();
            //}
            //previousBuildingCollider = currentCollider;
        }
        else if(collision.collider.CompareTag("Ground"))
        {
            Debug.Log("FAIL: walk onto the road");
            AddReward(-1f);
            ResetMaterialAndTag();
            EndEpisode();
        }

        else if(collision.collider.CompareTag("Tree"))
        {
            Debug.Log("PUNISHMENT: hit a tree");
            AddReward(-.1f);
        }
    }

    private void IsGettingCloser()
    {
        float currentDistance = Vector3.SqrMagnitude(targetWorldPos - transform.position);
        if (currentDistance < previousDistance) AddReward(.1f);
        //else AddReward(-.5f); Debug.Log("walking far away from target unity");
        previousDistance = currentDistance;
    }

    public void OnRightPath()
    {
        AddReward(.01f);
        //Debug.Log("On Path!");        
    }

    private void ResetMaterialAndTag()
    {
        if (targetTransform.parent.GetComponent<BuildingStatus>() != null)  //target is a building
        {
            targetTransform.GetComponent<MeshRenderer>().material = tempTargetMaterialBackup;
            targetTransform.gameObject.layer = LayerMask.NameToLayer("Building");
            targetTransform.tag = "Building";
        }

        markerTransform.GetComponent<Renderer>().material = tempMarkerMaterialBackup;
        markerTransform.gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void RemoveFromActive()
    {
        //int race = personalID[personalID.Length - 1] - '0';
        if (isResident && targetTransform == buildingResideIn)    //let the resident arrive home
        {
            BuildingStatus target = targetTransform.parent.GetComponent<BuildingStatus>();
            target.RegisterOrUpdateResident(personalID, true);
            target.InitializeInvokeMethod();

            navMeshAgent.enabled = false;
            _characterPool.Release(transform.parent);
        }
        else if (isResident && targetTransform.parent.GetComponent<BuildingStatus>() != null)  //let the resident arrive the building
        {
            BuildingStatus target = targetTransform.parent.GetComponent<BuildingStatus>();
            target.RegisterOrUpdateResidentVisitor(personalID, true);
            target.InitializeInvokeMethod();

            navMeshAgent.enabled = false;
            _characterPool.Release(transform.parent);
        }
        else if (isResident && targetTransform.parent.GetComponent<BuildingStatus>() == null)  //resident left the area
        {
            FindAnyObjectByType<DailyActivities>().UpdateResidentsOutsideNeighborhood(personalID, buildingResideIn);

            navMeshAgent.enabled = false;
            _characterPool.Release(transform.parent);
        }
        else if (!isResident && targetTransform.parent.GetComponent<BuildingStatus>() != null) //passer-by is visiting a building
        {
            BuildingStatus target = targetTransform.parent.GetComponent<BuildingStatus>();
            target.RegisterOrUpdateVisitor(personalID, true, this);
            target.InitializeInvokeMethod();

            navMeshAgent.enabled = false;
            _characterPool.Release(transform.parent);
        }
        else if (!isResident && targetTransform.parent.GetComponent<BuildingStatus>() == null) //passer-by walked through the area
        {

            navMeshAgent.enabled = false;
            _characterPool.Release(transform.parent);
            FindAnyObjectByType<UIManager>().UpdateNonResidentCount(12, -1, -1);
        }
    }


    /// <summary>
    /// building without BuildingStatus
    /// </summary>
    /// <param name="targetBuildingItself"></param>
    public void RegisterOrUpdateResidentInfo(Vector3 spawnLocation,Transform targetBuildingItself)
    {
        isResident = true;
        buildingResideIn = targetBuildingItself;        
        isDailyActivitiesInOperation = true;

        bornWorldPos = spawnLocation;
        targetTransform = targetBuildingItself;
    }

    /// <summary>
    /// destination is not set here
    /// </summary>
    /// <param name="spawnLocation"></param>
    public void RegisterOrUpdateNonresidentInfo(Vector3 spawnLocation,Transform targetLocation)
    {
        isResident = false;
        isDailyActivitiesInOperation = true;

        bornWorldPos = spawnLocation;
        targetTransform = targetLocation;
    }


    public bool IsResident()
    {
        return isResident;
    }

    public void HideMarker()
    {
        //mark the character in minimap
        markerTransform.GetComponent<MeshRenderer>().material = markerMaterial;
        markerTransform.gameObject.layer = LayerMask.NameToLayer("Marker");
    }

    private void UpdateCharacterInfo()
    {
        Label identityInfo = rootUI.Q<Label>(name:"Identity");
        Label targetInfo = rootUI.Q<Label>(name: "Target");
        Label homeInfo = rootUI.Q<Label>(name: "Home");
        identityInfo.Clear();
        targetInfo.Clear();
        homeInfo.Clear();

        if (isResident)
        {
            identityInfo.text = "Resident"; 
            homeInfo.text =$"{buildingResideIn.parent.name}";
        }            
        else identityInfo.text = $"Non-Resident";
        targetInfo.text= $"{targetTransform.parent.name}";

        identityInfo.style.color = Color.red;
        homeInfo.style.color = Color.red;
        targetInfo.style.color = Color.red;
    }

    public void TrySetCharacterPool(ObjectPool<Transform> pool)
    {
        if (_characterPool!=null) return;
        else _characterPool = pool; /*Debug.Log("pool is assigned");*/
    }

    public void SetID(string idToAssign)
    {
        personalID = idToAssign;
    }

        
}
