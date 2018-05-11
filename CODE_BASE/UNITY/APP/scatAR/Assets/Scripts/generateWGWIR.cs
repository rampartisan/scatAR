using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using UnityEngine;

using System.Text;
using System.IO;
using System;

public class generateWGWIR : MonoBehaviour
{
	public GameObject listener;
	public boundary boundary;

	public static int targetNumReflections = 8;
	public bool update = true;

	public bool enableListen = true;
	public bool doLateReflections = false;

	private reflectionFinder RF;

	private reflectionPath directSound;
	private delayLine directDelay;

	private const float airSpeed = 343.0f;

	private float directAtt = 0.0f;
	private float networkInScale = 0.0f;

	private List<reflectionPath> reflections;
	private List<generateWGWIRnode> network;

	private Queue<float> inSamples;
	private Queue<float> outSamples;
	private float[] impulse;
	private Queue<float> impulseResponse;

	private float inVal = 0.0f;
	private float outVal = 0.0f;
	private float netInVal = 0.0f;
	private float netOutVal = 0.0f;
	private float directVal = 0.0f;

	private float AFin = 0.0f;
	private float AFout = 0.0f;
	private float chanScale = 0.0f;
	private int numSamps;

	private int buffSize;

	private List<reflectionPath> waitingReflections;
	private reflectionPath waitingDirect;

	private int sampleRate;

	private bool haveNewReflections = false;
	private bool doClear = false;

	private Mutex sampleMX;
	private Mutex networkMX;
	private Thread audioProcessThread;
	private bool audioProcessKeepAlive = true;
	private bool scriptInit = false;
	private Thread propagationThread;
	private bool propagationKeepAlive = true;


	public static bool printit = false;
	public static bool p = true;

	private bool generatedNewIR = true;
	private bool triggerUpload = false;

	private bool reflectionsUpdated = true;
	private bool firstUpdate = false;

	public static int filterCase = 0; //0: flat frequency response, 1: wgwFilter, 2: low pass air filter

	void Start ()
	{
		AudioConfiguration AC = AudioSettings.GetConfiguration ();
		sampleRate = AC.sampleRate;
		buffSize = AC.dspBufferSize;

		inSamples = new Queue<float> ();
		outSamples = new Queue<float> ();
		impulseResponse = new Queue<float> ();
		impulse = new float[44100];
		impulse [0] = 0.967f;

		delayLine.sampleRate = sampleRate;

		int initalDirectDelay = delayLine.distanceToDelayTime (Vector3.Distance (gameObject.transform.position, listener.transform.position));
		directDelay = new delayLine (1.0f, initalDirectDelay);

		network = new List<generateWGWIRnode> ();
		RF = gameObject.AddComponent<reflectionFinder> ();
		RF.setListener (listener);

	
		RF.setboundary (boundary);
		RF.setNumInitalDirections (targetNumReflections);
		RF.doUpdate = update;
		RF.onNewReflections += handleNewReflections;
		/*
		generateWGWIRDraw draw = GetComponent<generateWGWIRDraw> ();
		if (draw != null) {
			RF.onNewReflections += draw.updateVisualNetwork;
		}*/

		sampleMX = new Mutex ();
		networkMX = new Mutex ();

		scriptInit = true;
	}	


	void Update ()
	{
		RF.doUpdate = update;
		if (update && haveNewReflections && generatedNewIR) {
			networkMX.WaitOne ();
			processReflections ();
			directDelay.setDelay (delayLine.distanceToDelayTime (directSound.totalDistance ()));
			directAtt = 1.0f / (Vector3.Distance (listener.transform.position, gameObject.transform.position) + 1.0f);
			networkInScale = 1.0f / network.Count;
			haveNewReflections = false;
			networkMX.ReleaseMutex();
			reflectionsUpdated = true;
		}

		if (!firstUpdate && directAtt > 0) {
			audioProcessThread = new Thread (audioProcess);
			audioProcessThread.Start ();
			propagationThread = new Thread (propThread);
			propagationThread.Start ();
			firstUpdate = true;
		}

		checkDelayClear ();
	}

	void OnApplicationQuit() {
		audioProcessKeepAlive = false;
	}

	public void audioProcess() {
		while (audioProcessKeepAlive) {
			convolveIR ();
		}
	}

	public void propThread() {
		while (propagationKeepAlive) {
			if (reflectionsUpdated) {
				reflectionsUpdated = false;
				Debug.Log ("GET IN");
				generatedNewIR = false;

				propagateNetwork ();

				generatedNewIR = true;
				triggerUpload = true;
				Debug.Log ("GET OUT");
			}
		}
	}
		
