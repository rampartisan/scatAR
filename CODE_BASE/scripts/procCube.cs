using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class procCube : MonoBehaviour {

	public Mesh mesh;
	public Vector3 size = Vector3.one;

	void Start() {
		bool inverse  = false;
		mesh = new Mesh();
		mesh.vertices = genVerts();
		mesh.normals = genNorms(inverse);
		mesh.triangles = genTri (inverse);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
	}

	public void setMesh(bool inverse,Vector3 newSize) {
		size = newSize;
		mesh.vertices = genVerts();
		mesh.normals = genNorms(inverse);
		mesh.triangles = genTri (inverse);
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
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
