using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Walk to a random position and repeat
[RequireComponent(typeof(NavMeshAgent))]
public class randomWalk : MonoBehaviour
{
	public GameObject listener;
	public GvrAudioSource motors;

	private Vector3 point;
	private NavMeshAgent NMA;
	private NavMeshTriangulation NMT;
	void Start()
	{
		NMA = gameObject.GetComponent<NavMeshAgent> ();

	}

	void Update()
	{
		if (!NMA.isStopped) {
			motors.pitch = 1.0f + Mathf.Clamp ((NMA.velocity.magnitude / 10.0f) + Random.Range (-0.05f, 0.05f), 0.0f, 0.15f);
			motors.volume = 0.1f + Mathf.Clamp ((NMA.velocity.magnitude / 2.0f), 0.1f, 0.2f);
		} else {
			motors.volume = 0.1f;

		}
		if (NMA.pathPending || NMA.remainingDistance > 0.1f) {
			return;
		}
			
		if (Vector3.Distance (gameObject.transform.position, listener.transform.position) > 1.0f && Random.Range(0,400) != 0) {
			return;
		}

		point = Vector3.zero;
		NMT = NavMesh.CalculateTriangulation ();
		do{
			int t = Random.Range (0, NMT.indices.Length - 3);
			point = Vector3.Lerp(NMT.vertices[NMT.indices[t]], NMT.vertices[NMT.indices[t+1]], Random.value);
			Vector3.Lerp(point, NMT.vertices[NMT.indices[t+2]], Random.value);
		} while(Vector3.Distance(point,listener.transform.position) < 1.0f);
		NMA.destination = point;
	
	}
}
