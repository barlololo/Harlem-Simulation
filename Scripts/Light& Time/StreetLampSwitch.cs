using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLampSwitch : MonoBehaviour
{
    [SerializeField] private Transform lights;
    private LightManager lightManager;
    private bool switchSignal = false;
    private bool stateSignal = false;

    private void Awake()
    {
        lightManager= GameObject.FindFirstObjectByType<LightManager>();
    }

    private void Start()
    {
        //subscribe to event set in LightManager
        lightManager.OnLightSwitchSignal += LightManager_OnLightSwitchSignal;
    }

    private void LightManager_OnLightSwitchSignal(object sender, LightManager.OnLightSwitchSignalEventArgs e)
    {
        stateSignal = e.signalOnLight;
    }


    private void Update()
    {
        if (switchSignal != stateSignal)
        {
            switchSignal = stateSignal;
            lights.gameObject.SetActive(switchSignal);
        }
    }

    public void OnOffLights(bool signal)
    {
        lights.gameObject.SetActive(signal);
    }
}
