using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioTrigger : MonoBehaviour {
	private GameObject Ball;
	private GameObject mainCam;
	AudioSource audioS;
	public AudioClip clip1;
	public AudioClip clip2;

	private bool playOne = false;
	// Use this for initialization
	void Start () {
		Ball = GameObject.Find("ball");
		mainCam = GameObject.Find("MainCamera");
		audioS = GetComponent<AudioSource>();
	}

	// Update is called once per frame
	void Update () {

		if (mainCam.GetComponent<testControl> ().getSoundType () == 0) {
			if (playOne) {
				audioS.Play ();
				audioS.loop = true;
				playOne = false;
				Debug.Log ("NEXT");
			}//audioS.loop = true;
		}
		else if (mainCam.GetComponent<testControl> ().getSoundType () == 1) {
			audioS.loop = false;
			if (Ball.GetComponent<ballJump> ().ballHit) {
				audioS.PlayOneShot (clip1);
				Ball.GetComponent<ballJump> ().setBallHit (false);
			}
		}
	}


	public void setAudioPlay(bool playOnce){
		playOne = playOnce;
	}
}
