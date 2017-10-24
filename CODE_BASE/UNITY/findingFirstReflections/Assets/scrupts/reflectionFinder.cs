using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reflectionFinder : MonoBehaviour
{
	// handle onto audio listener
	private GameObject listener;
	// handle onto boundary object that surrounds all geometry to be used for reflection finding
	private boundary boundary;
	// Max number of reflections to return
	private static int targetNumReflections = 8;
	// Should we get new reflections?
	public bool doUpdate = false;

	// Callback for when we have new reflections, other scripts can subscribe and get the newest 
	// direct sound and reflection list here ( reflectionFinder.onNewReflections += FUNCTION)
	public delegate void newReflections(reflectionPath directsound,List<reflectionPath> reflections);
	public event newReflections onNewReflections;

	// List of inital directions for the boundary finding rays
	private Vector3[] initalDirections;

	private Vector3 sourcePos;
	private Vector3 listenerPos;
	private Vector3 prevListenerPos = Vector3.zero;
	private Vector3 prevSourcePos = Vector3.zero;

	private reflectionPath directSound;
	private List<reflectionPath> reflections;

	// Angle to vary the direction of the boundary finding ray, so 
	// we deviate around its centre with and some scalars for it
	private float boundaryRayAngleDistance;
	private float[] xMuls = { 1f, 0f, -1f, 0f, 1f, 1f, -1f, -1f };
	private float[] zMuls = { 0f,  0.5f, 0, -0.5f, -1f,  1f, 1, -1f };

	// used in co-routine to make sure we dont update whilst we are allready updating
	private bool currentlyUpdating = false;

	void Start ()
	{
		directSound = new reflectionPath (Vector3.zero, Vector3.zero);
		reflections = new List<reflectionPath> ();
		sourcePos = gameObject.transform.position;

		initalDirections = new Vector3[targetNumReflections];
		generateDirections ();

		boundaryRayAngleDistance = (360.0f / initalDirections.Length) / 2.0f;
	}

	void Update ()
	{
		// If we should update and we are not allready doing so, check to see if the listener/source
		// has moved a substatial distance and begin the update
			if (doUpdate) {
				if (!currentlyUpdating) {
				if (Vector3.Distance (prevSourcePos, gameObject.transform.position) > 0.1f || Vector3.Distance (prevListenerPos, listener.transform.position) > 0.1f) {
					updateAllPaths ();
					}
				}
			}
	}
		
	public void setNumInitalDirections (int num)
	{

		if (currentlyUpdating) {
			StopCoroutine (updateReflections ());
			currentlyUpdating = false;
		}

		initalDirections = new Vector3[num];
		generateDirections ();
		boundaryRayAngleDistance = (360.0f / initalDirections.Length) / 2.0f;
	}

	public void setListener (GameObject listenerObject)
	{
		listener = listenerObject;
		listenerPos = listenerObject.transform.position;
	}

	public void setboundary(boundary newB) {
		boundary = newB;
	}

	public bool isUpdating ()
	{
		return currentlyUpdating;
	}
		
	public void updateAllPaths ()
	{
		if (!currentlyUpdating) {
			currentlyUpdating = true;

			sourcePos = gameObject.transform.position;
			listenerPos = listener.transform.position;

			updateDirectSound ();
			StartCoroutine (updateReflections ());
		}

	}

	private void updateDirectSound ()
	{
		// turn off the collider for cases when the listener is outside the
		// boundary, in which case a ray will allways hit the boundary instead 
		// of the listener
		boundary.turnOffCollider ();
		directSound.clear ();
		directSound.origin.Set (sourcePos.x, sourcePos.y, sourcePos.z);
		directSound.destination.Set (listenerPos.x, listenerPos.y, listenerPos.z);

		// Define a ray going from source -> listener
		directSound.segments.Add (new Ray (directSound.origin, directSound.destination - directSound.origin));
		directSound.lengths.Add (Vector3.Distance (sourcePos, listenerPos));

		// Make sure the ray actually hits the listener
		RaycastHit hit;
		if (Physics.Raycast (directSound.segments [0], out hit)) {
			if (hit.collider.name == listener.name) {
				directSound.isValid = true;
			} 
		}

	}


	private IEnumerator updateReflections ()
	{
		// Search for valid first order reflections over a few frames
		// A reflection is found by shooting a ray in order to find some
		// surface we can reflect off, then the image source is calculated.
		// This reflection is then verified and added if it passes the checks else
		// discard and repeat with a slightly altered rotated direction (to hopefully
		// find another relfective surface)

		int pathCount = 1;
		reflections.Clear ();
		for(int i = 0; i < initalDirections.Length; i++) {
			Vector3 dir = initalDirections[i];
			// turn off the collider for cases when the listener is outside the
			// boundary, in which case a ray will allways hit the boundary instead 
			// of the listener
			boundary.turnOffCollider();

			Ray boundaryRay = new Ray (sourcePos, dir);
			reflectionPath currPath = new reflectionPath (sourcePos, listenerPos);
			int numTry = 0;

			while (!currPath.isValid && numTry < 8) {
				findReflection (boundaryRay, ref currPath);
				boundaryRay.direction = rotateDirection (dir, numTry++);
			}

			if (currPath.isValid) {
				reflections.Add (currPath);
			} else {
				boundary.turnOnCollider();
				findReflection (new Ray (sourcePos, dir), ref currPath);
				if (currPath.isValid) {
					reflections.Add (currPath);
				}
				boundary.turnOffCollider ();
			}

			if (pathCount % 4 == 0) {
				yield return null;
			}

		}
		onNewReflections(directSound,reflections);
		currentlyUpdating = false;
	}

	private void findReflection (Ray boundaryRay, ref reflectionPath currPath)
	{

		RaycastHit boundaryHit;

		if (Physics.Raycast (boundaryRay, out boundaryHit)) {

			if (boundaryHit.collider.name != listener.name) {

				Vector3 invNormal = boundaryHit.normal * -1.0f;
				float angleToPlane = Vector3.Dot (invNormal, boundaryRay.direction);
				angleToPlane /= invNormal.magnitude * boundaryRay.direction.magnitude;
				float perpendicularDist = angleToPlane * boundaryHit.distance;

				Vector3 imageSource = sourcePos + (perpendicularDist * 2 * invNormal);
				Ray imageSourceRay = new Ray (imageSource, (listenerPos - imageSource).normalized);

				float angleToListener = Vector3.Dot (boundaryHit.normal, imageSourceRay.direction);
				angleToListener /= (boundaryHit.normal.magnitude * imageSourceRay.direction.magnitude);
				angleToListener = Mathf.Acos (angleToListener);
				float len = perpendicularDist / Mathf.Cos (angleToListener);
				Vector3 reflectionPoint = imageSource + (imageSourceRay.direction * len);

				currPath.segments.Add (new Ray (sourcePos, (reflectionPoint - sourcePos).normalized));
				currPath.segments.Add (new Ray (reflectionPoint, (listenerPos - reflectionPoint).normalized));
				currPath.lengths.Add (Vector3.Distance (sourcePos, reflectionPoint));
				currPath.lengths.Add (Vector3.Distance (reflectionPoint, listenerPos));

				validatePath (currPath);
			}
		} 
	}


	private bool validatePath (reflectionPath p)
	{

		RaycastHit incomingHit;

		if (!Physics.Raycast (p.segments [0], out incomingHit)) {
			return false;
		}

		if (incomingHit.collider.name == listener.name) {
			return false;
		}

			for(int i = 0; i < reflections.Count; i++) {
			reflectionPath cp = reflections [i];
			if (Vector3.Distance (p.segments [1].origin, cp.segments [1].origin) < 0.1f) {
				return false;
			}
		}

		RaycastHit outgoingHit;

		if (!Physics.Raycast (p.segments [1], out outgoingHit)) {
			return false;
		}

		if (outgoingHit.collider.name != listener.name) {
			return false;
		}

		p.isValid = true;
		return true;
	}

	private void generateDirections ()
	{

		int len = initalDirections.Length;
		float offset = 2.0f / len;
		float inc = Mathf.PI * (3.0f - Mathf.Sqrt (5.0f));

		for (int i = 0; i < len; i++) {

			float y = ((i * offset) - 1) + (offset / 2.0f);
			float r = Mathf.Sqrt (1 - Mathf.Pow (y, 2));

			float phi = (i % len) * inc;
			float x = Mathf.Cos (phi) * r;
			float z = Mathf.Sin (phi) * r;

			initalDirections [i] = new Vector3 (x, y, z);

		}

		float angleToRotate = Vector3.Angle (initalDirections [0], Vector3.up);

		for (int i = 0; i < initalDirections.Length; i++) {
			initalDirections [i] = Quaternion.AngleAxis (angleToRotate, Vector3.forward) * initalDirections [i];
		}
	}

	private Vector3 rotateDirection (Vector3 dir, int idx)
	{
		idx %= xMuls.Length;
		return Quaternion.Euler (new Vector3 (xMuls [idx] * boundaryRayAngleDistance, 0, zMuls [idx] * boundaryRayAngleDistance)) * dir;
	}

}