	public void convolveIR()
	{
		sampleMX.WaitOne ();
		networkMX.WaitOne ();
		int numSamps = inSamples.Count;

		int i;

		for ( i = 0; i < numSamps; i++) {
			outVal = 0.0f;
			inVal = inSamples.Dequeue ();

			//pseudocode
			//out = conv(inVal,impulseResponse);


			//outVal += directVal;
			outSamples.Enqueue (inVal);
		}
		sampleMX.ReleaseMutex ();
		networkMX.ReleaseMutex ();
	}

	public void propagateNetwork() {
		//sampleMX.WaitOne ();
		//networkMX.WaitOne ();
		int numSampsToConsume = impulse.Length;

			int i, j;
			impulseResponse.Clear ();
			for (i = 0; i < numSampsToConsume; i++) {
				netOutVal = 0.0f;
				netInVal = impulse [i];

				directDelay.write (netInVal);
				directVal = directDelay.read ();
				directVal *= directAtt;

				//inVal *= networkInScale;

				for (j = 0; j < network.Count; j++) {
					netOutVal += network [j].getOutgoing ();
					network [j].inputIncoming (netInVal);
					network [j].doScattering (doLateReflections);
				}


				netOutVal += directVal;
			
				impulseResponse.Enqueue (netOutVal);
			}

			//sampleMX.ReleaseMutex ();
			//networkMX.ReleaseMutex ();
	}

	public void doScatteringForNodeRange(int min,int max) {
		for (int i = min; i < max; i++) {
			network[i].doScattering (doLateReflections);
		}
	}

	public void checkDelayClear() {

		if (network != null && doClear) {
			directDelay.clear ();
			for (int i = 0; i < network.Count; i++) {
				generateWGWIRnode n = network [i];
				List<generateWGWIRconnection> connections = n.getConnections ();
				n.clearDelay ();
				for (int j = 0; j < connections.Count; j++) {
					connections [i].clearDelay ();
				}
			}
			doClear = false;
		}

	}

	public Bounds GetMaxBounds (GameObject g)
	{
		Bounds b = new Bounds (g.transform.position, Vector3.zero);
		foreach (MeshRenderer r in g.GetComponentsInChildren<Renderer>()) {
			b.Encapsulate (r.bounds);
		}
		return b;
	}

	public void clearAllDelays ()
	{
		doClear = true;
	}

	public void setNodeLoss (float loss)
	{
		generateWGWIRnode.setLoss (loss);
	}

	public void setNodeSpecularity (float spec)
	{
		generateWGWIRnode.setSpecularity (spec);
	}

	public void setNodeWallAbs (float abs)
	{
		generateWGWIRnode.setWallAbs (abs);
	}

	public void enableAll ()
	{
		enableListen = true;
		doLateReflections = true;
	}

	public void enableER ()
	{
		enableListen = true;
		doLateReflections = false;
	}

	public void disableAll ()
	{
		enableListen = false;
		doLateReflections = false;
	}

	public List<generateWGWIRnode> getNetwork ()
	{
		return network;
	}

	public void setNumRef (int numRef)
	{
		targetNumReflections = numRef;
		RF.setNumInitalDirections (numRef);
	}

	public void setUpdateNetwork (bool val)
	{
		update = val;
		RF.doUpdate = val;
	}

	void OnAudioFilterRead (float[] data, int channels)
	{		
		numSamps = data.Length / channels;
		chanScale = 1.0f / channels;
		int i, c;

		if (scriptInit) {
			sampleMX.WaitOne ();

			for ( i = 0; i < numSamps; i++) {
				AFin = 0.0f;
				for ( c = 0; c < channels; c++) {
					AFin += data [(i * channels) + c];
				}
				inSamples.Enqueue (AFin * chanScale);
			}

			if (!(outSamples.Count < numSamps)) {
				if (!enableListen) {
					for ( i = 0; i < numSamps; i++) {
						AFout = outSamples.Dequeue ();
						for ( c = 0; c < channels; c++) {
							data [(i * channels) + c] *= directAtt;
						}
					}
				} else {
					for ( i = 0; i < numSamps; i++) {
						AFout = outSamples.Dequeue ();
						for ( c = 0; c < channels; c++) {
							data [(i * channels) + c] = AFout;
						}

					}
				}
			} else {
				for ( i = 0; i < numSamps; i++) {
					for ( c = 0; c < channels; c++) {
						data [(i * channels) + c] = 0.0f;
					}
				}
			}
			sampleMX.ReleaseMutex ();

		} else {
			for (i = 0; i < numSamps; i++) {
				for (c = 0; c < channels; c++) {
					data [(i * channels) + c] = 0.0f;
				}
			}
		}
	}

