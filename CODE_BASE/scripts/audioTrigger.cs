using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class audioTrigger : MonoBehaviour {
	private GameObject Ball;
	private GameObject mainCam;
	AudioSource audioS;
	public AudioClip [] clips = new AudioClip[14];

	private bool playOne = false;
	private bool reset = true;
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
				/*
				audioS.Play ();
				audioS.loop = true;
*/
				audioS.enabled = false;
				audioS.enabled = true;
				audioS.PlayOneShot (clips [mainCam.GetComponent<testControl> ().getSoundID ()]);
				playOne = false;
			}//audioS.loop = true;
		} else if (mainCam.GetComponent<testControl> ().getSoundType () == 1) {
			audioS.loop = false;
			if (playOne) {
				if (reset) {
					audioS.enabled = false;
					audioS.enabled = true;
					reset = false;
				}
			
				if (Ball.GetComponent<ballJump> ().ballHit) {
					audioS.PlayOneShot (clips [mainCam.GetComponent<testControl> ().getSoundID ()]);
					Ball.GetComponent<ballJump> ().setBallHit (false);
				}
			}
		}

	}


	public void setAudioPlay(bool playOnce){
		playOne = playOnce;
	}
}
