using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SDNDraw : MonoBehaviour {

	// Materials for drawing the different parts of the network
	public Material directSoundMat;
	public Material reflectionMat;
	public Material junctionMat;

	public bool drawNetwork = false;
	public bool showDistances = false;

	// SDN component and emtpy game object to store all drawing objects in
	private SDN targetNetwork;
	private GameObject drawParent;

	void Start () {
		targetNetwork = gameObject.GetComponent<SDN> ();
		drawParent = new GameObject ("debugDrawParent");
		drawParent.transform.SetParent (gameObject.transform);
		createVisualNetwork ();
	}

	void Update() {
		TextMesh[] tm = drawParent.GetComponentsInChildren<TextMesh> ();
		for (int i = 0; i < tm.Length; i++) {
			tm [i].transform.rotation =  Quaternion.FromToRotation(Vector3.forward, (transform.position - targetNetwork.listener.transform.position).normalized);
		}
		setDraw (drawNetwork);
	}
		
	public void setDraw(bool newSet) {
		drawNetwork = newSet;

		if (drawNetwork) {
			drawParent.SetActive (true);
		} else {
			drawParent.SetActive (false);
		}
	}

	public void createVisualNetwork() {

		foreach (Transform child in drawParent.transform) {
			GameObject.Destroy(child.gameObject);
		}


		for (int i = 0; i < SDN.targetNumReflections + 1; i++) {
			GameObject path = new GameObject ("Path" + i.ToString ());
			path.transform.SetParent (drawParent.transform);
			LineRenderer lr = path.AddComponent<LineRenderer> ();
			lr.enabled = false;
			lr.startWidth = 0.02f;
			lr.endWidth = 0.02f;
			lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			lr.receiveShadows = false;
			lr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

			GameObject label = new GameObject ("label" + i.ToString ());
			label.transform.SetParent (drawParent.transform);
			TextMesh tm = label.AddComponent<TextMesh> ();
			tm.characterSize = 0.02f;
			tm.fontSize = 150;
			tm.text = "hello world";
			tm.anchor = TextAnchor.MiddleCenter;
			tm.GetComponent<MeshRenderer> ().enabled = false;


			if (i > 0) {
				GameObject junction = GameObject.CreatePrimitive (PrimitiveType.Cube);
				junction.transform.localScale  = new Vector3(0.12f, 0.12f, 0.12f);

				MeshRenderer mr = junction.GetComponent<MeshRenderer> ();
				mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
				mr.receiveShadows = false;
				mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
				mr.enabled = false;
				Destroy (junction.GetComponent<BoxCollider> ());				
				junction.transform.SetParent (drawParent.transform);
				junction.name = "Junction" + i.ToString ();
				mr.material = junctionMat;


			}
		}
	}
		
	public void updateVisualNetwork (reflectionPath newDS,List<reflectionPath> newR)
	{

		reflectionPath directSound = newDS;
		List<reflectionPath> reflections = newR;

		int lrCount = 0;
		int mrCount = 0;
		int tmcount = 0;

		LineRenderer[] lr = drawParent.GetComponentsInChildren<LineRenderer> ();
		MeshFilter[] mf = drawParent.GetComponentsInChildren<MeshFilter> ();
		TextMesh[] tm = drawParent.GetComponentsInChildren<TextMesh> ();

		if (directSound.isValid) {
			lr [lrCount].positionCount = 2;
			lr [lrCount].SetPosition (0, directSound.origin);
			lr [lrCount].SetPosition (1, directSound.destination);
			lr[lrCount].material = directSoundMat;
			lr [lrCount].enabled = true;
			lrCount++;
			if (showDistances) {
				tm [tmcount].text = directSound.totalDistance ().ToString ();
				tm [tmcount].transform.position = directSound.segments [0].origin + (directSound.segments [0].direction * (directSound.totalDistance () / 2.0f));
				tm [tmcount].GetComponent<MeshRenderer> ().enabled = true;
				tmcount++;
			}
		}

		for(int j = 0; j < reflections.Count;j++){
			reflectionPath p = reflections[j];
			if (p.isValid) {

				if (showDistances) {
					tm [tmcount].text = p.totalDistance ().ToString ();
					tm [tmcount].transform.position = p.segments [1].origin;
					tm [tmcount].
					GetComponent<MeshRenderer> ().enabled = true;
					tmcount++;
				}

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

				mf [mrCount].gameObject.transform.position = p.segments [1].origin;
				mf [mrCount].gameObject.GetComponent<MeshRenderer> ().enabled = true;
				mrCount++;
			}
		}

		for (int i = lrCount; i < lr.Length; i++) {
			lr [i].enabled = false;
		}
		for (int i = mrCount; i < mf.Length; i++) {
			mf [i].gameObject.GetComponent<MeshRenderer> ().enabled = false;
		}
		for (int i = tmcount; i < tm.Length; i++) {
			tm [i].gameObject.GetComponent<MeshRenderer> ().enabled = false;
		}


	}

}
