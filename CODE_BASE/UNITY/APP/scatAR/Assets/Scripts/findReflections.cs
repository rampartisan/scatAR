using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class findReflections : MonoBehaviour
{
	
	public GameObject listener;
	public GameObject debugDrawParent;
	public int maxReflectionOrder = 1;
	public int maxNumberOfPaths = 6;

	public Material directSoundMat;
	public Material reflectionMat;
	public Material junctionMat;


	public bool drawDebug = true;


	private Vector3 spos;
	private Vector3 lpos;
	private earlyReflections ER;

	void Start ()
	{
		ER = new earlyReflections (this.gameObject, listener.gameObject, maxNumberOfPaths, maxReflectionOrder);

		for (int i = 0; i < maxNumberOfPaths + 1; i++) {
			GameObject path = new GameObject ("debugDrawPath" + i.ToString ());
			path.transform.SetParent (debugDrawParent.transform);
			LineRenderer lr = path.AddComponent<LineRenderer> ();
			lr.enabled = false;
			lr.startWidth = 0.05f;
			lr.endWidth = 0.05f;

			if (i > 0) {
				GameObject junction = GameObject.CreatePrimitive (PrimitiveType.Cube);
				junction.transform.localScale = new Vector3 (0.2f, 0.2f, 0.2f);
				junction.GetComponent<MeshRenderer>().enabled = false;
				Destroy (junction.GetComponent<BoxCollider> ());				
				junction.transform.SetParent (debugDrawParent.transform);
				junction.name = "debugJunction" + i.ToString ();
			}
		
		}
		spos = this.gameObject.transform.position;
		lpos = listener.transform.position;
	}

	void Update ()
	{
		if (Mathf.Abs (Vector3.Distance (listener.transform.position, lpos)) > 0.1 || Mathf.Abs (Vector3.Distance (this.gameObject.transform.position, spos)) > 0.1) {

			spos = this.gameObject.transform.position;
			lpos = listener.transform.position;

			if (ER != null) {
			
				ER.updateAllPaths ();

				if (drawDebug) {
					drawPaths ();
					debugDrawParent.SetActive (true);
				} else {
					debugDrawParent.SetActive (false);
				}

			}
		}
	}

	void drawPaths ()
	{
		int lrCount = 0;
		int mrCount = 0;
		LineRenderer[] lr = debugDrawParent.GetComponentsInChildren<LineRenderer> ();
		MeshRenderer[] mr = debugDrawParent.GetComponentsInChildren<MeshRenderer> ();
		if (ER.directSound.isValid) {
			lr [lrCount].positionCount = 2;
			lr [lrCount].SetPosition (0, ER.directSound.origin);
			lr [lrCount].SetPosition (1, ER.directSound.destination);
			lr[lrCount].material = directSoundMat;
			lr [lrCount].enabled = true;
			lrCount++;
		}
			
		foreach (path p in ER.reflections) {
			if (p.isValid) {

				lr [lrCount].positionCount = p.segments.Count + 1;

				Vector3[] positions = new Vector3[p.segments.Count + 1];

				for (int i = 0; i < positions.Length; i++) {
					if (i < positions.Length - 1) {
						positions[i] = p.segments [i].origin;
					} else {
						positions [i] = p.destination;
					}
				}

				lr [lrCount].SetPositions (positions);
				lr [lrCount].enabled = true;
				lrCount++;
				mr [mrCount].gameObject.transform.position = p.segments [1].origin;
				mr [mrCount].enabled = true;
				mrCount++;
			}
		}



		for (int i = lrCount; i < lr.Length; i++) {
			lr [i].enabled = false;
		}
		for (int i = mrCount; i < mr.Length; i++) {

			mr [i].enabled = false;
		}

	}


	private class path
	{
		public Vector3 origin;
		public Vector3 destination;
		public List<Ray> segments;
		public bool isValid;

		public path (Vector3 sourcePos, Vector3 listenerPos)
		{
			origin = sourcePos;
			destination = listenerPos;
			segments = new List<Ray> ();
			isValid = false;

		}
	}

	private class earlyReflections
	{
		private Vector3[] simpleDirections = new Vector3[] {
			Vector3.right,
			Vector3.left,
			Vector3.up,
			Vector3.down,
			Vector3.back,
			Vector3.forward
		};
			
		private GameObject source;
		private GameObject listener;

		public int maxNumPath;
		public int maxNumReflec;

		public path directSound;

		public List<path> reflections;

		public earlyReflections (GameObject sourceObject, GameObject listenerObject, int maxNumberOfPaths, int maxNumberOfReflections)
		{
			source = sourceObject;
			listener = listenerObject;

			maxNumPath = maxNumberOfPaths;
			maxNumReflec = maxNumberOfReflections; 

			directSound = new path (source.transform.position, listener.transform.position);
			directSound.segments.Add (new Ray (Vector3.zero, Vector3.zero));

			reflections = new List<path> ();

			updateAllPaths ();
		}

		public void updateAllPaths ()
		{
			updateDirectSound ();
			updateReflections ();
		}

		public void updateDirectSound ()
		{
			directSound.isValid = false;

			directSound.origin.Set (source.transform.position.x, source.transform.position.y, source.transform.position.z);
			directSound.destination.Set (listener.transform.position.x, listener.transform.position.y, listener.transform.position.z);

			float distance = Vector3.Distance (directSound.origin, directSound.destination);
			directSound.segments [0] = new Ray (directSound.origin, directSound.destination - directSound.origin);
	
			RaycastHit hit;

			if (Physics.Raycast (directSound.segments [0], out hit, distance)) {
				if (hit.collider.name == listener.name) {
					directSound.isValid = true;
				} 
			}
		}

		public void updateReflections ()
		{

			reflections.Clear ();
		
			foreach (Vector3 dir in simpleDirections) {

				//find the point at which a ray travelling this dir will hit something in the room
				Ray ray = new Ray (source.transform.position, dir);
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit)) {
					if (hit.collider.name != listener.name) {

						Vector3 imageSource = hit.point + (dir * hit.distance);
						Ray imageSourceRay = new Ray (imageSource, listener.transform.position - imageSource);

						if (Physics.Raycast (imageSourceRay, out hit)) {
							if (hit.collider.name != listener.name) {
								path currPath = new path (source.transform.position, listener.transform.position);
								currPath.segments.Add (new Ray (source.transform.position, hit.point - source.transform.position));
								currPath.segments.Add (new Ray (hit.point, listener.transform.position - hit.point));
								currPath.isValid = true;
								reflections.Add (currPath);
							}
						}
					}
				}
			}
		}


	}


}
