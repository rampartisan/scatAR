using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using UnityEngine;

public class WGW : MonoBehaviour
{
	public GameObject listener;
	public boundary boundary;
	public GameObject geoForNetwork;

	public static int targetNumReflections = 8;
	public bool update = true;

	public bool enableListen = true;
	public bool doLateReflections = true;

	private reflectionFinder RF;

	private reflectionPath directSound;
	private delayLine directDelay;

	private const float airSpeed = 343.0f;

	private float directAtt = 0.0f;
	private float networkInScale = 0.0f;

	private List<reflectionPath> reflections;
	private List<WGWnode> network;

	private Queue<float> inSamples;
	private Queue<float> outSamples;

	private float inVal = 0.0f;
	private float outVal = 0.0f;
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

	void Start ()
	{
		AudioConfiguration AC = AudioSettings.GetConfiguration ();
		sampleRate = AC.sampleRate;
		buffSize = AC.dspBufferSize;

		inSamples = new Queue<float> ();
		outSamples = new Queue<float> ();

		delayLine.sampleRate = sampleRate;

		int initalDirectDelay = delayLine.distanceToDelayTime (Vector3.Distance (gameObject.transform.position, listener.transform.position));
		directDelay = new delayLine (1.0f, initalDirectDelay);

		network = new List<WGWnode> ();
		RF = gameObject.AddComponent<reflectionFinder> ();
		RF.setListener (listener);

		boundary.setBoundaryBounds (GetMaxBounds (geoForNetwork));	
		RF.setboundary (boundary);

		RF.setNumInitalDirections (targetNumReflections);
		RF.doUpdate = update;
		RF.onNewReflections += handleNewReflections;

		SDNDraw draw = GetComponent<SDNDraw> ();
		if (draw != null) {
			RF.onNewReflections += draw.updateVisualNetwork;
		}
		sampleMX = new Mutex ();
		networkMX = new Mutex ();
		audioProcessThread = new Thread (audioProcess);
		audioProcessThread.Start ();
		scriptInit = true;
	}	


	void Update ()
	{
		RF.doUpdate = update;

		if (update && haveNewReflections) {
			networkMX.WaitOne ();
			processReflections ();
			directDelay.setDelay (delayLine.distanceToDelayTime (directSound.totalDistance ()));
			directAtt = 1.0f / (Vector3.Distance (listener.transform.position, gameObject.transform.position) + 1.0f);
			networkInScale = 1.0f / network.Count;
			haveNewReflections = false;
			networkMX.ReleaseMutex();
		}
		checkDelayClear ();
	}

	void OnApplicationQuit() {
		audioProcessKeepAlive = false;
	}

	public void audioProcess() {

		while (audioProcessKeepAlive) {
			propagateNetwork ();
		}

	}

	public void propagateNetwork() {

		sampleMX.WaitOne ();
		networkMX.WaitOne ();
		int numSampsToConsume = inSamples.Count;
		// horrible hack to solve latency @ startup - the queue quickly fills up from
		// the audio thread before the main thread can catch up. I am a bad person for doing this.
		if (inSamples.Count > 10000) {
			inSamples.Clear ();
			sampleMX.ReleaseMutex ();
			return;
		}

		int i,j;

		for ( i = 0; i < numSampsToConsume; i++) {
			outVal = 0.0f;
			inVal = inSamples.Dequeue () * networkInScale;

			directDelay.write (inVal);
			directVal = directDelay.read ();
			directVal *= directAtt;


			for (j = 0; j < network.Count; j++) {
				outVal += network [j].getOutgoing ();
				network [j].inputIncoming (inVal);
				network [j].doScattering (doLateReflections);
			}


			outVal += directVal;
			outSamples.Enqueue (outVal);
		}
		sampleMX.ReleaseMutex ();
		networkMX.ReleaseMutex ();
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
				WGWnode n = network [i];
				List<WGWconnection> connections = n.getConnections ();
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
		WGWnode.setLoss (loss);
	}

	public void setNodeSpecularity (float spec)
	{
		WGWnode.setSpecularity (spec);
	}

