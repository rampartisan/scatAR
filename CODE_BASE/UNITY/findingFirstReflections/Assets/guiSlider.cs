using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class guiSlider : MonoBehaviour {

	public float hSliderValue = 0.0f;
	public double val = 0.0f;
	private bool sliderTouched = false;

	int scaleLength = 420;
	int offSetX = 65;
	int offSetY = 50;
	int scalePosX;
	int scalePosY;
	private GameObject cam;
	void Start()
	{
		cam = GameObject.Find("Camera");
		hSliderValue = 5.0f;
	}

	void OnGUI()
	{
		if (cam.GetComponent<cameraMoveScript> ().camTurnDone) {

			scalePosX = Screen.width - scaleLength - offSetX;
			scalePosY = Screen.height - offSetY;
			GUI.Box (new Rect (scalePosX - 5, scalePosY - 40, scaleLength + 10, 80), "");
			hSliderValue = GUI.HorizontalSlider (new Rect (scalePosX, scalePosY, scaleLength, 30), hSliderValue, 0.0F, 10.0F);

			if (hSliderValue != 5.0f)
				sliderTouched = true;

			val = hSliderValue;
			val = System.Math.Round (val, 2); 
			//val = (int) hSliderValue;
			//GUI.TextField (new Rect (scalePosX + scaleLength, scalePosY - 5, 40, 20), val.ToString());

			GUI.color = Color.white;
			GUI.Label (new Rect (scalePosX, scalePosY - 40, scaleLength, 35), "Please rate the quality of the audio output, considering the dimensions \nof the room and the position of the audio source");
			GUI.Label (new Rect (scalePosX, scalePosY + 20, scaleLength, 20), "Very Unrealistic     Quite Unrealistic     Quite Realistic     Very Realistic");

			if (sliderTouched) {
				if (GUI.Button (new Rect (scalePosX + scaleLength + 10, scalePosY - 20, 50, 50), "Next")) {
					//save slidervalue and log algorithm, position and sound source
					//next scene
					sliderTouched = false;
					hSliderValue = 5.0f;
				}
			}
		}
	}
}
