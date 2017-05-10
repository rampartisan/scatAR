using System.Collections;
using System.Linq;

using System.Collections.Generic;
using UnityEngine;

public class SDN : MonoBehaviour
{

	public GameObject listener;
	public boundary boundary;

	public int targetNumReflections = 6;
	public bool update = true;
	public bool enableListen = true;
	public bool doLateReflections = true;

	public delegate void newNetwork (List<SDNnode> network, reflectionPath directSound);

	public event newNetwork onNewNetwork;

	private reflectionFinder RF;

	private reflectionPath directSound;
	private delayLine directDelay;

	private List<reflectionPath> reflections;
	private List<SDNnode> network;

	private int sampleRate;

	private bool haveNewReflections = false;
	private bool isProcessing = false;

	void Start ()
	{
		AudioConfiguration AC = AudioSettings.GetConfiguration ();
		sampleRate = AC.sampleRate;

		delayLine.sampleRate = sampleRate;
		int initalDirectDelay = delayLine.distanceToDelayTime (Vector3.Distance (gameObject.transform.position, listener.transform.position));
		directDelay = new delayLine (3.0f, initalDirectDelay);

		network = new List<SDNnode> ();
		RF = gameObject.AddComponent<reflectionFinder> ();
		RF.setListener (listener);
		RF.setboundary (boundary);
		RF.setNumInitalDirections (targetNumReflections);
		RF.doUpdate = update;
		RF.onNewReflections += handleNewReflections;
	}
		
	void Update ()
	{
		if (update && !isProcessing && haveNewReflections) {
			StartCoroutine (processReflections ());
			directDelay.setDelay (delayLine.distanceToDelayTime (directSound.totalDistance ()));
		}
	}

