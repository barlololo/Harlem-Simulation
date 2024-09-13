using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Reference", menuName = "Scriptable Objects/Reference")]
public class ReferenceSO : ScriptableObject
{
    public SpawnLocationCollection spawnLocationCollection;
    public DailyActivities dailyActivities;
    public UIManager uIManager;
    public BusSchedule busSchedule;
    public LightManager lightManager;
}