	void handleNewReflections (reflectionPath newDirectSound, List<reflectionPath> newReflections)
	{
		directSound = newDirectSound;
		reflections = newReflections;
		haveNewReflections = true;
	}


	private void processReflections ()
	{

		for (int i = 0; i < network.Count; i++) {

			if (reflections.Count > 0) {

				int minDistIdx = 0;
				float minDist = float.MaxValue;

				for (int j = 0; j < reflections.Count; j++) {

					float dist = Vector3.Distance (reflections [j].segments [1].origin, network [i].getPosition ());

					if (dist < minDist) {
						minDist = dist;
						minDistIdx = j;
					}
				}

				network [i].updatePath (reflections [minDistIdx]);
				reflections.RemoveAt (minDistIdx);

			} else {
				network [i].informConnectionDelete ();
				network.Remove (network [i]);
			}
		}

		if (reflections.Count > 0) {
			for(int i = 0; i < reflections.Count; i++) {
				network.Add (new generateWGWIRnode (reflections[i]));
			}
		}

		for (int i = 0; i < network.Count; i++) {
			for (int j = 1; j < network.Count; j++) {
				int idx = (j + i) % network.Count;
				if (!network [i].containsConnection (network [idx])) {
					network [i].addConnection (network [idx]);
				}
			}/*
			for (int j = 0; j < network.Count-1; j++) {
				network[i].initSampleLists(j, network [i].getConnections ().Count);
			}*/
		}

		/*
		for (int i = 0; i < network.Count; i++) {
			for (int j = 0; j < network.Count; j++) {
				for (int k = 1; k < network.Count; k++) {      
					int idx = (j + k) % network.Count;
					if (j != i && idx != i && idx != j && j < idx) {
						network [i].addSecondConnection (network [j], network [idx]);
					}
				}
			}
		} */

		for (int i = 0; i < network.Count; i++) {
			generateWGWIRnode n = network [i];
			n.findReverseConnections ();
			n.updateConnectionDelay ();
		}

	}
	/*
	public float[] getIR(){
		float[] IRforUpload;
		if (impulseResponse.Count < 1) {
			IRforUpload = impulse;
		} else {
			IRforUpload = impulseResponse.ToArray ();
		}
		SaveInventory ();
		return IRforUpload;
	}*/

	public AudioClip getIR(){
		AudioClip IRforUpload = AudioClip.Create ("IR", impulseResponse.Count, 1, sampleRate, false);

		if (impulseResponse.Count < 1) {
			IRforUpload.SetData (impulse, 0);
		} else {
			IRforUpload.SetData (impulseResponse.ToArray (), 0);
		} 
		return IRforUpload;
	}

	public bool doUpload(){
		if (triggerUpload) {
			triggerUpload = false;
			return true;
		}
		else {
			return false;
		}
	}

	void SaveInventory ()
	{
		string filePath = getPath ();
		float [] IRforWrite = impulseResponse.ToArray ();
		//This is the writer, it writes to the filepath
		StreamWriter writer = new StreamWriter (filePath);

		//This is writing the line of the type, name, damage... etc... (I set these)
		//writer.WriteLine ("Type,Name,Damage/Armor,AttackSpeed,CritChance,CritDamage");
		//This loops through everything in the inventory and sets the file to these.

		for (int i = 0; i < IRforWrite.Length; ++i) {
			writer.WriteLine (IRforWrite[i].ToString () + ",");
		}
		writer.Flush ();
		//This closes the file
		writer.Close ();
	}

	private string getPath ()
	{
		#if UNITY_EDITOR
		return Application.dataPath + "/CSV/" + "irsamp.csv";
		#elif UNITY_ANDROID
		return Application.persistentDataPath+"Saved_Inventory.csv";
		#elif UNITY_IPHONE
		return Application.persistentDataPath+"/"+"Saved_Inventory.csv";
		#else
		return Application.dataPath +"/"+"Saved_Inventory.csv";
		#endif
		}

	public class delayLine
	{
		public static int sampleRate;
		public static float interpVal = 0.1f;
		private int delayTime;
		private int readPtr;
		private int interpReadPtr;
		private float interpFactor;
		private bool doInterp;

		private bool newInterpWaiting;
		private int newInterpReadPtr;

		private int writePtr;

		private int capacity;
		private float[] buffer;

