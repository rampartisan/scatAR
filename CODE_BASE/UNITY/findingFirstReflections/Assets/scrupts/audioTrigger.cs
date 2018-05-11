using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioTrigger : MonoBehaviour {
	private GameObject Ball;
	AudioSource audio;
	// Use this for initialization
	void Start () {
		Ball = GameObject.Find("ball");
		audio = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
		if (Ball.GetComponent<ballJump> ().ballHit) {
			audio.Play();
			Ball.GetComponent<ballJump> ().setBallHit (false);
			Debug.Log ("YAS");
		}
	}
}
