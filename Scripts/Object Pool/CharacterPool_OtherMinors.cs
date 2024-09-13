using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Pool;


public class CharacterPool_OtherMinors : MonoBehaviour
{
    public ObjectPool<Transform> _pool;

    [Header("Characters And Locations")]
    [SerializeField] private Transform character;
    private DailyActivities dailyActivities;

    private int maxSize;
    private int race;
    private bool isResident = true;

    private void Awake()
    {
        dailyActivities = GetComponent<DailyActivities>();
    }

    private void OnEnable()
    {
        if (dailyActivities.IsInOperation())
        {
            _pool = new ObjectPool<Transform>(CreateCharacterAgent, OnTakeCharacterAgentFromPool,
            OnReturnCharacterAgentToPool, OnDestroyCharacterAgent, true, maxSize, maxSize * 2);
            dailyActivities.TryAddPool(_pool, race);

        }
    }

    private Transform CreateCharacterAgent()
    {
        Transform agent = Instantiate(character, dailyActivities.GetTransformParent(isResident));
        return agent;
    }

    private void OnTakeCharacterAgentFromPool(Transform agent)
    {
        agent.gameObject.SetActive(true);
    }

    private void OnReturnCharacterAgentToPool(Transform agent)
    {
        agent.gameObject.SetActive(false);
    }

    private void OnDestroyCharacterAgent(Transform agent)
    {
        Destroy(agent);
    }

    public void SetMaxSizeAndRace(int size, int raceNumber)
    {
        maxSize = size;
        race = raceNumber;
    }
}