		private float outSamp;
		public delayLine (float maxTimeInSeconds, int newDelayTime)
		{
			capacity = (int)(sampleRate * maxTimeInSeconds);
			buffer = new float[capacity];

			writePtr = 0;
			interpFactor = 0.0f;
			doInterp = false;
			newInterpWaiting = false;

			if (newDelayTime > capacity) {
				print (string.Format ("delay time of {0} for buffer size of {1} is not possible", newDelayTime, capacity));
				newDelayTime = capacity - 1;
			}
			if (newDelayTime < 0) {
				newDelayTime = 1;
			}
			readPtr = writePtr - newDelayTime;

			if (readPtr < 0) {
				readPtr += capacity;
			}

			delayTime = newDelayTime;
		}

		public void clear ()
		{
			System.Array.Clear (buffer, 0, buffer.Length);
		}

		public int getDelayTime ()
		{
			return delayTime;
		}

		public void setDelay (int newDelayTime)
		{

			if (newDelayTime > capacity) {
				print (string.Format ("delay time of {0} for buffer size of {1} is not possible", newDelayTime, capacity));
				newDelayTime = capacity - 1;
			}

			if (newDelayTime <= 2) {
				newDelayTime = 3;
			}

			if (doInterp) {
				newInterpWaiting = true;
				if (newDelayTime != delayTime) {
					newInterpReadPtr = writePtr - newDelayTime;
					if (newInterpReadPtr < 0) {
						newInterpReadPtr += capacity;
					}
					delayTime = newDelayTime;
				}

			} else {

				if (newDelayTime != delayTime) {
					interpReadPtr = writePtr - newDelayTime;
					if (interpReadPtr < 0) {
						interpReadPtr += capacity;
					}
					delayTime = newDelayTime;
					doInterp = true;
					interpFactor = 0.0f;
				}
			}
		}

		public void write (float sample)
		{
			buffer [writePtr] = sample;
			writePtr++;
			writePtr %= capacity;
		}

		public float read ()
		{

			if (newInterpWaiting && !doInterp) {
				doInterp = true;
				newInterpWaiting = false;
				interpReadPtr = newInterpReadPtr;
				interpVal = 0.0f;
			}

			if (doInterp) {

				outSamp = (buffer [readPtr] * (1.0f - interpFactor)) + (buffer [interpReadPtr] * interpFactor);

				interpReadPtr++;
				interpReadPtr %= capacity;
				interpFactor += interpVal;

				if (interpFactor > 1.0f) {
					readPtr = interpReadPtr;
					interpFactor = 0.0f;
					doInterp = false;
				}

			} else {
				outSamp = buffer [readPtr];
			}

			readPtr++;
			readPtr %= capacity;
			
			return outSamp;
		}

		public static int distanceToDelayTime (float distance)
		{
			return Mathf.CeilToInt ((distance * sampleRate) /airSpeed);
		}
	}

	public class generateWGWIRnode
	{
		public static float specularity = 0.5f;
		public static float wallAbsCoeff = 0.97f;
		public static float nodeLoss = 1.0f - wallAbsCoeff;
		//		public static float nodeLoss = 0.0f;

		private Vector3 position;
		private List<generateWGWIRconnection> connections;
		private List<generateWGWIRconnection> secondConnections;
		private reflectionPath nodePath;

		private delayLine incoming;
		private delayLine outgoing;

		private BiQuadFilter wallFilter;
		private List<BiQuadFilter> interFilters;
		private List<BiQuadFilter> secondInFilters;
		private List<BiQuadFilter> secondOutFilters;

		private float scatteringFactor;
		private float scatteringFactorDiag;

		private float FOSample = 0.0f;
		private float HalfFOSample = 0.0f;
		private float outgoingSample = 0.0f;
		private float incomingAttFactor = 0.0f;
		private float outgoingAttFactor = 0.0f;

		private List<float> interNodeAttFactor;
		private float attenuationMultiplier = 0.0f;

		//	private float initialSOsample = 0.0f;
		private List<float> initialSOsamples;
		private List<float> SOsamples;
		private List<float> scatterSOsamples;
		private float SOsum;
		//	private float onlyEarlyReflections = 0.0f;
		private float zero = 0.0f;
		private List<float> posSums;


		private int numConnections = 0;

