using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Light Listener")]

public class LightListener : ScriptableObject
{
    public Transform lightManager;
    public bool signalOnLight;
}
