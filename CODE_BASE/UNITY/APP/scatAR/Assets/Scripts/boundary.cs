using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boundary : MonoBehaviour {
	private procCube boundaryCube;
	private MeshCollider mc;
	public bool init = false;
	void Start () {
		
		
	}
	
	void Update () {
		
	}

	public void initialise() {
		boundaryCube = new procCube (true);
		mc  = gameObject.AddComponent<MeshCollider> ();
		mc.sharedMesh = boundaryCube.mesh;
		gameObject.AddComponent<MeshRenderer> ();
		MeshFilter mf = gameObject.AddComponent<MeshFilter> ();
		mf.mesh = boundaryCube.mesh;
		init = true;
	}

	public void setBoundaryBounds(Bounds maxBounds) {
		if (init) {
			gameObject.transform.position = maxBounds.center;
			gameObject.transform.localScale = maxBounds.size;	
		}
	}

	public void setBoundaryLayer(string name) {
		if (init) {
			gameObject.layer = LayerMask.NameToLayer (name);
		}
	}

	public void setBoundaryMaterial(Material mat) {
		if (init) {
			gameObject.GetComponent<MeshRenderer> ().material = mat;
		}
	}

	public void turnOnCollider() {
		mc.enabled = true;
	}

	public void turnOffCollider() {
		mc.enabled = false;
	}
}