		public generateWGWIRnode (reflectionPath thePath)
		{
			switch (filterCase)
			{
			case 0:
				wallFilter = BiQuadFilter.flatResponse (delayLine.sampleRate);
				break;
			case 1:
				wallFilter = BiQuadFilter.wgwFilter (delayLine.sampleRate);
				break;
			case 2:
				wallFilter = BiQuadFilter.highPassAirFilter (delayLine.sampleRate);
				break;
				//Debug.Log (interFilters.Count);
			}
			interFilters = new List<BiQuadFilter>();
			secondInFilters = new List<BiQuadFilter>();
			secondOutFilters = new List<BiQuadFilter>();
			//wallFilter = BiQuadFilter.flatResponse (delayLine.sampleRate);
			connections = new List<generateWGWIRconnection> ();
			secondConnections = new List<generateWGWIRconnection> ();
			nodePath = thePath;
			position = nodePath.segments [1].origin;
			incoming = new delayLine (1.0f, delayLine.distanceToDelayTime (nodePath.lengths [0]));
			outgoing = new delayLine (1.0f, delayLine.distanceToDelayTime (nodePath.lengths [1]));
			//set secondorder delay line here

			incomingAttFactor = 1.0f / (nodePath.lengths [0] + 1.0f);
			outgoingAttFactor = 1.0f / (nodePath.lengths [1] + 1.0f);
			SOsamples = new List<float>();
			scatterSOsamples = new List<float>();
			initialSOsamples = new List<float>();
			interNodeAttFactor = new List<float>();
			posSums = new List<float>();
		}

		public void updatePath (reflectionPath thePath)
		{
			nodePath = thePath;
			position = thePath.segments [1].origin;
			incoming.setDelay (delayLine.distanceToDelayTime (nodePath.lengths [0]));
			outgoing.setDelay (delayLine.distanceToDelayTime (nodePath.lengths [1]));
			incomingAttFactor = 1.0f / (nodePath.lengths [0] + 1.0f);
			outgoingAttFactor = 1.0f / (nodePath.lengths [1] + 1.0f);
		}

		public void clearDelay ()
		{
			incoming.clear ();
			outgoing.clear ();
		}

		public static void setLoss (float loss)
		{
			nodeLoss = loss;
		}

		public static void setSpecularity (float spec)
		{
			specularity = spec;
		}

		public static void setWallAbs (float abs)
		{
			wallAbsCoeff = abs;
		}

		public void inputIncoming (float sample)
		{
			incoming.write (sample);
		}

		public float getOutgoing ()
		{
			return outgoing.read (); 
		}

		public float getSourceNodeDistance()
		{
			return nodePath.lengths [0];
		}

