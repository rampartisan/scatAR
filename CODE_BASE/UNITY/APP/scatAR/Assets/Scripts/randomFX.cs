using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class randomFX : MonoBehaviour {
	public AudioClip[] fxClips;
	private int numClip;

	private float[][] samples;

	public float volScale = 0.1f;

	private int currClipPtr;
	private int currClipLength;
	private int currClipIdx;

	private bool playingClip = false;

	void Start () {
		numClip = fxClips.Length;
		samples = new float[numClip][];

		for(int i = 0; i < numClip; i++) {
			samples [i] = new float[fxClips [i].samples];
			fxClips [i].GetData (samples [i], 0);
		}

	}

	void Update () {
		if (!playingClip) {
			if (Random.Range (0, 40) == 0) {
				getNewClip ();
			}
		}
	}
	private void getNewClip() {
		currClipPtr = 0;
		currClipIdx = Random.Range (0, numClip);
		currClipLength = samples [currClipIdx].Length;
		playingClip = true;
	}


	void OnAudioFilterRead (float[] data, int channels){
		int numSamps = data.Length / channels;
		if (playingClip) {

			int numSampsToConsume = Mathf.Min (numSamps, currClipLength - currClipPtr);

			for (int i = 0; i < numSampsToConsume; i++) {
				for (int c = 0; c < channels; c++) {
					data [(i * channels) + c] += samples [currClipIdx] [currClipPtr] * volScale;
				}
				currClipPtr++;
			}

			if (currClipPtr >= currClipLength) {
				playingClip = false;
			}

		}

	}
}
