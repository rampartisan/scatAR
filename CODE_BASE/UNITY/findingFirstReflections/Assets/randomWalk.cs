using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Walk to a random position and repeat
[RequireComponent(typeof(NavMeshAgent))]
public class randomWalk : MonoBehaviour
{
	public GameObject listener;


	private NavMeshAgent m_agent;

	public GvrAudioSource motors;
	NavMeshTriangulation nmd;
	Vector3 point;
	void Start()
	{

		m_agent = GetComponent<NavMeshAgent>();

	}

	void Update()
	{
		
		motors.pitch = 1.0f + Mathf.Clamp((m_agent.velocity.magnitude / 10.0f) + Random.Range(-0.03f,0.03f),0.0f,0.15f);
		motors.volume = 0.7f + Mathf.Clamp ((m_agent.velocity.magnitude / 2.0f), 0.0f, 0.3f);


		if (m_agent.pathPending || m_agent.remainingDistance > 0.1f)
			return;

		if (Vector3.Distance (gameObject.transform.position, listener.transform.position) > 1.0f)
			return;

		point = Vector3.zero;

		do{
			nmd = NavMesh.CalculateTriangulation ();
		int t = Random.Range (0, nmd.indices.Length - 3);
		point = Vector3.Lerp(nmd.vertices[nmd.indices[t]], nmd.vertices[nmd.indices[t+1]], Random.value);
		Vector3.Lerp(point, nmd.vertices[nmd.indices[t+2]], Random.value);
		} while(Vector3.Distance(point,listener.transform.position) < 1.0f);
		m_agent.destination = point;


	}
}