	public void setNodeWallAbs (float abs)
	{
		WGWnode.setWallAbs (abs);
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

	public List<WGWnode> getNetwork ()
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
				network.Add (new WGWnode (reflections[i]));
			}
		}

		for (int i = 0; i < network.Count; i++) {
			for (int j = 1; j < network.Count; j++) {
				int idx = (j + i) % network.Count;
				if (!network [i].containsConnection (network [idx])) {
					network [i].addConnection (network [idx]);
				}
			}
		}

		for (int i = 0; i < network.Count; i++) {
			WGWnode n = network [i];
			n.findReverseConnections ();
			n.updateConnectionDelay ();
		}

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

	public class WGWnode
	{
		public static float specularity = 0.5f;
		public static float wallAbsCoeff = 0.97f;
		public static float nodeLoss = 1.0f - wallAbsCoeff;
		//		public static float nodeLoss = 0.0f;

		private Vector3 position;
		private List<WGWconnection> connections;
		private reflectionPath nodePath;

		private delayLine incoming;
		private delayLine outgoing;

		private BiQuadFilter wallFilter;

		private float scatteringFactor;
		private float scatteringFactorDiag;

		private float FOSample = 0.0f;
		private float HalfFOSample = 0.0f;
		private float outgoingSample = 0.0f;
		private float incomingAttFactor = 0.0f;
		private float outgoingAttFactor = 0.0f;

		private float interNodeAttFactor = 0.0f;
		private float attenuationMultiplier = 0.0f;

		private List<float> SOsamples;
		private float zero = 0.0f;


		private int numConnections = 0;

		public WGWnode (reflectionPath thePath)
		{
			wallFilter = BiQuadFilter.highPassAirFilter (delayLine.sampleRate);
			connections = new List<WGWconnection> ();
			nodePath = thePath;
			position = nodePath.segments [1].origin;
			incoming = new delayLine (1.0f, delayLine.distanceToDelayTime (nodePath.lengths [0]));
			outgoing = new delayLine (1.0f, delayLine.distanceToDelayTime (nodePath.lengths [1]));
			//set secondorder delay line here

			incomingAttFactor = 1.0f / (nodePath.lengths [0] + 1.0f);
			outgoingAttFactor = 1.0f / (nodePath.lengths [1] + 1.0f);
			//calculate second order attenuationfactors here
			SOsamples = new List<float>();
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
			FOSample *= incomingAttFactor;
			FOSample = wallFilter.Transform (FOSample);
			FOSample *= wallAbsCoeff;
			outgoingSample = FOSample;
			HalfFOSample = 0.5f * FOSample;

			int i ,j;

			for (i = 0; i < numConnections; i++) {
				connections [i].posSamp = connections [i].getSampleFromReverseConnection ();

				SOsamples [i] = connections [i].getSampleFromReverseConnection ();
				interNodeAttFactor = 1.0f / (1.0f + (nodePath.lengths [1] + connections[i].getLength()) / connections[i].getTargetDistanceToSource());
				attenuationMultiplier = 2.0f / (numConnections - 1);
			}

			for (i = 0; i < numConnections; i++) {

				outgoingSample += connections [i].posSamp;
				outgoingSample += connections [i].prevSample;
				connections [i].negSamp += (HalfFOSample);

				for (j = 0; j < numConnections; j++) {
					if (i == j) {
						connections [i].negSamp += connections [j].posSamp * scatteringFactorDiag;
					} else {
						connections [i].negSamp += connections [j].posSamp * scatteringFactor;
					}
				}

				//				connections [i].negSamp = connections [i].connectFilter.Transform (connections [i].negSamp);
				connections [i].negSamp -= connections [i].prevSample;
				connections [i].inputToDelay (connections [i].negSamp);
				connections [i].prevSample = connections [i].negSamp;

			}

			if (outputLateReflections) {
				outgoingSample *= outgoingAttFactor;
				outgoing.write (outgoingSample);
			} else {
				FOSample *= outgoingAttFactor;
				outgoing.write (FOSample);
			}
		}


		public void updateScatteringFactor ()
		{
			int minCon = connections.Count + 1;

			scatteringFactor = (2.0f / minCon) - nodeLoss;
			scatteringFactorDiag = ((2.0f - minCon) / minCon) - -nodeLoss;
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

		public List<WGWconnection> getConnections ()
		{
			return connections;
		}

		public void addConnection (WGWnode n)
		{
			connections.Add (new WGWconnection (this, n));
			updateScatteringFactor ();
			numConnections = connections.Count;

			//add element to SOsamples. This needs to be the same size as numConnections
			SOsamples.Add(zero);
		}

		public void findReverseConnections ()
		{
			for (int i = 0; i < connections.Count; i++) {
				WGWconnection theTarget = connections [i].getTarget ().connections.Find (item => item.getTarget () == this);
				connections [i].setReverseConnection (ref theTarget);
			}
		}

		public void informConnectionDelete ()
		{
			for (int i = 0; i < connections.Count; i++) {
				WGWnode nodeToInform = connections [i].getTarget ();
				int idx = nodeToInform.connections.FindIndex (item => item.getTarget () == this);
				nodeToInform.connections.RemoveAt (idx);
				nodeToInform.numConnections = nodeToInform.connections.Count;
				nodeToInform.updateScatteringFactor ();

			}
		}

		public bool containsConnection (WGWnode n)
		{
			for (int i = 0; i < connections.Count; i++) {
				if (connections [i].getTarget ().Equals (n)) {
					return true;
				}
			}
			return false;
		}

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

	public class WGWconnection
	{
		public float posSamp = 0.0f;
		public float negSamp = 0.0f;
		public float prevSample = 0.0f;

		private WGWnode parent;
		private WGWnode target;
		private float length;
		private float distanceToSource;
		private delayLine delay;
		private WGWconnection reverseConnection;
		public BiQuadFilter connectFilter;

		public WGWconnection (WGWnode theParent, WGWnode theTarget)
		{
			connectFilter = BiQuadFilter.highPassAirFilter (delayLine.sampleRate);

			parent = theParent;
			target = theTarget;
			length = Vector3.Distance (parent.getPosition (), target.getPosition ());
			delay = new delayLine (1.0f, delayLine.distanceToDelayTime (length));
			distanceToSource = theTarget.getSourceNodeDistance();
		}

		public void clearDelay ()
		{
			delay.clear ();
		}

		public void setTarget (WGWnode n)
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

		public WGWnode getTarget ()
		{
			return target;
		}

		public WGWnode getParent ()
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

		public void setReverseConnection (ref WGWconnection c)
		{
			reverseConnection = c;
		}

		public void updateDelayLength ()
		{
			length = Vector3.Distance (parent.getPosition (), target.getPosition ());
			delay.setDelay (delayLine.distanceToDelayTime (length));
		}
	}

	public class WGWSecondConnection
	{
		public float posSamp = 0.0f;
		public float negSamp = 0.0f;
		public float prevSample = 0.0f;

		private WGWnode parent;
		private WGWnode middle;
		private WGWnode target;
		private float length;
		private delayLine delay;
		private WGWconnection reverseConnection;
		public BiQuadFilter connectFilter;

		public WGWSecondConnection (WGWnode theParent, WGWnode theMiddle, WGWnode theTarget)
		{
			connectFilter = BiQuadFilter.highPassAirFilter (delayLine.sampleRate);

			parent = theParent;
			middle = theMiddle;
			target = theTarget;
			length = Vector3.Distance (parent.getPosition (), middle.getPosition ()) + Vector3.Distance (middle.getPosition(),target.getPosition());
			delay = new delayLine (1.0f, delayLine.distanceToDelayTime (length));
		}

		public void clearDelay ()
		{
			delay.clear ();
		}

		public void setMiddle (WGWnode n)
		{
			middle = n;
			updateDelayLength ();
		}

		public void setTarget (WGWnode n)
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

		public WGWnode getTarget ()
		{
			return target;
		}

		public WGWnode getMiddle ()
		{
			return middle;
		}

		public WGWnode getParent ()
		{
			return parent;
		}

		public float getLength ()
		{
			return length;
		}

		public void setReverseConnection (ref WGWconnection c)
		{
			reverseConnection = c;
		}

		public void updateDelayLength ()
		{
			length = Vector3.Distance (parent.getPosition (), middle.getPosition ()) + Vector3.Distance (middle.getPosition(),target.getPosition());
			delay.setDelay (delayLine.distanceToDelayTime (length));
		}
	}
}


