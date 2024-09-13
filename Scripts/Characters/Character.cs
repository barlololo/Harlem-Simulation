using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// controller for the character
/// </summary>
public class Character : MonoBehaviour
{
    [Header("Player Info:")]
    [SerializeField] private float walkSpeed= 7f;
    [SerializeField] private float rotateSpeed= .7f;
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private float playerRadius = .4f;

    [Header("Player Step Climb:")]
    [SerializeField] Transform stepRayUpper;
    [SerializeField] Transform stepRayLower;
    [SerializeField] float stepHeight = 0.3f;
    [SerializeField] float stepSmooth = 0.3f;
    [SerializeField] float upperRayDistance = 0.2f;
    [SerializeField] float lowerRayDistance = 0.1f;

    [Header("Player Input:")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private bool isControlledByAgent=false;

    private Vector2 inputVector;    //value from GameInput
    private LayerMask layerToMask;
    private LayerMask layerToInteract;

    private void Awake()
    {
        inputVector= new Vector2();
        stepRayUpper.transform.position = new Vector3(stepRayUpper.position.x, stepRayLower.position.y+stepHeight, stepRayUpper.position.z);

        if (Time.timeScale != 1)    //used in fast training process
        {
            upperRayDistance *= Time.timeScale;
            lowerRayDistance *= Time.timeScale;
        }
                    
        if (!isControlledByAgent)   //diable mlagents from running
        {
            Academy.Instance.AutomaticSteppingEnabled = false;
            Academy.Instance.Dispose();
            GetComponent<CharacterAgent>().GetComponent<DecisionRequester>().enabled = false;
            GetComponent<CharacterAgent>().GetComponent<Agent>().enabled = false;
            GetComponent<CharacterAgent>().HideMarker();    //disable marker in function
        }
    }

    private void Start()
    {
        layerToMask = ~(1 << LayerMask.NameToLayer("Checkpoint"));
        layerToInteract = 1 << LayerMask.NameToLayer("Pathway");
    }

    private void FixedUpdate()
    {
        if (!isControlledByAgent)       inputVector = gameInput.GetWalkVector().normalized;    //freeplay
        MovementInFirstPerson();
        if (inputVector.y != 0)         StepClimb();
    }

    //private void MovementThirdPerson()
    //{
    //    Vector3 walkDirection = new Vector3(inputVector.x, 0, inputVector.y);
    //    float walkDistance = walkSpeed * Time.deltaTime;
    //    bool canWalk = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight,
    //        playerRadius, walkDirection, out RaycastHit raycastHit, playerRadius, 2, QueryTriggerInteraction.Ignore);

    //    if (!canWalk)
    //    {
    //        //attempt on perpendicular direction to the normal
    //        Vector3 walkDirPerp = (walkDirection - Vector3.Project(walkDirection, raycastHit.normal)).normalized;

    //        canWalk = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight,
    //            playerRadius, walkDirPerp, playerRadius, 2, QueryTriggerInteraction.Ignore);

    //        if (canWalk) walkDirection = walkDirPerp;
    //        else walkDirection = Vector3.zero;  //might walk into a corner
    //    }
    //    transform.position += walkDirection * walkDistance;

    //    //rotation                                      
    //    if (walkDirection != Vector3.zero) transform.forward = Vector3.Slerp(transform.forward, walkDirection, Time.deltaTime * rotateSpeed);
    //}


    private void MovementInFirstPerson()
    {
        if (inputVector.y != 0)
        {
            Vector3 walkDirection = (transform.forward * inputVector.y).normalized;

            if (Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius,
                 walkDirection, out RaycastHit raycastHit, walkSpeed * Time.deltaTime, layerToMask, QueryTriggerInteraction.Ignore))
            {
                //Debug.Log("first try hit");
                Vector3 walkDirPerp = (walkDirection - Vector3.Project(walkDirection, raycastHit.normal)).normalized; //test on perpendicular

                if (Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight,
                    playerRadius, walkDirPerp, walkSpeed * Time.deltaTime, layerToMask, QueryTriggerInteraction.Ignore))
                    walkDirection = Vector3.zero;                                           //not walkable
                else walkDirection = walkDirPerp; /*Debug.Log("second try not hit");  */        //walkable 
            }
            transform.position += walkDirection * walkSpeed * Time.deltaTime;
        }

        if (inputVector.x != 0)
        {
            Vector3 turn = (Vector3.Cross(Vector3.up, transform.forward) * inputVector.x).normalized;
            transform.forward = Vector3.Slerp(transform.forward, turn, Time.deltaTime * rotateSpeed);
        }
            
    }


    private void StepClimb()
    {
        if (Physics.Raycast(stepRayLower.position, transform.TransformDirection(Vector3.forward),lowerRayDistance, layerToInteract, QueryTriggerInteraction.Ignore))
        {            
            if (!Physics.Raycast(stepRayUpper.position, transform.TransformDirection(Vector3.forward),  upperRayDistance))
            {
                //Debug.Log("Upper clear");
                transform.position -= new Vector3(0f, -stepSmooth, 0f);
                return;
            }
        }

        if (Physics.Raycast(stepRayLower.position, transform.TransformDirection(1.5f, 0, 1), lowerRayDistance*1.5f, layerToInteract, QueryTriggerInteraction.Ignore))
        {
            if (!Physics.Raycast(stepRayUpper.position, transform.TransformDirection(1.5f, 0, 1), upperRayDistance))
            {
                //Debug.Log("45 Upper clear");
                transform.position -= new Vector3(0f, -stepSmooth, 0f);
                return;
            }
        }

        if (Physics.Raycast(stepRayLower.position, transform.TransformDirection(-1.5f, 0, 1),lowerRayDistance * 1.5f, layerToInteract, QueryTriggerInteraction.Ignore))
        {
            if (!Physics.Raycast(stepRayUpper.position, transform.TransformDirection(-1.5f, 0, 1), upperRayDistance))
            {
    
                transform.position -= new Vector3(0f, -stepSmooth, 0f);
                return;
            }
        }
    }

    /// <summary>
    /// agent controls the character and sends orders on walking to which direction
    /// </summary>
    /// <param name="inputValuesVectorized"></param>
    /// <returns></returns>
    public void SetWalkInputNormalized(Vector2 inputValuesVectorized)
    {
        inputVector= inputValuesVectorized.normalized;
    }
}
