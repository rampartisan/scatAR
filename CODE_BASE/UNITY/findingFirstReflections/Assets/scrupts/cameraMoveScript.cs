using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraMoveScript : MonoBehaviour {
	public Camera cam1;
	public Camera cam2;

	private float camSpeed = 0.75f;
	// Use this for initialization
	//private bool wentStraight = false;
	//private bool wentRight = false;
	//private bool wentBack = false;
	//private bool wentLeft = false;

	public bool camTurnDone = false;
	private bool doOnce = true;
	private GameObject mainCam;

	void Start () {
			mainCam = GameObject.Find("MainCamera");
			cam1.enabled = true;
			cam2.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		
		if (Time.realtimeSinceStartup < 21.5f) {
			transform.Translate (Vector3.forward * camSpeed * Time.deltaTime);
			transform.Rotate (Vector3.up * 20 * Time.deltaTime);
		} else if (Time.realtimeSinceStartup > 25.0f){
			cam1.enabled = false;
			cam2.enabled = true;
			camTurnDone = true;
			if (doOnce) {
				mainCam.GetComponent<testControl> ().setScene (true);
				doOnce = false;
			}
		}
		/*
		if (gameObject.transform.position.x > -3f && gameObject.transform.position.z >= -2f && !wentStraight) {
			transform.Translate (Vector3.forward * camSpeed * Time.deltaTime);
		}
		else if (gameObject.transform.position.x < -3f && gameObject.transform.position.z < 2f && !wentRight) {
			transform.Translate (Vector3.right * camSpeed * Time.deltaTime);
			wentStraight = true;
		} else if (gameObject.transform.position.x < 3f && gameObject.transform.position.z > 2f && !wentBack) {
			transform.Translate (Vector3.back * camSpeed * Time.deltaTime);
			wentRight = true;
		} else if (gameObject.transform.position.x > 3f && gameObject.transform.position.z > -2f && !wentLeft) {
			transform.Translate (Vector3.left * camSpeed * Time.deltaTime);
			wentBack = true;
		}*/
	}
}
