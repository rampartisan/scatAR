using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reflectionPath
{
	public Vector3 origin;
	public Vector3 destination;
	public List<Ray> segments;
	public List<float> lengths;
	public bool isValid;

	public reflectionPath (Vector3 sourcePos, Vector3 listenerPos)
	{
		origin = sourcePos;
		destination = listenerPos;
		segments = new List<Ray> ();
		lengths = new List<float> ();
		isValid = false;

	}

	public void clear ()
	{
		origin = Vector3.zero;
		destination = Vector3.zero;
		segments.Clear ();
		lengths.Clear ();
		isValid = false;

	}

	public float totalDistance() {
		float total = 0.0f;
		foreach (float f in lengths) {
			total += f;
		}
		return total;
	}

}
