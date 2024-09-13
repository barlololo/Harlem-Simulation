using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// controller for the vehicle, 
/// also provides data for other scripts
/// </summary>
public class CarDriver : MonoBehaviour
{
    [Header("Vehicle Settings:")]
    [SerializeField] private float acceleration= 500f;
    [SerializeField] private float brakingForce=300f;
    [SerializeField] private float maxTurnAngle=15f;
    [SerializeField] private float MAXROTATIONSPEED = 5000;
    [SerializeField] private bool isBackWheelDrive = false;

    [Header("Wheels:")]
    [SerializeField] private Transform FLWheelTransform;
    [SerializeField] private Transform FRWheelTransform;
    [SerializeField] private Transform BLWheelTransform;
    [SerializeField] private Transform BRWheelTransform;
    [SerializeField] private WheelCollider FLWheelCollider;
    [SerializeField] private WheelCollider FRWheelCollider;
    [SerializeField] private WheelCollider BLWheelCollider;
    [SerializeField] private WheelCollider BRWheelCollider;
    
    private float currentAcceleration = 0f;
    private float currentBrakeForce = 0f;
    private float currentTurnAngle = 0f;

    [SerializeField] private GameInput gameInput;
    [SerializeField] private bool isControlledByAgent = false;
    [SerializeField] private bool isInSimulation=false;

    [Header("Lights:")]
    [SerializeField] private Transform frontLights;
    private LightManager lightManager;
    private bool stateSignal= false;
    private bool switchSignal = false;

    //-------------------------------------------------------------

    private void Awake()
    {
        lightManager = FindFirstObjectByType<LightManager>();

        //diable mlagents from running
        if (!isControlledByAgent) 
        {
            Academy.Instance.AutomaticSteppingEnabled= false;
            Academy.Instance.Dispose(); 
            GetComponent<CarDriverAgent>().GetComponent<DecisionRequester>().enabled = false;
            GetComponent<CarDriverAgent>().GetComponent<Agent>().enabled = false;
        }

        isInSimulation = transform.parent.GetComponent<ContainerForSignals>().TransferOperationSignal();
    }

    private void Start()
    {
        //lower the center of mass to prevent the car from rolling
        GetComponent<Rigidbody>().centerOfMass += Vector3.up * -1;

        if (isInSimulation)
        {
            //subsrcibe to event of LightManager and check current status
            lightManager.OnLightSwitchSignal += LightManager_OnLightSwitchSignal;
            stateSignal = lightManager.GetCurrentSignal();
        }
    }

    private void LightManager_OnLightSwitchSignal(object sender, LightManager.OnLightSwitchSignalEventArgs e)
    {
        stateSignal = e.signalOnLight;
    }

    private void Update()
    {
        if (isInSimulation && switchSignal != stateSignal)
        {
            switchSignal = stateSignal;
            frontLights.gameObject.SetActive(switchSignal);
        }
    }

    private void FixedUpdate()
    {
        //apply acceleration to some wheels        
        //if(!isTraining) { currentAcceleration = acceleration * Input.GetAxis("Vertical"); }
        if (!isControlledByAgent) currentAcceleration = acceleration * gameInput.GetDriveVector().y; 
        if (!isBackWheelDrive)
        {
            if (FLWheelCollider.rotationSpeed < MAXROTATIONSPEED)
            {
                FLWheelCollider.motorTorque = currentAcceleration;
                FRWheelCollider.motorTorque = currentAcceleration;
            }
            else
            {
                FLWheelCollider.motorTorque = 0;
                FRWheelCollider.motorTorque = 0;
            }
        }
        else
        {
            if (BLWheelCollider.rotationSpeed < MAXROTATIONSPEED)
            {
                BLWheelCollider.motorTorque = currentAcceleration;
                BRWheelCollider.motorTorque = currentAcceleration;
            }
            else
            {
                BLWheelCollider.motorTorque = 0;
                BRWheelCollider.motorTorque = 0;
            }
        }

        //apply brake to all wheels
        if(!isControlledByAgent) currentBrakeForce = brakingForce * gameInput.GetBrakeValue();
        FLWheelCollider.brakeTorque = currentBrakeForce;
        FRWheelCollider.brakeTorque = currentBrakeForce;
        BLWheelCollider.brakeTorque = currentBrakeForce;
        BRWheelCollider.brakeTorque = currentBrakeForce;

        //steering
        //if (!isTraining) { currentTurnAngle = maxTurnAngle * Input.GetAxis("Horizontal"); }        
        if (!isControlledByAgent) currentTurnAngle = maxTurnAngle * gameInput.GetDriveVector().x;
        FLWheelCollider.steerAngle= currentTurnAngle;
        FRWheelCollider.steerAngle = currentTurnAngle;

        //update visuals of all wheels
        UpdateWheel(FLWheelTransform, FLWheelCollider);
        UpdateWheel(FRWheelTransform, FRWheelCollider);
        UpdateWheel(BLWheelTransform, BLWheelCollider);
        UpdateWheel(BRWheelTransform, BRWheelCollider);
    }

    /// <summary>
    /// change the visual appearence of a wheel
    /// </summary>
    /// <param name="wheelTransform"></param>
    /// <param name="wheelCollider"></param>
    private void UpdateWheel(Transform wheelTransform, WheelCollider wheelCollider)
    {
        //get wheel collider state
        Vector3 position;
        Quaternion rotation;
        wheelCollider.GetWorldPose(out position, out rotation);

        //set wheel transform state
        wheelTransform.position = position;
        wheelTransform.rotation = rotation;
    }

    /// <summary>
    /// get inputs from the agent when in training mode
    /// </summary>
    /// <param name="accelerationAmount"></param>
    /// <param name="turnAmount"></param>
    public void SetInput(float accelerationAmount, float turnAmount, float brakeAmount)
    {
        currentAcceleration = acceleration * accelerationAmount;
        currentTurnAngle = maxTurnAngle * turnAmount;
        currentBrakeForce = brakingForce * brakeAmount;
    }

    /// <summary>
    /// reset this vehicle to its default status
    /// </summary>
    public void StopAndReset()
    {
        currentAcceleration = 0f;
        currentTurnAngle = 0f;
        currentBrakeForce = 0f;

        if (!isBackWheelDrive)
        {
            FLWheelCollider.motorTorque = 0;
            FRWheelCollider.motorTorque = 0;
        }
        else
        {
            BLWheelCollider.motorTorque = 0;
            BRWheelCollider.motorTorque = 0;
        }
                    
        FLWheelCollider.steerAngle = 0;
        FRWheelCollider.steerAngle = 0;

        GetComponent<Rigidbody>().velocity=Vector3.zero;
    }

    /// <summary>
    /// current rotation speed/MAXROTATIONSPEED, used for observation
    /// </summary>
    /// <returns></returns>
    public float RotationSpeedRatio()
    {
        if (!isBackWheelDrive) return FLWheelCollider.rotationSpeed/ MAXROTATIONSPEED;
        else return BLWheelCollider.rotationSpeed/ MAXROTATIONSPEED;
    }
}