	public void clearAllDelays ()
	{
		directDelay.clear ();
		foreach (SDNnode n in network) {
			n.clearDelay ();
			foreach (SDNConnection c in n.getConnections()) {
				c.clearDelay ();
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

	public void coroutineHandler (IEnumerator coroutineMethod)
	{
		StartCoroutine (coroutineMethod);
	}

	public List<SDNnode> getNetwork ()
	{
		return network;
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

	void OnAudioFilterRead (float[] data, int channels)
	{
		if (directSound != null && network != null) {

			int numSamps = data.Length / channels;

			float[] monoIn = new float[numSamps];
			float[] direct = new float[numSamps];

			float[] monoOut = new float[numSamps];

			for (int i = 0; i < numSamps; i++) {
				for (int c = 0; c < channels; c++) {
					monoIn [i] += data [(i * channels) + c];
				}
				monoIn [i] *= 1.0f / channels;
			}

			directDelay.write (monoIn);
			directDelay.readToArray (ref direct);
			delayLine.attenuateForDistance (ref direct, directSound.lengths [0]);

			foreach (SDNnode n in network) {
				n.inputIncoming (monoIn);
				n.doScattering (monoIn.Length, doLateReflections);
				n.getOutgoing (ref monoOut);
			}
			float att = 2.0f / network.Count;
			for (int i = 0; i < numSamps; i++) {
				monoOut [i] *= att;
				monoOut [i] += direct [i];
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
		directSound = newDirectSound;
		reflections = newReflections;
		haveNewReflections = true;
	}


	private IEnumerator processReflections ()
	{
		isProcessing = true;
		haveNewReflections = false;



		foreach (SDNnode n in network.ToList()) {

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
		yield return null;

		for (int i = 0; i < network.Count; i++) {
			for (int j = 1; j < network.Count; j++) {
				int idx = (j + i) % network.Count;
				if (!network [i].containsConnection (network [idx])) {
					network [i].addConnection (network [idx]);
				}
			}
		}

		foreach (SDNnode n in network) {
			n.findReverseConnections ();
			n.updateConnectionDelay ();
		}

		yield return null;
		onNewNetwork (network, directSound);
		isProcessing = false;
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

	public reflectionPath getDirectSound ()
	{
		return directSound;
	}

	public List<reflectionPath> getReflections ()
	{
		return reflections;
	}

	public class delayLine
	{
		public static int sampleRate;
		public static float interpVal = 0.1f;
		public static float airCoeff = 0.015f;

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

		public void write (float[] samples)
		{
			for (int i = 0; i < samples.Length; i++) {
				buffer [(writePtr + i) % capacity] = samples [i];
			}

			writePtr += samples.Length;
			writePtr %= capacity;
		}

		public void readToArray (ref float[] outSamps)
		{
			if (newInterpWaiting && !doInterp) {
				doInterp = true;
				newInterpWaiting = false;
				interpReadPtr = newInterpReadPtr;
				interpVal = 0.0f;
			}

			if (doInterp) {
				float val;
				int startReadIdx = readPtr - outSamps.Length;
				if (startReadIdx < 0) {
					startReadIdx += capacity;
				}
				int startInterpIdx = interpReadPtr - outSamps.Length;
				if (startInterpIdx < 0) {
					startInterpIdx += capacity;
				}
				for (int i = 0; i < outSamps.Length; i++) {

					val = (buffer [(startReadIdx + i) % capacity] * (1.0f - interpFactor)) + (buffer [(startInterpIdx + i) % capacity] * interpFactor);
					outSamps [i] += val;

				}
				interpReadPtr += outSamps.Length;
				interpReadPtr %= capacity;
				interpFactor += interpVal;

				if (interpFactor > 1.0f) {
					readPtr = interpReadPtr;
					interpFactor = 0.0f;
					doInterp = false;
				}

			} else {

				int startReadIdx = readPtr - outSamps.Length;
				if (startReadIdx < 0) {
					startReadIdx += capacity;
				}

				for (int i = 0; i < outSamps.Length; i++) {
					outSamps [i] += buffer [(startReadIdx + i) % capacity];
				}
			}

			readPtr += outSamps.Length;
			readPtr %= capacity;
		}

		public float[] read (int numSamps)
		{
			if (newInterpWaiting && !doInterp) {
				doInterp = true;
				newInterpWaiting = false;
				interpReadPtr = newInterpReadPtr;
				interpVal = 0.0f;
			}

			float[] outSamps = new float[numSamps];
			if (doInterp) {

				float val;
				int startReadIdx = readPtr - outSamps.Length;
				if (startReadIdx < 0) {
					startReadIdx += capacity;
				}
				int startInterpIdx = readPtr - outSamps.Length;
				if (startInterpIdx < 0) {
					startInterpIdx += capacity;
				}

				for (int i = 0; i < outSamps.Length; i++) {
					val = (buffer [(startReadIdx + i) % capacity] * (1.0f - interpFactor)) + (buffer [(startInterpIdx + i) % capacity] * interpFactor);
					outSamps [i] = val;

				}
				interpReadPtr += outSamps.Length;
				interpReadPtr %= capacity;
				interpFactor += interpVal;

				if (interpFactor >= 1.0f) {
					readPtr = interpReadPtr;
					interpFactor = 0.0f;
					doInterp = false;
				}

			} else {

				int startReadIdx = readPtr - outSamps.Length;
				if (startReadIdx < 0) {
					startReadIdx += capacity;
				}

				for (int i = 0; i < outSamps.Length; i++) {
					outSamps [i] = buffer [(startReadIdx + i) % capacity];
				}
			}
			readPtr += outSamps.Length;
			readPtr %= capacity;

			return outSamps;
		}

		public static int distanceToDelayTime (float distance)
		{
			return (int)((distance * sampleRate) / 340.0f);

		}

		public static void attenuateForDistance (ref float[] samples, float distance)
		{
			float G = 340.0f / sampleRate;
			float attFactor = 1.0f - (G / distance);
			for (int i = 0; i < samples.Length; i++) {
				samples [i] *= attFactor;
			}
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
		private static float specularity = 0.55f;
		private static float nodeLoss = 0.05f;
		private static float wallAbsCoeff = 0.95f;

		public SDNnode (reflectionPath thePath)
		{
			wallFilter = BiQuadFilter.LowPassFilter (delayLine.sampleRate, 9000, 0.75f);
			connectFilter = BiQuadFilter.LowPassFilter (delayLine.sampleRate, 9000, 0.75f);

			connections = new List<SDNConnection> ();

			nodePath = thePath;
			position = nodePath.segments [1].origin;
			incoming = new delayLine (3.0f, delayLine.distanceToDelayTime (nodePath.lengths [0]));
			outgoing = new delayLine (3.0f, delayLine.distanceToDelayTime (nodePath.lengths [1]));
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
			nodeLoss = abs;
		}

		public void inputIncoming (float[] samples)
		{
			incoming.write (samples);
		}

		public void getOutgoing (ref float[] outSamps)
		{
			outgoing.readToArray (ref outSamps);
		}

		public void doScattering (int numSamps, bool outputLateReflections)
		{
			float[] firstOrderSamples = incoming.read (numSamps);
			delayLine.attenuateForDistance (ref firstOrderSamples, nodePath.lengths [0]);

			for (int i = 0; i < numSamps; i++) {
				firstOrderSamples [i] *= wallAbsCoeff;
				firstOrderSamples [i] *= specularity - nodeLoss;
				firstOrderSamples [i] = wallFilter.Transform (firstOrderSamples [i]);
			}

			float[] connectionSamples = new float[numSamps];
			foreach (SDNConnection c in connections) {	
				c.getSamplesFromReverseConnection (ref connectionSamples);
			}

			float[] outGoingSamples = new float[numSamps];
			System.Array.Copy (firstOrderSamples, outGoingSamples, numSamps);

			for (int i = 0; i < numSamps; i++) {

				connectionSamples [i] = connectFilter.Transform (connectionSamples [i]);
				outGoingSamples [i] += connectionSamples [i];
				connectionSamples [i] *= scatteringFactor;
				connectionSamples [i] += (firstOrderSamples [i] * ((1.0f - specularity) - nodeLoss)) * scatteringFactor;
				connectionSamples [i] *= wallAbsCoeff;
			}

			foreach (SDNConnection c in connections) {
				c.inputToDelay (connectionSamples);
			}

			if (outputLateReflections) {
				delayLine.attenuateForDistance (ref outGoingSamples, nodePath.lengths [1]);
				outgoing.write (outGoingSamples);
			} else {
				delayLine.attenuateForDistance (ref firstOrderSamples, nodePath.lengths [1]);
				outgoing.write (firstOrderSamples);
			}
		}


		public void updateScatteringFactor ()
		{
			scatteringFactor = 1.0f / connections.Count;
		}

		public void updateConnectionDelay ()
		{
			foreach (SDNConnection c in connections) {
				c.updateDelayLength ();
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
			foreach (SDNConnection c in connections) {
				SDNConnection theTarget = c.getTarget ().connections.Find (item => item.getTarget () == this);
				c.setReverseConnection (ref theTarget);
			}
		}

		public void informConnectionDelete ()
		{
			foreach (SDNConnection c in connections) {
				SDNnode nodeToInform = c.getTarget ();
				nodeToInform.connections.Remove (nodeToInform.connections.Find (item => item.getTarget () == this));
				nodeToInform.updateScatteringFactor ();
			}
		}


		public bool containsConnection (SDNnode n)
		{
			foreach (SDNConnection c in connections) {
				if (c.getTarget ().Equals (n)) {
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
			delay = new delayLine (3.0f, delayLine.distanceToDelayTime (length));
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

		public void inputToDelay (float[] samples)
		{
			delay.write (samples);
		}

		public void readFromDelay (ref float[] samples)
		{
			delay.readToArray (ref samples);
		}

		public void getSamplesFromReverseConnection (ref float[] samples)
		{
			if (reverseConnection != null) {

				float[] temp = new float[samples.Length];
				reverseConnection.readFromDelay (ref temp);
				delayLine.attenuateForDistance (ref temp, samples.Length);

				for (int i = 0; i < samples.Length; i++) {
					samples [i] += temp [i];
				}
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
