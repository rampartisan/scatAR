using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ballJump : MonoBehaviour {
	public float thrust;
	public Rigidbody rb;
	private bool jump = false;
	public bool ballHit = false;
	// Use this for initialization
	void Start () {
		rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		if (jump) {
			rb.AddForce (new Vector3 (0, thrust, 0), ForceMode.Impulse);
			ballHit = true;
			jump = false;
		}
	}


	void OnCollisionEnter (Collision col)
	{
		jump = true;
	}



	public void setBallHit(bool ballState){
		ballHit = ballState;
	}
}
