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

	private string name = "ir";

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
		if (gameObject.GetComponent<generateWGWIR> ().doUpload ()) {
			IR = gameObject.GetComponent<generateWGWIR>().getIR();
			float[] data = new float[IR.samples];
			IR.GetData (data, 0);
			Debug.Log ("Uploading impulse response " + IR.name + " to slot 0 with data length: " + data.Length);
			ConvolutionReverb_UploadSample (0, data, data.Length / IR.channels, IR.channels, IR.frequency, IR.name);
		}
	}
}
