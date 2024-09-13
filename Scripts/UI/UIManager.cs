using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI Animation:")]
    [SerializeField] private GameInput gameInput;
    [SerializeField] private Animator uiAnimator;
    private bool homepageFinished = false;

    [Header("Camera Switch:")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera characterCamera;
    [SerializeField] private Camera driverCamera;
    private bool characterTrigger= false;
    private bool driverTrigger= false;

    //settings for camera zoom
    private readonly float smoothTime =1f;   
    private float currentZoom;
    enum CameraProjectionSize
    {
        first = 20,
        second = 100,
        third = 400,
    }
    CameraProjectionSize currentProjectionSize = CameraProjectionSize.third;

    enum CameraMoveSpeed
    {
        first = 1,
        second = 2,
        third = 4,
    }
    CameraMoveSpeed currentMoveSpeed = CameraMoveSpeed.third;
    private readonly int edgeScrollSize = 50;
    //private bool cameraPanActivated;
    //private Vector2 lastMousePosition;

    enum CameraInActive
    {
        mainCamera= 0,
        characterCamera=1,
        driverCamera= 2,
    }
    CameraInActive cameraInActive=CameraInActive.mainCamera;


    [Header("Statistics:")]
    [SerializeField] private Transform homepageCanvas;
    private TextMeshProUGUI[] characterTMP;
    [SerializeField] private Transform characterPanel;
    [SerializeField] private TextMeshProUGUI characterTotal;
    [SerializeField] private DailyActivities dailyActivities;
    private TextMeshProUGUI[] busTMP;
    [SerializeField] private Transform busPanel;
    [SerializeField] private TextMeshProUGUI busTotal;
    [SerializeField] private BusSchedule busSchedule;

    //statistics info: for 13 entries
    private int[] residentCount;
    private int[] residentCountActive;
    private int[] nonResidentCount;
    private int[] nonResidentCountActive;

    //statistics info: for 12 entries
    private int[] busRunning;
    private int[] nextTimer;

    [Header("Bus Info:")]
    [SerializeField] private TextMeshProUGUI busRouteName;


    private void Awake()
    {
        characterTMP =new TextMeshProUGUI[13];
        for (int i = 0; i < characterPanel.childCount; i++) characterTMP[i] = characterPanel.GetChild(i).GetComponent<TextMeshProUGUI>(); 
        busTMP = new TextMeshProUGUI[12];
        for (int i = 0; i < busPanel.childCount; i++) busTMP[i] = busPanel.GetChild(i).GetComponent<TextMeshProUGUI>();

        residentCount = new int[13];
        nonResidentCount = new int[13];
        residentCountActive = new int[13];
        nonResidentCountActive = new int[13];

        busRunning = new int[12];
        nextTimer = new int[12];
    }

    void Start()
    {
        ManageCamera();
    }

    void Update()
    {
        //start homepage
        if (!homepageFinished && gameInput.GetMouseClickValue())
        {
            uiAnimator.SetTrigger("homepage_out");
            homepageFinished = true;
        }

        //main camera view
        if ((int)cameraInActive == 0)   
        {
            CameraZoom(mainCamera);
            EdgeScrolling(mainCamera);
            //CameraPan(mainCamera);

            if (uiAnimator.GetBool("driver_in"))
            {
                cameraInActive= CameraInActive.driverCamera;
                ManageCamera();
            }

            if (uiAnimator.GetBool("character_in"))
            {
                cameraInActive = CameraInActive.characterCamera;
                ManageCamera();
            }

            if (uiAnimator.GetBool("statistics_in"))
            {
                UpdateStatistics();
                InvokeRepeating(nameof(UpdateStatistics), 3, 3);
            }

            if (uiAnimator.GetBool("statistics_out"))
            {
                CancelInvoke(nameof(UpdateStatistics));
                Debug.Log("invoke canceled==");
            }
        }

        //character camera view
        if ((int)cameraInActive == 1)   
        {
            if (uiAnimator.GetBool("axo_in"))
            {
                cameraInActive = CameraInActive.mainCamera;
                ManageCamera();
                Debug.Log("character camera view cancelled");
            }

            if (!characterTrigger && uiAnimator.GetBool("character_refresh"))
            {
                characterCamera.gameObject.SetActive(false);                
                characterCamera = FindObjectOfType<CharacterAgent>().transform.GetChild(1).GetComponent<Camera>();
                characterCamera.gameObject.SetActive(true);
                cameraInActive = CameraInActive.characterCamera;
                characterTrigger= true;
                //Debug.Log("character_refresh\"");
            }
        }

        //driver camera view
        if ((int)cameraInActive == 2)   
        {
            if (uiAnimator.GetBool("axo_in"))
            {
                cameraInActive = CameraInActive.mainCamera;
                ManageCamera();
            }

            if (!driverTrigger && uiAnimator.GetBool("driver_refresh"))
            {
                driverCamera.gameObject.SetActive(false);

                CarDriverAgent driver = FindObjectOfType<CarDriverAgent>();
                busRouteName.text = driver.GetRouteTrack().transform.name;
                driverCamera = driver.transform.GetChild(2).GetComponent<Camera>();
                driverCamera.gameObject.SetActive(true);
                cameraInActive = CameraInActive.driverCamera;
                driverTrigger = true;
            }

        }

    }

    public void OnClick()
    {
        characterTrigger = !characterTrigger;
        driverTrigger = !driverTrigger;
    }

    public void ManageCamera()
    {
        if ((int)cameraInActive == 0)   Cam_main();         
        if ((int)cameraInActive == 1)   Cam_character();    
        if ((int)cameraInActive == 2)   Cam_driver();       
    }

    void Cam_main() 
    { 
        mainCamera.gameObject.SetActive(true); 
        characterCamera.gameObject.SetActive(false);
        driverCamera.gameObject.SetActive(false);
    }

    void Cam_character()
    {
        mainCamera.gameObject.SetActive(false);
        characterCamera=FindAnyObjectByType<CharacterAgent>().transform.GetChild(1).GetComponent<Camera>();
        characterCamera.gameObject.SetActive(true);
        driverCamera.gameObject.SetActive(false);
    }

    void Cam_driver()
    {
        mainCamera.gameObject.SetActive(false);
        characterCamera.gameObject.SetActive(false);
        CarDriverAgent driver = FindAnyObjectByType<CarDriverAgent>();
        busRouteName.text = driver.GetRouteTrack().transform.name;
        driverCamera = driver.transform.GetChild(2).GetComponent<Camera>();
        //driverCamera = FindAnyObjectByType<CarDriverAgent>().transform.GetChild(2).GetComponent<Camera>();

        driverCamera.gameObject.SetActive(true);
    }

    /// <summary>
    /// Main Camera View: zoom in and out
    /// </summary>
    /// <param name="camera"></param>
    private void CameraZoom(Camera camera)
    {
        if (gameInput.GetMouseScrollValue() < 0)
        {
            currentProjectionSize = currentProjectionSize.GetNextEnumValue();
            currentMoveSpeed= currentMoveSpeed.GetNextEnumValue();
            currentZoom = (float)currentProjectionSize;
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, currentZoom, smoothTime);
        }
        else if (gameInput.GetMouseScrollValue() > 0)
        {
            currentProjectionSize = currentProjectionSize.GetPreviousEnumValue();
            currentMoveSpeed= currentMoveSpeed.GetPreviousEnumValue();
            currentZoom = (float)currentProjectionSize;
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, currentZoom, smoothTime);
        }
    }

    /// <summary>
    /// Main Camera View: move through the canvas
    /// </summary>
    /// <param name="camera"></param>
    private void EdgeScrolling(Camera camera)
    {
        Vector2 moveDir = new Vector2();
        if (gameInput.GetMousePanValue().x < edgeScrollSize)                    moveDir.x = .5f;
        if (gameInput.GetMousePanValue().x > Screen.width+ edgeScrollSize)      moveDir.x = -.5f;
        if (gameInput.GetMousePanValue().y < edgeScrollSize)                    moveDir.y = .5f;
        if (gameInput.GetMousePanValue().y > Screen.height- edgeScrollSize)     moveDir.y = -.5f;

        camera.transform.position += new Vector3(moveDir.x, 0, moveDir.y)*(float)currentMoveSpeed;
    }

    //private void CameraPan(Camera camera)
    //{
    //    if (gameInput.GetMouseClickValue())
    //    {
    //        cameraPanActivated = true;
    //        lastMousePosition= gameInput.GetMousePanValue();
    //    }
    //    if (gameInput.GetMouseReleaseValue()) cameraPanActivated = false;

    //    if (cameraPanActivated)
    //    {
    //        Vector2 mouseMovementDelta= gameInput.GetMousePanValue() - lastMousePosition;
    //        camera.transform.position += new Vector3(mouseMovementDelta.x, 0, mouseMovementDelta.y) * (float)currentMoveSpeed;
    //        lastMousePosition =gameInput.GetMousePanValue();
    //    }
    //}

    private void UpdateStatistics()
    {
        //update character
        string[] character=new string[13] {"Apartment","Church","Commercial","Community","Firehouse","Hospital",
            "House","Mixed","Parking","Police","Postal","School","Pass-by"};

        int characterCount = 0; int characterActive = 0;
        for(int i = 0; i < characterTMP.Length; i++)
        {
            characterTMP[i].text = 
                String.Format("<mspace=mspace=19>{0,-15}</mspace>{1}" + "({2})" + "{3,18}"+"({4})", 
                character[i], residentCount[i], residentCountActive[i], nonResidentCount[i], nonResidentCountActive[i]);
            
            characterCount += (residentCount[i] + nonResidentCount[i]);
            characterActive += (residentCountActive[i] + nonResidentCount[i]);
        }
        characterTotal.text= String.Format("{0,-20}{1,-18}{2,6}", "TOTAL", characterCount, characterActive); 

        //update bus
        string[] bus = new string[12] {"BX33/E","BX33/W","M1/S  ","M1/N  ","M2/S  ","M2/N  ",
            "M7/S  ","M7/N  ","M10/S ","M10/N ","M102/S","M102/N"};

        int busCount = 0;
        for (int i = 0; i < busTMP.Length; i++)
        {
            string timeString = $"{(int)nextTimer[i] / 60:D2}:{(int)nextTimer[i] % 60:D2}";
            busTMP[i].text =String.Format("<mspace=mspace=19>{0,-17}</mspace>{1,-15}{2,6}", bus[i], busRunning[i], timeString);

            busCount += busRunning[i];
        }
        busTotal.text= String.Format("{0,-24}{1,-15}", "TOTAL", busCount);

    }

    public void InitializeResidentCount(int buildingType, int ResidentChange)
    {
        residentCount[buildingType] += ResidentChange;
    }

    public void UpdateResidentActiveCount(int buildingType, int activeResidentChange)
    {
        residentCountActive[buildingType] += activeResidentChange;
    }

    public void UpdateNonResidentCount(int buildingType, int totalChange,int activeNonChange)
    {
        nonResidentCount[buildingType] += totalChange;
        nonResidentCountActive[buildingType] += activeNonChange;
    }

    public void AddBusStatus(int routeIndex,int totalChange, int nextTime)
    {
        busRunning[routeIndex] += totalChange;
        nextTimer[routeIndex] = nextTime;
    }

    public void RemoveBusFromRoute(int routeIndex)
    {
        busRunning[routeIndex] -= 1;
    }


}



public static class EnumExtensions
{
    public static T GetNextEnumValue<T>(this T currentValue) where T : Enum
    {
        T[] values = (T[])Enum.GetValues(typeof(T));
        int currentIndex = Array.IndexOf(values, currentValue);
        if (currentIndex < values.Length-1) return values[(currentIndex + 1) % values.Length]; 
        else return values[currentIndex];
    }

    public static T GetPreviousEnumValue<T>(this T currentValue) where T : Enum
    {
        T[] values = (T[])Enum.GetValues(typeof(T));
        int currentIndex = Array.IndexOf(values, currentValue);
        if (currentIndex ==0) return values[currentIndex];
        else return values[(currentIndex - 1 + values.Length) % values.Length];
    }
}
