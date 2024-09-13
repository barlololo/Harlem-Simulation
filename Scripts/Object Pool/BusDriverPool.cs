using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BusDriverPool : MonoBehaviour
{
    public ObjectPool<Transform> _pool;

    [SerializeField] private Transform busTransformParent;
    private BusSchedule busSchedule;
    private int maxSize = 12;       // default size of running buses in 12 routes

    private void Awake()
    {
        busSchedule = GetComponent<BusSchedule>();
    }

    private void OnEnable()
    {
        if (busSchedule.isBusInOperation())
        {
            _pool = new ObjectPool<Transform>(CreateDriverAgent, OnTakeDriverAgentFromPool,
                OnReturnDriverAgentToPool, OnDestroyDriverAgent, true, maxSize, maxSize * 2);
            busSchedule.SetPool(_pool);
        }
    }

    private Transform CreateDriverAgent()
    {
        Transform spawnLocaiton = busSchedule.BusSpawnLocation();
        Transform agentParent = Instantiate(busTransformParent, spawnLocaiton.position, spawnLocaiton.rotation, busSchedule.GetTransformParent());
        return agentParent;
    }

    private void OnTakeDriverAgentFromPool(Transform agent)
    {
        Transform spawnLocaiton = busSchedule.BusSpawnLocation();
        agent.position = spawnLocaiton.position;
        agent.rotation = spawnLocaiton.rotation;
        agent.gameObject.SetActive(true);
    }

    private void OnReturnDriverAgentToPool(Transform agent)
    {
        agent.gameObject.SetActive(false);
    }

    private void OnDestroyDriverAgent(Transform agent)
    {
        Destroy(agent);
    }

}