		//commit testing
		public void doScattering (bool outputLateReflections)
		{
			FOSample = incoming.read ();

			//if (incoming.read() > 0)
				//Debug.Log (incoming.read());

			FOSample *= incomingAttFactor;

			FOSample = wallFilter.Transform (FOSample);
			FOSample *= wallAbsCoeff;
			HalfFOSample = 0.5f * FOSample;
			/*
			initialSOsample = FOSample;
			initialSOsample *= incomingAttFactor * 0.5f;
			initialSOsample = wallFilter.Transform (initialSOsample);
			initialSOsample *= wallAbsCoeff;
			*/


			//outgoingSample = initialSOsample;

			int i ,j;

			for (i = 0; i < numConnections; i++) {
				initialSOsamples[i] = FOSample;
				initialSOsamples[i] *=  0.5f;
				initialSOsamples[i] = secondInFilters[i].Transform (initialSOsamples[i]);
				initialSOsamples[i] *= wallAbsCoeff;

				outgoingSample += initialSOsamples [i];

				SOsamples [i] = connections [i].getSampleFromReverseConnection ();

				for (j = 0; j < numConnections; j++) { 
					//connections [i].posSamp [j] = connections [i].getScatterSampleFromReverseConnection (j);
					//posSums [i] += connections [i].posSamp [j];

					int id = j * numConnections;

					for (int k = 0; k < numConnections; k++) {
						scatterSOsamples [id+k] = SOsamples [k];
						/*
						if(printit)
						Debug.Log ("before filtering:  " + scatterSOsamples [id + k]);
						*/
						scatterSOsamples [id+k] = interFilters[id+k].Transform(scatterSOsamples [id+k]);
						/*
						if(printit)
							Debug.Log ("after filtering:  " + scatterSOsamples [id + k]);
							*/
					}
				}

				SOsamples [i] = secondOutFilters[i].Transform(SOsamples[i]);
				SOsum += SOsamples [i] * interNodeAttFactor [i];
				SOsum *= attenuationMultiplier;
			}
			/*
			for (i = 0; i < numConnections; i++) {
				
			

			}*/
			/*	
			for (i = 0; i < numConnections; i++) {
				connections [i].FOnegSamp -= connections [i].FOprevSample;
				connections [i].inputToDelay (connections [i].FOnegSamp);
				connections [i].FOprevSample = connections [i].FOnegSamp;
			}*/


			for (i = 0; i < numConnections; i++) {
				//connections [i].FOnegSamp += initialSOsample; //+ posSums[i]; //put filter
				outgoingSample += SOsum;
				outgoingSample += connections [i].prevSample;
				connections [i].negSamp += initialSOsamples[i];	

				for (j = 0; j < numConnections; j++) {

					////Scattering factors might not be correct. needs to be reworked.
					if (i == j) {
						connections [i].negSamp += scatterSOsamples[j+(i*numConnections)] * scatteringFactorDiag;
					} else {
						connections [i].negSamp += scatterSOsamples[j+(i*numConnections)] * scatteringFactor;
					}
				}
				//connections [i].negSamp = connections [i].connectFilter.Transform (connections [i].negSamp);	
				connections [i].negSamp -= connections [i].prevSample;
				connections [i].inputToDelay (connections [i].negSamp);
				connections [i].prevSample = connections [i].negSamp;
				//if (printit)
				//Debug.Log (connections [i].FOnegSamp);
				//Debug.Log ("pos:  " + connections [i].posSamp.Count + "  neg:  " + connections [i].negSamp.Count + "  prev:  " + connections [i].prevSamp.Count + " sds:  " + connections [i].scatterDelays.Count);

				//outgoingSample += connections [i].posSamp;
				//outgoingSample += connections [i].prevSample;





				/*
				for (j = 0; j < SOsamples.Count; j++){
					//Debug.Log (connections [i].negSamp [j]);// += SOsamples[j]; // this will crash unity. but used for debugging.
					connections [i].negSamp [j] += SOsamples[j];
			 //UNCOMMENT SECTION FOR NETWORK ACTION 
					for (int k = 0; k < numConnections; k++) {
					if (i == j) {
							connections [i].negSamp[j] += connections [j].posSamp[k] * scatteringFactorDiag;
					} else {
							connections [i].negSamp[j] += connections [j].posSamp[k] * scatteringFactor;
					}
				}

				//				connections [i].negSamp = connections [i].connectFilter.Transform (connections [i].negSamp); //NEEDS FILTERING?¨

					connections [i].negSamp[j] -= connections [i].prevSamp[j];
					connections [i].scatterInputToDelay (connections [i].negSamp[j],j);
					connections [i].prevSamp[j] = connections [i].negSamp[j];
				}*/

			}


			//FIX OUTGOING SAMPLE ! SOSUM + SCATTERING OUTPUT///
			if (outputLateReflections) {
				outgoingSample += FOSample;
				outgoingSample *= outgoingAttFactor;
				outgoing.write (outgoingSample);

				SOsum = 0.0f;
			} else {
				/*
				onlyEarlyReflections = FOSample + SOsum;
				onlyEarlyReflections *= outgoingAttFactor;
				outgoing.write (onlyEarlyReflections);*/
				FOSample *= outgoingAttFactor;
				outgoing.write (FOSample);
				SOsum = 0.0f;
			}
		}

		//		public void doScattering (bool outputLateReflections)
		//		{
		//			FOSample = incoming.read ();
		//			FOSample *= incomingAttFactor;
		//			FOSampleNoFilt = 0.5f * FOSample;
		//			FOSample = wallFilter.Transform (FOSample);
		//			FOSample *= wallAbsCoeff;
		//			HalfFOSample = 0.5f * FOSample;
		//			outgoingSample = FOSample;
		//
		//			int i ,j;
		//
		//			for (i = 0; i < numConnections; i++) {
		//				connections [i].posSamp = connections [i].getSampleFromReverseConnection ();
		//
		//				SOsamples [i] = connections [i].getSampleFromReverseConnection ();
		//
		//			}
		//
		//			for (i = 0; i < numConnections; i++) {
		//				
		//				outgoingSample += connections [i].posSamp;
		//				outgoingSample += connections [i].prevSample;
		//				connections [i].negSamp += (HalfFOSample);
		//
		//				for (j = 0; j < numConnections; j++) {
		//					if (i == j) {
		//						connections [i].negSamp += connections [j].posSamp * scatteringFactorDiag;
		//					} else {
		//						connections [i].negSamp += connections [j].posSamp * scatteringFactor;
		//					}
		//				}
		//
		//				//				connections [i].negSamp = connections [i].connectFilter.Transform (connections [i].negSamp);
		//				connections [i].negSamp -= connections [i].prevSample;
		//				connections [i].inputToDelay (connections [i].negSamp);
		//				connections [i].prevSample = connections [i].negSamp;
		//
		//			}
		//
		//			if (outputLateReflections) {
		//				outgoingSample *= outgoingAttFactor;
		//				outgoing.write (outgoingSample);
		//			} else {
		//				FOSample *= outgoingAttFactor;
		//				outgoing.write (FOSample);
		//			}
		//		}


