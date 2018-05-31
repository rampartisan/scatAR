using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class adjustParams : MonoBehaviour {

	public AudioMixer masterMixer;

	public void setWetLevel(float wetLevel){
		masterMixer.SetFloat ("convWet", wetLevel);
	}

	public void setGainLevel(float gainLevel){
		masterMixer.SetFloat ("convGain", gainLevel);
	}

	public void setUseSample(float sample){
		masterMixer.SetFloat ("convUseSample", sample);
	}


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
