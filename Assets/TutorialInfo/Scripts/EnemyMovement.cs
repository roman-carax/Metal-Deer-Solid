using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] GameObject goal;
    NavMeshAgent agent;
    AudioSource source;
    void Awake()
    {
        agent  = GetComponent<NavMeshAgent>();
        source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        agent.destination = goal.transform.position;
        source.pitch = 1.5f*Time.timeScale;
    }
}