		public void updateScatteringFactor ()
		{
			int minCon = connections.Count + 1;

			scatteringFactor = (2.0f / minCon) - nodeLoss;
			scatteringFactorDiag = ((2.0f - minCon) / minCon) - -nodeLoss;

			//update 
			for (int i = 0; i < numConnections; i++) {
				interNodeAttFactor[i] = 1.0f / (1.0f + (nodePath.lengths [1] + connections [i].getLength ()) / connections [i].getTargetDistanceToSource ());
			}
			attenuationMultiplier = 2.0f / (numConnections - 1);

			//Debug.Log (connections [numConnections-1].getLength());
		}

		public void updateConnectionDelay ()
		{
			for (int i = 0; i < connections.Count; i++) {
				connections [i].updateDelayLength ();
			}
		}

		public void setInternodeGains()
		{

		}

		public List<generateWGWIRconnection> getConnections ()
		{
			return connections;
		}

		public void addConnection (generateWGWIRnode n)
		{
			connections.Add (new generateWGWIRconnection (this, n));
			numConnections = connections.Count;

			SOsamples.Add(zero);
			initialSOsamples.Add(zero);
			interNodeAttFactor.Add (zero);
			posSums.Add (zero);

			updateScatteringFactor ();

			switch (filterCase)
			{
			case 0:
				secondInFilters.Add(BiQuadFilter.flatResponse(delayLine.sampleRate));
				secondOutFilters.Add(BiQuadFilter.flatResponse(delayLine.sampleRate));
				break;
			case 1:
				secondInFilters.Add(BiQuadFilter.wgwFilter(delayLine.sampleRate));
				secondOutFilters.Add(BiQuadFilter.wgwFilter(delayLine.sampleRate));
				break;
			case 2:
				secondInFilters.Add(BiQuadFilter.highPassAirFilter(delayLine.sampleRate));
				secondOutFilters.Add(BiQuadFilter.highPassAirFilter(delayLine.sampleRate));
				break;
				//Debug.Log (interFilters.Count);
			}

			for(int i = 0; i < (numConnections + numConnections - 1); i++)
			{
				scatterSOsamples.Add(zero);

				switch (filterCase)
				{
				case 0:
					interFilters.Add (BiQuadFilter.flatResponse (delayLine.sampleRate));
					break;
				case 1:
					interFilters.Add(BiQuadFilter.wgwFilter(delayLine.sampleRate));
					break;
				case 2:
					interFilters.Add(BiQuadFilter.highPassAirFilter(delayLine.sampleRate));
					break;
					//Debug.Log (interFilters.Count);
				}
			}
		}

		public void initSampleLists(int i, int numC)
		{
			//Debug.Log (i + "      " + numC);
			//connections [i].updateSampleLists (numC); //fill list of samples for each connection. Each connection needs N-1 amount of samples
			//Debug.Log ("pos:  " + connections [i].posSamp.Count + "  neg:  " + connections [i].negSamp.Count + "  prev:  " + connections [i].prevSamp.Count + " sds:  " + connections [i].scatterDelays.Count);
		}
		/*
		public void addSecondConnection (WGWnode m, WGWnode n)
		{
			secondConnections.Add (new WGWconnection (m, n));
			updateScatteringFactor ();
			numSecondConnections = secondConnections.Count;
		}*/

		public void findReverseConnections ()
		{
			for (int i = 0; i < connections.Count; i++) {
				generateWGWIRconnection theTarget = connections [i].getTarget ().connections.Find (item => item.getTarget () == this);
				connections [i].setReverseConnection (ref theTarget);
			}
		}
		/*
		public void findSecondReverseConnections ()
		{
			for (int i = 0; i < secondConnections.Count; i++) {
				WGWconnection theTarget = secondConnections [i].getTarget ().secondConnections.Find (item => item.getTarget () == this);
				secondConnections [i].setReverseConnection (ref theTarget);
			}
		}*/

		public void informConnectionDelete ()
		{
			for (int i = 0; i < connections.Count; i++) {
				generateWGWIRnode nodeToInform = connections [i].getTarget ();
				int idx = nodeToInform.connections.FindIndex (item => item.getTarget () == this);
				nodeToInform.connections.RemoveAt (idx);
				nodeToInform.numConnections = nodeToInform.connections.Count;
				nodeToInform.updateScatteringFactor ();

			}
		}

		public bool containsConnection (generateWGWIRnode n)
		{
			for (int i = 0; i < connections.Count; i++) {
				if (connections [i].getTarget ().Equals (n)) {
					return true;
				}
			}
			return false;
		}

