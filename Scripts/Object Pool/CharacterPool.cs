using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class CharacterPool : MonoBehaviour
{
    //public ObjectPool<GameObject> _pool;

    //[SerializeField] private GameObject character;
    //private CharacterAgent characterAgent;
    //private DailyActivities dailyActivities;

    //private int maxSize=200;
    //private bool isResident = true;

    //private void Awake()
    //{
    //    dailyActivities = GetComponent<DailyActivities>();
    //    characterAgent = character.GetComponentInChildren<CharacterAgent>();
    //}

    //private void OnEnable()
    //{
    //    if (dailyActivities.IsInOperation())
    //    {
    //        _pool = new ObjectPool<GameObject>(CreateCharacterAgent, OnTakeCharacterAgentFromPool,
    //            OnReturnCharacterAgentToPool, OnDestroyCharacterAgent, true, maxSize, maxSize * 2);
    //        dailyActivities.SetPool(_pool);
    //    }
    //}   

    //private GameObject CreateCharacterAgent()
    //{
    //    GameObject agent = Instantiate(character, dailyActivities.GetTransformParent(isResident));
    //    characterAgent.SetCharacterPool(_pool);

    //    return agent;
    //}

    //private void OnTakeCharacterAgentFromPool(GameObject agent)
    //{
    //    agent.SetActive(true);
    //}

    //private void OnReturnCharacterAgentToPool(GameObject agent)
    //{
    //    agent.SetActive(false);
    //}

    //private void OnDestroyCharacterAgent(GameObject agent)
    //{
    //    Destroy(agent);
    //}

    //public void SetMaxSize(int size)
    //{
    //    maxSize = size;
    //}
}
