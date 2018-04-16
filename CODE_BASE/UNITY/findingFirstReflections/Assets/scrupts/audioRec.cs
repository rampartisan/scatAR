using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Threading;


public class audioRec : MonoBehaviour {

	const int HEADER_SIZE = 44;

	struct ClipData{
		public  int samples;
		public  int channels;
		public float[] samplesData;
	}

	public string saveDirectory;
	public string fileNameStem;
	public int maxRecTime;

	public bool recordAudio;
	public bool init = false;
	int sampleRate;
	int sampleCount;
	int channels;
	string fileName;
	float[] bigBuff;

	void Start () {

//		AudioSettings.outputSampleRate = 44100;


		if(!Directory.Exists(saveDirectory)) {
			Directory.CreateDirectory (saveDirectory);
		
		}

		fileName = "NULL";
		sampleCount = 0;
		channels = 2;
		sampleRate = AudioSettings.outputSampleRate;
		bigBuff = new float[sampleRate * maxRecTime];
		if(recordAudio) {
			beginRecord ();
		}

	}
			
	public void beginRecord() {
		Array.Clear (bigBuff, 0, bigBuff.Length);
		sampleCount = 0;
		string timeID = DateTime.Now.ToString ();
		timeID = timeID.Replace (@"/", "");
		timeID = timeID.Replace (" ", "");
		timeID = timeID.Replace (":", "");
		fileName = saveDirectory + fileNameStem + timeID + ".wav";
		init = true;
		recordAudio = true;
	}

	void OnApplicationQuit() {
		if (recordAudio) {
			endRecord ();
		}
	}

	public void endRecord() {
		recordAudio = false;
		saveAudio ();
	}
		
	void OnAudioFilterRead(float[] data, int channels) {
		if (recordAudio && init) {

			if (sampleCount + data.Length > bigBuff.Length) {
				recordAudio = false;
				endRecord ();
				return;
			}

			Array.Copy (data, 0, bigBuff, sampleCount,data.Length);
			sampleCount += data.Length;
		}
				
	}
		
	void saveAudio() {

		if (!fileName.ToLower().EndsWith(".wav")) {
			fileName += ".wav";
		}

		var filepath = fileName;
		Directory.CreateDirectory(Path.GetDirectoryName(filepath));
		ClipData clipdata = new ClipData ();
		clipdata.samples = sampleCount;
		clipdata.channels = channels;


		clipdata.samplesData = new float[sampleCount];
		Array.Copy (bigBuff, 0, clipdata.samplesData, 0, sampleCount);


		using (var fileStream = CreateEmpty(filepath)) {
			MemoryStream memstrm = new MemoryStream ();

			ConvertAndWrite(memstrm, clipdata);

			memstrm.WriteTo (fileStream);
			WriteHeader(fileStream,clipdata.samples,clipdata.channels,sampleRate);
			print ("wrote file");
		}
	}

	FileStream CreateEmpty(string filepath) {
		var fileStream = new FileStream(filepath, FileMode.Create);
		byte emptyByte = new byte();

		for(int i = 0; i < HEADER_SIZE; i++) //preparing the header
		{
			fileStream.WriteByte(emptyByte);
		}

		return fileStream;
	}

	void ConvertAndWrite(MemoryStream memStream, ClipData clipData)
	{
		float[] samples = new float[clipData.samples*clipData.channels];

		samples = clipData.samplesData;

		Int16[] intData = new Int16[samples.Length];

		Byte[] bytesData = new Byte[samples.Length * 2];

		const float rescaleFactor = 32767; //to convert float to Int16

		for (int i = 0; i < samples.Length; i++)
		{
			intData[i] = (short)(samples[i] * rescaleFactor);
			//Debug.Log (samples [i]);
		}
		Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);
		memStream.Write(bytesData, 0, bytesData.Length);
	}

	void WriteHeader(FileStream fileStream, int samples,int channels,int hz) {

		fileStream.Seek(0, SeekOrigin.Begin);

		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		fileStream.Write(riff, 0, 4);

		Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
		fileStream.Write(chunkSize, 0, 4);

		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		fileStream.Write(wave, 0, 4);

		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		fileStream.Write(fmt, 0, 4);

		Byte[] subChunk1 = BitConverter.GetBytes(16);
		fileStream.Write(subChunk1, 0, 4);

		UInt16 two = 2;
		UInt16 one = 1;

		Byte[] audioFormat = BitConverter.GetBytes(one);
		fileStream.Write(audioFormat, 0, 2);

		Byte[] numChannels = BitConverter.GetBytes(channels);
		fileStream.Write(numChannels, 0, 2);

		Byte[] sampleRate = BitConverter.GetBytes(hz);
		fileStream.Write(sampleRate, 0, 4);

		Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
		fileStream.Write(byteRate, 0, 4);

		UInt16 blockAlign = (ushort) (channels * 2);
		fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		fileStream.Write(bitsPerSample, 0, 2);

		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		fileStream.Write(datastring, 0, 4);

		Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
		fileStream.Write(subChunk2, 0, 4);

		//		fileStream.Close();
	}
}
