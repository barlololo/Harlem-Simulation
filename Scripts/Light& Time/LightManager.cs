using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LightManager : MonoBehaviour
{
    [Header("Sunlight Settings:")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private LightPreset preset;
    [SerializeField] private float dayLength = 1440;
    [SerializeField, Range(0, 1440)] private float timeOfDay;
    [SerializeField] TextMeshProUGUI timeText;
    private StringBuilder timerText= new StringBuilder();

    private readonly float sunriseTime = 360f;
    private readonly float sunsetTime = 1080f;
    private bool signalTrigger = false;

    [Header("Interior Light:")]
    [SerializeField] private Material lightEmissive;    

    //to inform listeners that light should be turned ON or OFF
    public event EventHandler<OnLightSwitchSignalEventArgs> OnLightSwitchSignal;
    public class OnLightSwitchSignalEventArgs : EventArgs    {public bool signalOnLight;}

    [SerializeField] private DailyActivities dailyActivities;

    //private void Awake()
    //{
    //    //Method II: ScriptableObject with LightListener
    //    //if (timeOfDay < sunriseTime || timeOfDay > sunsetTime)
    //    //{
    //    //    signalTrigger = true;
    //    //    lightListener.signalOnLight = signalTrigger;
    //    //}
    //    //else
    //    //{
    //    //    signalTrigger = false;
    //    //    lightListener.signalOnLight = signalTrigger;
    //    //}
    //    isInOperation = dailyActivities.IsInOperation();
    //}


    private void Update()
    {
        timeOfDay += Time.deltaTime;
        timeOfDay %= dayLength;
        UpdateLight(timeOfDay / dayLength);
        UpdateTimeText();

        if(timeOfDay < sunriseTime&& signalTrigger == false)
        {
            signalTrigger = true;
            InteriorLight();
            //dailyActivities.UpdateInvokingMethod(1);
            OnLightSwitchSignal?.Invoke(this, new OnLightSwitchSignalEventArgs { signalOnLight=signalTrigger});
        }
        else if (timeOfDay > sunriseTime && timeOfDay < sunsetTime && signalTrigger == true)
        {
            signalTrigger = false;
            InteriorLight();
            //dailyActivities.UpdateInvokingMethod(2);
            OnLightSwitchSignal?.Invoke(this, new OnLightSwitchSignalEventArgs { signalOnLight = signalTrigger });
        }
        else if(timeOfDay>sunsetTime && signalTrigger == false)
        {
            signalTrigger = true;
            InteriorLight();
            //dailyActivities.UpdateInvokingMethod(3);
            OnLightSwitchSignal?.Invoke(this, new OnLightSwitchSignalEventArgs { signalOnLight = signalTrigger });
        }



        //if (preset == null)
        //    return;

        //if (Application.isPlaying)
        //{
        //    timeOfDay += Time.deltaTime;
        //    timeOfDay %= dayLength;
        //    UpdateLight(timeOfDay/ dayLength);
        //}
        //else
        //{
        //    UpdateLight(timeOfDay/ dayLength);
        //}
    }

    /// <summary>
    /// update sunlight according to time
    /// </summary>
    /// <param name="timePercent"></param>
    private void UpdateLight(float timePercent)
    {
        RenderSettings.ambientLight=preset.AmbientColor.Evaluate(timePercent);
        RenderSettings.fogColor=preset.FogColor.Evaluate(timePercent);

        if(directionalLight != null)
        {
            directionalLight.color=preset.DirectionalColor.Evaluate(timePercent);
            directionalLight.transform.localRotation = Quaternion.Euler(new Vector3(timePercent * 360f - 90f, 170f, 0));
        }
    }

    private void OnValidate()
    {
        if (directionalLight != null)
            return;

        if(RenderSettings.sun!= null) directionalLight = RenderSettings.sun;
        else
        {
            Light[] lights=GameObject.FindObjectsOfType<Light>();
            foreach (Light light in lights)
                if (light.type == LightType.Directional) directionalLight = light; return;
        }
    }

    private void UpdateTimeText()
    {
        //int hour = (int)timeOfDay / 60;
        //int min = (int)timeOfDay % 60;
        //if (timeText != null) timeText.text = $"TIME \n{hour:D2}:{min:D2}";
        timerText.Length = 0;
        timerText.Append("TIME \n");
        timerText.Append($"{(int)timeOfDay / 60:D2}:{(int)timeOfDay % 60:D2}");
        timeText.text = timerText.ToString();
    }

    public bool GetCurrentSignal()
    {
        return signalTrigger;
    }


    /// <summary>
    /// control window light visuals
    /// </summary>
    private void InteriorLight()
    {
        if(signalTrigger)
        {
            lightEmissive.EnableKeyword("_EMISSION");
            lightEmissive.SetColor("_EmissionColor", Color.white);
        }
        else
        {
            lightEmissive.DisableKeyword("_EMISSION");
        }
    }
}