		//		public bool containsSecondConnection (WGWnode n)
		//		{
		//			for (int i = 0; i < secondConnections.Count; i++) {
		//				if (secondConnections [i].getTarget ().Equals (n)) {
		//					return true;
		//				}
		//			}
		//			return false;
		//		}

		public Vector3 getPosition ()
		{
			return position;
		}

		public int getIncomingDelayTime ()
		{
			return incoming.getDelayTime ();
		}

		public int getOutgoingDelayTime ()
		{
			return outgoing.getDelayTime ();
		}

		public int getTotalDelayTime ()
		{
			return outgoing.getDelayTime () + incoming.getDelayTime ();
		}

		public reflectionPath getPath ()
		{
			return nodePath;
		}
	}

	public class generateWGWIRconnection
	{
		//public List<float> posSamp;
		//public List<float> negSamp;
		//public List<float> prevSamp;
		//public List<delayLine> scatterDelays;
		public float posSamp = 0.0f;
		public float negSamp = 0.0f;
		public float prevSample = 0.0f;

		private generateWGWIRnode parent;
		private generateWGWIRnode target;
		private float length;
		private float distanceToSource;
		private delayLine delay;
		private generateWGWIRconnection reverseConnection;
		public BiQuadFilter connectFilter;
		//private float zero = 0.0f;

		public generateWGWIRconnection (generateWGWIRnode theParent, generateWGWIRnode theTarget)
		{
			//posSamp = new List<float>();
			//negSamp = new List<float>();
			//prevSamp = new List<float>();
			//scatterDelays = new List<delayLine>();
			//connectFilter = BiQuadFilter.highPassAirFilter (delayLine.sampleRate);
			//connectFilter = BiQuadFilter.flatResponse (delayLine.sampleRate);

			parent = theParent;
			target = theTarget;
			length = Vector3.Distance (parent.getPosition (), target.getPosition ());
			delay = new delayLine (1.0f, delayLine.distanceToDelayTime (length));
			distanceToSource = theTarget.getSourceNodeDistance();
		}

		public void clearDelay ()
		{
			delay.clear ();
			/*
			for (int i = 0; i < scatterDelays.Count; i++) {
				scatterDelays [i].clear ();
			}*/
		}

		public void setTarget (generateWGWIRnode n)
		{
			target = n;
			updateDelayLength ();
		}

		public int getDelayTime ()
		{
			return delay.getDelayTime ();
		}

		public void inputToDelay (float sample)
		{
			delay.write (sample);
		}

		public float readFromDelay ()
		{
			return delay.read ();
		}

		public float getSampleFromReverseConnection ()
		{
			if (reverseConnection != null) {
				return reverseConnection.readFromDelay ();
			} else {					
				return 0.0f;
			}
		}
		/*
		public int getScatterDelayTime (int index)
		{
			return scatterDelays[index].getDelayTime ();
		}

		public void scatterInputToDelay (float sample, int index)
		{
			scatterDelays[index].write (sample);
		}

		public float readFromScatterDelays (int index)
		{
			return scatterDelays[index].read ();
		}

		public float getScatterSampleFromReverseConnection (int index)
		{
			if (reverseConnection != null) {
				return reverseConnection.readFromScatterDelays (index);
			} else {					
				return 0.0f;
			}
		}*/

		public generateWGWIRnode getTarget ()
		{
			return target;
		}

		public generateWGWIRnode getParent ()
		{
			return parent;
		}

		public float getLength ()
		{
			return length;
		}

		public float getTargetDistanceToSource ()
		{
			return distanceToSource;
		}

		public void setReverseConnection (ref generateWGWIRconnection c)
		{
			reverseConnection = c;
		}

		public void updateDelayLength ()
		{
			length = Vector3.Distance (parent.getPosition (), target.getPosition ());
			delay.setDelay (delayLine.distanceToDelayTime (length));
			/*
			for (int i = 0; i < scatterDelays.Count; i++) {
				scatterDelays[i].setDelay(delayLine.distanceToDelayTime (length));
			}*/
		}
		/*
		public void updateSampleLists (int numConnections)
		{
			//posSamp.Clear();
			if (posSamp.Count < numConnections) {
				for (int i = 0; i < numConnections; i++) {
					posSamp.Add (zero);
					negSamp.Add (zero);
					prevSamp.Add (zero);
					scatterDelays.Add (new delayLine (1.0f, delayLine.distanceToDelayTime (length)));
					//Debug.Log ("pos:  " + posSamp.Count + "  neg:  " + negSamp.Count + "  prev:  " + prevSamp.Count + " sds:  " + scatterDelays.Count);
				}
			}
		}*/
	}
}




