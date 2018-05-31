using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class boundary : MonoBehaviour {
	private MeshCollider mc;
	private Mesh theMesh;
	public bool init = false;
	public bool inverse = true;
	public Vector3 size = Vector3.one;


	void Start () {
		
		theMesh = new Mesh();
		theMesh.vertices = genVerts();
		theMesh.normals = genNorms(inverse);
		theMesh.triangles = genTri (inverse);
		theMesh.RecalculateBounds();
		theMesh.RecalculateNormals();

		mc  = gameObject.AddComponent<MeshCollider> ();
		mc.sharedMesh = theMesh;
		//gameObject.AddComponent<MeshRenderer> ();
		MeshFilter mf = gameObject.AddComponent<MeshFilter> ();
		mf.mesh = theMesh;
		init = true;
		
	}
	
	void Update () {
		
	}
		
	public void setBoundaryBounds(Bounds maxBounds) {
			gameObject.transform.position = maxBounds.center;
			gameObject.transform.localScale = maxBounds.size;	
	}

	public void setBoundaryLayer(string name) {
		if (init) {
			gameObject.layer = LayerMask.NameToLayer (name);
		}
	}
	/*
	public void setBoundaryMaterial(Material mat) {
		if (init) {
			gameObject.GetComponent<MeshRenderer> ().material = mat; //don't want the boundary collider to be rendered
		}
	}*/

	public void turnOnCollider() {
		//mc.enabled = true;	
		mc.enabled = false;  //very lazy way of disregarding the boundary collider. time pressure.

	}

	public void turnOffCollider() {
		mc.enabled = false;
	}

	public void setMesh(bool inverse,Vector3 newSize) {
		size = newSize;
		theMesh.vertices = genVerts();
		theMesh.normals = genNorms(inverse);
		theMesh.triangles = genTri (inverse);
		theMesh.RecalculateBounds();
		theMesh.RecalculateNormals();
	}

	private Vector3[] genVerts() {

		Vector3 vertice_0 = new Vector3(-size.x * .5f, -size.y * .5f, size.z * .5f);
		Vector3 vertice_1 = new Vector3(size.x * .5f, -size.y * .5f, size.z * .5f);
		Vector3 vertice_2 = new Vector3(size.x * .5f, -size.y * .5f, -size.z * .5f);
		Vector3 vertice_3 = new Vector3(-size.x * .5f, -size.y * .5f, -size.z * .5f);

		Vector3 vertice_4 = new Vector3(-size.x * .5f, size.y * .5f, size.z * .5f);
		Vector3 vertice_5 = new Vector3(size.x * .5f, size.y * .5f, size.z * .5f);
		Vector3 vertice_6 = new Vector3(size.x * .5f, size.y * .5f, -size.z * .5f);
		Vector3 vertice_7 = new Vector3(-size.x * .5f, size.y * .5f, -size.z * .5f);

		Vector3[] vertices = new Vector3[]
		{
			vertice_0, vertice_1, vertice_2, vertice_0,
			vertice_7, vertice_4, vertice_0, vertice_3,
			vertice_4, vertice_5, vertice_1, vertice_0,
			vertice_6, vertice_7, vertice_3, vertice_2,
			vertice_5, vertice_6, vertice_2, vertice_1,
			vertice_7, vertice_6, vertice_5, vertice_4
		};
		return vertices;
	}

	private Vector3[] genNorms(bool inverse) {
		Vector3 up = Vector3.up;
		Vector3 down = Vector3.down;
		Vector3 front = Vector3.forward;
		Vector3 back = Vector3.back;
		Vector3 left = Vector3.left;
		Vector3 right = Vector3.right;

		Vector3[] norms = new Vector3[] {
			down, down, down, down,
			left, left, left, left,
			front, front, front, front,
			back, back, back, back,
			right, right, right, right,
			up, up, up, up
		};

		if (inverse) {
			for (int i = 0; i < norms.Length; i++) {
				norms [i] = -norms [i];
			}
			return norms;
		} else {
			return norms;
		}

	}

	private int[] genTri(bool inverse) {
		int[] triangles = new int[] {
			3, 1, 0,
			3, 2, 1,
			3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
			3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
			3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
			3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
			3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
			3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
			3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
			3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
			3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
			3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,
		};
		if (inverse) {
			System.Array.Reverse (triangles);
			return triangles;
		} else {
			return triangles;

		}


	}
}
