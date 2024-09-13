using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/BusRoutes")]
public class BusRoutes : ScriptableObject
{
    public List<Transform> routeTransform;
}
