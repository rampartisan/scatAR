using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class SDN1 : MonoBehaviour
{
	public GameObject listener;
	public boundary boundary;

	public static int targetNumReflections = 6;
	public bool update = true;
	public bool enableListen = true;
	public bool doLateReflections = true;

	private reflectionFinder RF;

	private reflectionPath directSound;
	private delayLine directDelay;

	private List<reflectionPath> reflections;
	private List<SDNnode> network;

	private List<reflectionPath> waitingReflections;
	private reflectionPath waitingDirect;

	private int sampleRate;
	private bool isProc = false;
	private bool haveNewReflections = false;
	public GameObject env;
	private Material glowingMat;
	private float glowingVal = 0.0f;

	void Start ()
	{
		glowingMat = gameObject.GetComponent<Renderer> ().material;
		AudioConfiguration AC = AudioSettings.GetConfiguration ();

		sampleRate = AC.sampleRate;

		delayLine.sampleRate = sampleRate;
		int initalDirectDelay = delayLine.distanceToDelayTime (Vector3.Distance (gameObject.transform.position, listener.transform.position));
		directDelay = new delayLine (1.0f, initalDirectDelay);
		network = new List<SDNnode> ();
		RF = gameObject.AddComponent<reflectionFinder> ();
		RF.setListener (listener);
		RF.setboundary (boundary);
		RF.setNumInitalDirections (targetNumReflections);
		RF.doUpdate = update;
		RF.onNewReflections += handleNewReflections;

		this.gameObject.GetComponent<AudioSource> ().Play ();
		boundary.setBoundaryBounds (GetMaxBounds (env));

		SDNDraw draw = GetComponent<SDNDraw> ();
		if (draw != null) {
						RF.onNewReflections += draw.updateVisualNetwork;
		}

	}

	public Bounds GetMaxBounds(GameObject g) {
		Bounds b = new Bounds (g.transform.position, Vector3.zero);
		foreach(MeshRenderer r in g.GetComponentsInChildren<Renderer>()) {
			b.Encapsulate (r.bounds);
		}
		return b;
	}

	void Update ()
	{
		boundary.setBoundaryBounds (GetMaxBounds (env));

		glowingMat.SetFloat ("_MKGlowTexStrength", glowingVal * 7.0f);

		if (!isProc) {
			reflections = waitingReflections;
			directSound = waitingDirect;
		}

	}

	public void clearAllDelays ()
	{
		directDelay.clear ();
		for(int i = 0; i < network.Count;i++) {
			SDNnode n = network[i];
			List<SDNConnection> connections = n.getConnections();
			n.clearDelay ();
			for(int j = 0; j < connections.Count; j++) {
				connections[i].clearDelay ();
			}
		}
	}

	public void setNodeLoss(float loss) {
		SDNnode.setLoss (loss);
	}

	public void setNodeSpecularity(float spec) {
		SDNnode.setSpecularity (spec);
	}
	public void setNodeWallAbs(float abs) {
		SDNnode.setWallAbs (abs);
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

	public List<SDNnode> getNetwork ()
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
		if (update && haveNewReflections) {
			processReflections ();
			directDelay.setDelay (delayLine.distanceToDelayTime (directSound.totalDistance ()));
			haveNewReflections = false;
		}

		if (directSound != null && network != null) {

			int numSamps = data.Length / channels;

			float sum = 0.0f;

			float[] monoIn = new float[numSamps];
			float[] direct = new float[numSamps];
			float[] monoOut = new float[numSamps];

			for (int i = 0; i < numSamps; i++) {
				for (int c = 0; c < channels; c++) { 
					monoIn [i] += data [(i * channels) + c];
				}
				monoIn [i] *= 1.0f / channels;

				sum += Mathf.Abs (monoIn [i]);
			}

			sum /= numSamps;
			glowingVal += sum;
			glowingVal /= 2.0f;


			float directAtt = 1/Vector3.Distance(directSound.origin,directSound.destination);

			for (int i = 0; i < numSamps; i++) {
				directDelay.write (monoIn[i]);
				direct[i] = directDelay.read();
				direct [i] *= directAtt;
			}
		
			float scaling = 1.0f / network.Count;



			for (int i = 0; i < numSamps; i++) {
//				monoIn [i] *= scaling;
				for(int j = 0; j < network.Count; j++) {
					network[j].inputIncoming (monoIn[i]);
				}
			}

			for (int i = 0; i < numSamps; i++) {
				for (int j = 0; j < network.Count; j++) {
					network [j].doScattering(true);
				}
			}

			for (int i = 0; i < numSamps; i++) {
				for (int j = 0; j < network.Count; j++) {
					monoOut[i] += network [j].getOutgoing();
				}
			}

				
			for (int i = 0; i < numSamps; i++) {
				monoOut[i] += direct [i];
			}
				
			if (enableListen) {
				for (int i = 0; i < numSamps; i++) {
					for (int c = 0; c < channels; c++) {
						data [(i * channels) + c] = monoOut [i];
					}
				}
			}
		} 
	}

	void handleNewReflections (reflectionPath newDirectSound, List<reflectionPath> newReflections)
	{
		waitingDirect = newDirectSound;
		waitingReflections = newReflections;
		haveNewReflections = true;
	}


	private void processReflections ()
	{

		isProc = true;

		for(int j = 0; j < network.Count; j++) {
			SDNnode n = network [j];

			if (reflections.Count > 0) {

				int minDistIdx = 0;
				float minDist = float.MaxValue;

				for (int i = 0; i < reflections.Count; i++) {

					float dist = Vector3.Distance (reflections [i].segments [1].origin, n.getPosition ());

					if (dist < minDist) {
						minDist = dist;
						minDistIdx = i;
					}
				}

				n.updatePath (reflections [minDistIdx]);
				reflections.RemoveAt (minDistIdx);

			} else {
				n.informConnectionDelete ();
				network.Remove (n);

			}
		}

		if (reflections.Count > 0) {
			foreach (reflectionPath p in reflections) {
				network.Add (new SDNnode (p));
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

		for(int i = 0; i < network.Count;i++) {
			SDNnode n = network [i];
			n.findReverseConnections ();
			n.updateConnectionDelay ();
		}
		isProc = false;
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
			if (newDelayTime > sampleRate) {
				return;
			}


			if (newDelayTime > capacity) {
				print (string.Format ("delay time of {0} for buffer size of {1} is not possible", newDelayTime, capacity));
				newDelayTime = capacity - 1;
			}

			if (newDelayTime < 0) {
				newDelayTime = 1;
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
			float outSamp;

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
				outSamp  = buffer [readPtr];
			}

			readPtr ++;
			readPtr %= capacity;
			return outSamp;
		}
			

		public static int distanceToDelayTime (float distance)
		{
			return (int)((distance * sampleRate) / 340.0f);

		}
			
	public static float attenuateForDistance (float sample, Vector3 lpos,Vector3 spos)
		{
			return sample *=  1.0f / (Mathf.Sqrt((lpos - spos).sqrMagnitude));
		}
	}

	public class SDNnode
	{
		private Vector3 position;
		private List<SDNConnection> connections;
		private reflectionPath nodePath;

		private delayLine incoming;
		private delayLine outgoing;

		private BiQuadFilter connectFilter;
		private BiQuadFilter wallFilter;

		private float scatteringFactor;
		public static float specularity = 0.5f;
		public static float nodeLoss = 0.05f;
		public static float wallAbsCoeff = 0.93f;

		private float prevConnectionVal = 0.0f;

		public SDNnode (reflectionPath thePath)
		{
			wallFilter = BiQuadFilter.LowPassFilter (delayLine.sampleRate, 10000, 0.7071f);
			connectFilter = BiQuadFilter.LowPassFilter (delayLine.sampleRate, 13000, 0.7071f);

			connections = new List<SDNConnection> ();
			nodePath = thePath;
			position = nodePath.segments [1].origin;
			incoming = new delayLine (1.0f, delayLine.distanceToDelayTime (nodePath.lengths [0]));
			outgoing = new delayLine (1.0f, delayLine.distanceToDelayTime (nodePath.lengths [1]));
		}

		public void updatePath (reflectionPath thePath)
		{
			nodePath = thePath;
			position = thePath.segments [1].origin;
			incoming.setDelay (delayLine.distanceToDelayTime (nodePath.lengths [0]));
			outgoing.setDelay (delayLine.distanceToDelayTime (nodePath.lengths [1]));
		}

		public void clearDelay ()
		{
			incoming.clear ();
			outgoing.clear ();
		}

		public static void setLoss(float loss) {
			nodeLoss = loss;
		}

		public static void setSpecularity(float spec) {
			specularity = spec;
		}

		public static void setWallAbs(float abs) {
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
			
		public void doScattering ( bool outputLateReflections)
		{
			float FOSample = incoming.read();
			FOSample *=  1 / nodePath.lengths[0];
			FOSample = wallFilter.Transform (FOSample);
			FOSample  *= wallAbsCoeff;

			float connectionSample = 0.0f;
			float outgoingSample = 0.0f;

			foreach (SDNConnection c in connections) {
				connectionSample += c.getSampleFromReverseConnection ();
			}

			outgoingSample += connectionSample;

			connectionSample *= scatteringFactor;
			connectionSample += prevConnectionVal;			
			connectionSample *= 0.5f;


			connectionSample = connectFilter.Transform (connectionSample);
			connectionSample += FOSample * ((1 - specularity) - nodeLoss);
			outgoingSample  += FOSample * (specularity - nodeLoss);


			prevConnectionVal = connectionSample;

			foreach (SDNConnection c in connections) {
				c.inputToDelay (connectionSample);
			}

			if (outputLateReflections) {
				outgoingSample *= 1 / nodePath.lengths [1];
				outgoing.write (outgoingSample);
			} else {
				FOSample *= 1 / nodePath.lengths[1];
				outgoing.write (FOSample);
			}
		}


		public void updateScatteringFactor ()
		{
			scatteringFactor = 1.0f / (connections.Count);
		}

		public void updateConnectionDelay ()
		{
			for(int i = 0; i < connections.Count; i++) {
				connections[i].updateDelayLength ();
			}
		}

		public List<SDNConnection> getConnections ()
		{
			return connections;
		}

		public void addConnection (SDNnode n)
		{
			connections.Add (new SDNConnection (this, n));
			updateScatteringFactor ();
		}

		public void findReverseConnections ()
		{
			for(int i = 0; i < connections.Count; i++) {
				SDNConnection theTarget = connections[i].getTarget ().connections.Find (item => item.getTarget () == this);
				connections[i].setReverseConnection (ref theTarget);
			}
		}

		public void informConnectionDelete ()
		{
			for(int i = 0; i < connections.Count; i++) {
				SDNnode nodeToInform = connections[i].getTarget ();
				nodeToInform.connections.Remove (nodeToInform.connections.Find (item => item.getTarget () == this));
				nodeToInform.updateScatteringFactor ();
			}
		}

		public bool containsConnection (SDNnode n)
		{
			for(int i = 0; i < connections.Count; i++) {
				if (connections[i].getTarget ().Equals (n)) {
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

	public class SDNConnection
	{

		private SDNnode parent;
		private SDNnode target;
		private float length;
		private delayLine delay;
		private SDNConnection reverseConnection;

		public SDNConnection (SDNnode theParent, SDNnode theTarget)
		{
			parent = theParent;
			target = theTarget;
			length = Vector3.Distance (parent.getPosition (), target.getPosition ());
			delay = new delayLine (1.0f, delayLine.distanceToDelayTime (length));
		}


		public void clearDelay ()
		{
			delay.clear ();
		}

		public void setTarget (SDNnode n)
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

		public SDNnode getTarget ()
		{
			return target;
		}

		public SDNnode getParent ()
		{
			return parent;
		}

		public float getLength ()
		{
			return length;
		}

		public void setReverseConnection (ref SDNConnection c)
		{
			reverseConnection = c;
		}

		public void updateDelayLength ()
		{
			length = Vector3.Distance (parent.getPosition (), target.getPosition ());
			delay.setDelay (delayLine.distanceToDelayTime (length));
		}
	}
}
