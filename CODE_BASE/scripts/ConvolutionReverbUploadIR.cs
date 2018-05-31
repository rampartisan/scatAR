using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public class ConvolutionReverbUploadIR : MonoBehaviour
{
    [DllImport("AudioPluginConvolution")]
    private static extern bool ConvolutionReverb_UploadSample(int index, float[] data, int numsamples, int numchannels, int samplerate, [MarshalAs(UnmanagedType.LPStr)] string name);

    public AudioClip[] impulse = new AudioClip[0];
    public int index;

    private bool[] uploaded = new bool [64];
    private AudioClip[] currImpulse = new AudioClip [64];

	private AudioClip IR;
    void Start()
    {
        UploadChangedClips();
    }

    void Update()
    {
        UploadChangedClips();
    }
    
    void UploadChangedClips()
    {
		if (!gameObject.GetComponent<generateWGWIR> ().doUpload ()) {
			int currindex = index;
			foreach (var s in impulse) {
				if (currImpulse [currindex] != s)
					uploaded [currindex] = false;

				if (s != null && s.loadState == AudioDataLoadState.Loaded && !uploaded [currindex]) {
					Debug.Log ("Uploading impulse response " + s.name + " to slot " + currindex);
					float[] data = new float[s.samples];
					s.GetData (data, 0);
					ConvolutionReverb_UploadSample (currindex, data, data.Length / s.channels, s.channels, s.frequency, s.name);
					Debug.Log (currindex + "   " + data + "   " + data.Length + "   " + s.channels + "   " + s.frequency + "   " + s.name);
					uploaded [currindex] = true;
					currImpulse [currindex] = s;
				}

				currindex++;
			}
		} else {
			IR = gameObject.GetComponent<generateWGWIR>().getIR();
			float[] data = new float[IR.samples];
			IR.GetData (data, 0);
			Debug.Log ("Uploading impulse response " + IR.name + " to slot 1 with data length: " + data.Length + "     " + IR.frequency);
			ConvolutionReverb_UploadSample (2, data, data.Length / IR.channels, IR.channels, IR.frequency, IR.name);
		}
	}
}