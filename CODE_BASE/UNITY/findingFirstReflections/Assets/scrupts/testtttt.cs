using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testtttt : MonoBehaviour {

	// Use this for initialization
	void Start () {
		Debug.Log ("Yas");
		gameObject.GetComponent<adjustParams>().setUseSample(8.0f);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
