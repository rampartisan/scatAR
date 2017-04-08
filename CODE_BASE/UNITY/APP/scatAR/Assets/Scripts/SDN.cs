using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDN : MonoBehaviour {

	public GameObject E_SDNListener;

	public int targetNumReflections = 6;

	public bool drawNetwork = true;
	public bool freezeNetwork = false;

	public GameObject debugDrawParent;
	public Material directSoundMat;
	public Material reflectionMat;
	public Material junctionMat;

	private reflectionFinder RF;

	void Start () {

		RF = new reflectionFinder (this.gameObject,E_SDNListener,targetNumReflections);

		for (int i = 0; i < targetNumReflections + 1; i++) {
			GameObject path = new GameObject ("debugDrawPath" + i.ToString ());
			path.transform.SetParent (debugDrawParent.transform);
			LineRenderer lr = path.AddComponent<LineRenderer> ();
			lr.enabled = false;
			lr.startWidth = 0.05f;
			lr.endWidth = 0.05f;

			if (i > 0) {
				GameObject junction = GameObject.CreatePrimitive (PrimitiveType.Cube);
				junction.transform.localScale  = new Vector3(0.1f, 0.1f, 0.1f);
				MeshRenderer mr = junction.GetComponent<MeshRenderer> ();
				mr.enabled = false;
				Destroy (junction.GetComponent<BoxCollider> ());				
				junction.transform.SetParent (debugDrawParent.transform);
				junction.name = "debugJunction" + i.ToString ();
				mr.material = junctionMat;

			}

		}
		
	}

	void OnDrawGizmos() {

		if (RF != null) {
			print (RF.basicDirections.Length);

			foreach (Vector3 v in RF.basicDirections) {
				Gizmos.DrawRay (this.transform.position, v);

			}

		}
	}

	void Update () {
		
		if (RF != null) {
			if (!freezeNetwork) {
				RF.updateAllPaths ();
			}
			if (drawNetwork) {
				drawPaths ();
				debugDrawParent.SetActive (true);
			} else {
				debugDrawParent.SetActive (false);

			}
		}
	}

	void drawPaths ()
	{
		int lrCount = 0;
		int mrCount = 0;
		LineRenderer[] lr = debugDrawParent.GetComponentsInChildren<LineRenderer> ();
		MeshRenderer[] mr = debugDrawParent.GetComponentsInChildren<MeshRenderer> ();

		if (RF.directSound.isValid) {
			lr [lrCount].positionCount = 2;
			lr [lrCount].SetPosition (0, RF.directSound.origin);
			lr [lrCount].SetPosition (1, RF.directSound.destination);
			lr[lrCount].material = directSoundMat;
			lr [lrCount].enabled = true;
			lrCount++;
		}

		foreach (path p in RF.reflections) {

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
				lr [lrCount].material = reflectionMat;
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
		public List<float> lengths;
		public bool isValid;

		public path (Vector3 sourcePos, Vector3 listenerPos)
		{
			origin = sourcePos;
			destination = listenerPos;
			segments = new List<Ray> ();
			lengths = new List<float> ();
			isValid = false;

		}

		public void clear ()
		{
			origin = Vector3.zero;
			destination = Vector3.zero;
			segments.Clear ();
			lengths.Clear ();
			isValid = false;

		}

	}

	private class reflectionFinder
	{
		public Vector3[] simpleDirections = new Vector3[] {
			Vector3.right,
			Vector3.left,
			Vector3.up,
			Vector3.down,
			Vector3.back,
			Vector3.forward
		};

		public Vector3[] basicDirections;

		private GameObject source;
		private GameObject listener;

		public path directSound;

		public List<path> reflections;

		public reflectionFinder (GameObject sourceObject, GameObject listenerObject, int targetNumReflections)
		{
			basicDirections = new Vector3[targetNumReflections];
			generateDirections();

			source = sourceObject;
			listener = listenerObject;

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
			directSound.clear ();

			directSound.origin.Set (source.transform.position.x, source.transform.position.y, source.transform.position.z);
			directSound.destination.Set (listener.transform.position.x, listener.transform.position.y, listener.transform.position.z);
			directSound.segments.Add (new Ray (directSound.origin, directSound.destination - directSound.origin));

			float distance = Vector3.Distance (source.transform.position, listener.transform.position);

			directSound.lengths.Add (distance);
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
				Ray ray = new Ray (source.transform.position, dir);
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit)) {
					if (hit.collider.name != listener.name) {

						Vector3 planeNorm = hit.normal * -1.0f;
						Vector3 imageSource = source.transform.position + (planeNorm * (hit.distance * 2.0f));
						Ray imageSourceRay = new Ray (imageSource, listener.transform.position - imageSource);

						float dot = Vector3.Dot (imageSourceRay.direction, dir);
						float theta = dot / (Mathf.Abs (imageSourceRay.direction.magnitude) * Mathf.Abs (dir.magnitude));
						float altitude = Mathf.Abs ((hit.distance * 2.0f) * Mathf.Sin (theta));

						float len = Mathf.Sqrt (Mathf.Abs (Mathf.Pow (altitude, 2) - Mathf.Pow (hit.distance * 2.0f, 2)));

						Vector3 reflectionPoint = imageSource + (imageSourceRay.direction.normalized * len);
						path currPath = new path (source.transform.position, listener.transform.position);
						currPath.segments.Add (new Ray (source.transform.position, reflectionPoint - source.transform.position));
						currPath.segments.Add (new Ray (reflectionPoint, listener.transform.position - reflectionPoint));
						currPath.lengths.Add (Vector3.Distance (source.transform.position, reflectionPoint));
						currPath.lengths.Add (Vector3.Distance (reflectionPoint, listener.transform.position));
						reflections.Add (currPath);
					}
				}
			}
			validatePaths ();
		}
			
		public void validatePaths ()
		{
			RaycastHit hit;
			foreach (path p in reflections) {
				bool valid = true;
				for (int i = 0; i < p.segments.Count; i++) {
					if (i == p.segments.Count - 1) {
						if (Physics.Raycast (p.segments [i], out hit)) {
							if (hit.collider.name != listener.name) {
								valid = false;
							} 

						} else {
							valid = false;
						}
					} else {
						if (Physics.Raycast (p.segments [i], out hit)) {
							if (hit.collider.name == listener.name) {
								valid = false;
							}
						} else {
							valid = false;
						}
					}

					p.isValid = valid;

				}
			}
		}

		private void generateDirections() {

			int numDirec = basicDirections.Length;

			for (int i = 0; i < numDirec; i++) {

				float phi = Mathf.Acos (-1 + (2 * i) / numDirec);
				float theta = Mathf.Sqrt (numDirec * Mathf.PI) * phi;

				float x = Mathf.Cos (theta) * Mathf.Sin (phi);
				float y = Mathf.Sin (theta) * Mathf.Sin (phi);
				float z = Mathf.Cos (phi);

				basicDirections [i] = new Vector3 (x, y, z);


			}
		}

	}
}
