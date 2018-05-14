using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Text;
using System.IO;
using System;

public class testControl : MonoBehaviour {

	public GameObject ball;
	public GameObject character;
	public GameObject listener;
	public GameObject soundEmitter;

	public float hSliderValue = 0.0f;
	public double val = 0.0f;
	public int AB = 1;
	public int participantNumber = 1;
	private bool sliderTouched = false;

	int scaleLength = 420;
	int offSetX = 65;
	int offSetY = 50;
	int scalePosX;
	int scalePosY;
	private GameObject cam;

	private int caseSwitch = 0;
	private int count = 0;
	private int stringArrayCount = 0;
	private int soundSource = 0;
	private string algorithm;
	private string pos;
	private string soundType; 

	private string[] array;
	private string time;
	private bool nextScene = false;

	private int[] randomCaseSwitch = new int[9];
	private bool endTest = false;
	 
	void Start()
	{
		cam = GameObject.Find ("Camera");
		hSliderValue = 5.0f;
		array = new string[18];
		time = DateTime.Now.Minute.ToString();
		//random indexes for first case
		RandomUnique ();

		if (AB == 1) {
			ball.transform.position = new Vector3 (10.0f, 0.0f, 10.0f);
			soundSource = 0;
			caseSwitch = randomCaseSwitch [count];
		} else if (AB == 2) {
			character.transform.position = new Vector3 (10.0f, 0.0f, 10.0f);
			soundSource = 1;
			caseSwitch = randomCaseSwitch [count];
		}

		endTest = false;
		//Debug.Log (randomCaseSwitch [0] + "" + randomCaseSwitch [1] + "" + randomCaseSwitch [2] + "" + randomCaseSwitch [3] + "" + randomCaseSwitch [4] + "" + randomCaseSwitch [5] + "" + randomCaseSwitch [6] + "" + randomCaseSwitch [7] + "" + randomCaseSwitch [8]);
	}

	void Update() 
	{
		if(soundSource == 1 && caseSwitch == 6)//odd error causes the position assignment not to work in case 
			soundEmitter.transform.position = new Vector3 (-2.01f, 1.4f, -1.18f);
		//Control Scenarios
		if (nextScene) {
			soundEmitter.GetComponent<audioTrigger> ().setAudioPlay (true);
			nextScene = false;
			switch (soundSource) {
			case 0:
				soundType = "speech";
				ball.transform.position = new Vector3 (10.0f, 0.0f, 10.0f);
				//disable jumping
				switch (caseSwitch) {
				case 0: 
					choosePosition (0);
					chooseAlgorithm (0);
					pos = "1";
					algorithm = "sdn";
					break;
				case 1:
					choosePosition (1);
					chooseAlgorithm (1);
					pos = "2";
					algorithm = "wgw";
					break;
				case 2:
					choosePosition (2);
					chooseAlgorithm (2);
					pos = "3";
					algorithm = "ach";
					break;
				case 3: 
					choosePosition (0);
					chooseAlgorithm (1);
					pos = "1";
					algorithm = "wgw";
					break;
				case 4:
					choosePosition (1);
					chooseAlgorithm (2);
					pos = "2";
					algorithm = "ach";
					break;
				case 5:
					choosePosition (2);
					chooseAlgorithm (0);
					pos = "3";
					algorithm = "sdn";
					break;
				case 6: 
					choosePosition (0);
					chooseAlgorithm (2);
					pos = "1";
					algorithm = "ach";
					break;
				case 7:
					choosePosition (1);
					chooseAlgorithm (0);
					pos = "2";
					algorithm = "sdn";
					break;
				case 8:
					choosePosition (2);
					chooseAlgorithm (1);
					pos = "3";
					algorithm = "wgw";
					break;
				}
				break;
			case 1:
				soundType = "ball";
				character.transform.position = new Vector3 (10.0f, 0.0f, 10.0f);
				switch (caseSwitch) {
				case 0: 
					choosePosition (3);
					chooseAlgorithm (0);
					pos = "1";
					algorithm = "sdn";
					break;
				case 1:
					choosePosition (4);
					chooseAlgorithm (1);
					pos = "2";
					algorithm = "wgw";
					break;
				case 2:
					choosePosition (5);
					chooseAlgorithm (2);
					pos = "3";
					algorithm = "ach";
					break;
				case 3: 
					choosePosition (3);
					chooseAlgorithm (2);
					pos = "1";
					algorithm = "ach";
					break;
				case 4:
					choosePosition (4);
					chooseAlgorithm (0);
					pos = "2";
					algorithm = "sdn";
					break;
				case 5:
					choosePosition (5);
					chooseAlgorithm (1);
					pos = "3";
					algorithm = "wgw";
					break;
				case 6: 
					choosePosition (3);
					chooseAlgorithm (1);
					pos = "1";
					algorithm = "wgw";
					break;
				case 7:
					choosePosition (4);
					chooseAlgorithm (2);
					pos = "2";
					algorithm = "ach";
					break;
				case 8:
					choosePosition (5);
					chooseAlgorithm (0);
					pos = "3";
					algorithm = "sdn";
					break;
				}
				break;
			}
		}
	}

	void OnGUI()
	{
		if (!endTest) {
			if (cam.GetComponent<cameraMoveScript> ().camTurnDone) {

				scalePosX = Screen.width - scaleLength - offSetX;
				scalePosY = Screen.height - offSetY;
				GUI.Box (new Rect (scalePosX - 5, scalePosY - 40, scaleLength, 80), "");
				hSliderValue = GUI.HorizontalSlider (new Rect (scalePosX, scalePosY, scaleLength - 10, 30), hSliderValue, 0.0F, 10.0F);

				if (hSliderValue != 5.0f)
					sliderTouched = true;

				val = hSliderValue;
				val = System.Math.Round (val, 2); 
				//val = (int) hSliderValue;
				//GUI.TextField (new Rect (scalePosX + scaleLength, scalePosY - 5, 40, 20), val.ToString());

				GUI.color = Color.white;
				//GUI.Label (new Rect (scalePosX, scalePosY - 40, scaleLength, 35), "Please rate the quality of the audio output, considering the dimensions \nof the room and the position of the audio source");
				//GUI.Label (new Rect (scalePosX, scalePosY + 20, scaleLength, 20), "Very Unrealistic     Quite Unrealistic     Quite Realistic     Very Realistic");
				GUI.Label (new Rect (scalePosX, scalePosY - 30, scaleLength, 20), "Please rate how well you think the audio output suits the environment");
				GUI.Label (new Rect (scalePosX, scalePosY + 20, scaleLength, 20), "Very Poorly           Quite Poorly              Quite Well              Very Well");


				if (sliderTouched) {
					if (GUI.Button (new Rect (scalePosX + scaleLength + 10, scalePosY - 20, 50, 50), "Next")) {
						//save slidervalue and log algorithm, position and sound source
						if (soundSource == 0) {
							array [count] = soundType + "," + pos + "," + algorithm + "," + val.ToString ();
							stringArrayCount += 1;
						} else if (soundSource == 1) {
							array [count + 9] = soundType + "," + pos + "," + algorithm + "," + val.ToString ();
							stringArrayCount += 1;
						}

						//next scene
						count += 1;

						if (count < 9)
							caseSwitch = randomCaseSwitch [count]; //count don't reach zero for second case
					
						if (AB == 1) {
							if (count > 8 && soundSource == 0) {
								soundSource = 1;
								count = 0;
	
								//random integers for second case
								RandomUnique ();
								caseSwitch = randomCaseSwitch [count];
							} else if (count > 7 && soundSource == 1) {
								count = 8;
							}
						} else if (AB == 2) {
							if (count > 8 && soundSource == 1) {
								soundSource = 0;
								count = 0;

								//random integers for second case
								RandomUnique ();
								caseSwitch = randomCaseSwitch [count];
							} else if (count > 8 && soundSource == 0) {
								count = 8;
							}
						}
						nextScene = true;

						//Debug.Log (stringArrayCount);
						if (stringArrayCount > 17) {
							for (int i = 0; i < array.Length; ++i) {
								Debug.Log (array [i]);
							}
							SaveInventory (); //also quit application
							endTest = true;
						}

						sliderTouched = false;
						hSliderValue = 5.0f;
					}
				}
			}
		} else {
			GUI.Box (new Rect (Screen.width/2 - scaleLength/2, Screen.height/2-10, scaleLength, 40), "");
			GUI.Label (new Rect (Screen.width/2 - scaleLength/2, Screen.height/2, scaleLength, 20), "That's it! Thank you for your participation.");
		}
	}

	public void choosePosition (int val){
		//character positions
		if (val == 0) {
			character.transform.position = new Vector3 (-2.01f, 0.0f, -1.18f);
			character.transform.rotation = Quaternion.Euler (0, 66, 0);
			soundEmitter.transform.position = new Vector3 (-2.01f, 1.4f, -1.18f);
			listener.transform.position = new Vector3 (1.67f, 1.85f, 0.98f);
			listener.transform.rotation = Quaternion.Euler (0, 226, 0);
		} else if (val == 1) {
			character.transform.position = new Vector3 (-1.26f, 0.0f, -1.19f);
			character.transform.rotation = Quaternion.Euler (0, -18, 0);
			soundEmitter.transform.position = new Vector3 (-1.26f, 1.4f, -1.19f);
			listener.transform.position = new Vector3 (-2.1f, 1.85f, 2.47f);
			listener.transform.rotation = Quaternion.Euler (0, 147, 0);
		} else if (val == 2) {
			/*
			character.transform.position = new Vector3 (2.36f, 0.0f, 1.74f);
			character.transform.rotation = Quaternion.Euler (0, 240, 0);
			soundEmitter.transform.position = new Vector3 (2.36f, 1.4f, 1.74f);
			listener.transform.position = new Vector3 (-4.02f, 1.85f, -1.54f);
			listener.transform.rotation = Quaternion.Euler (0, 80, 0);
			*/
			character.transform.position = new Vector3 (-1.73f, 0.0f, -0.55f);
			character.transform.rotation = Quaternion.Euler (0, -40, 0);
			soundEmitter.transform.position = new Vector3 (-1.73f, 1.4f, -0.55f);
			listener.transform.position = new Vector3 (-4.2f, 1.85f, 2.4f);
			listener.transform.rotation = Quaternion.Euler (0, 122, 0);
			//ball positions
		} else if (val == 3) {
			ball.transform.position = new Vector3 (-2.01f, 0.5f, -1.18f);
			soundEmitter.transform.position = new Vector3 (-2.01f, 1.4f, -1.18f);
			listener.transform.position = new Vector3 (1.67f, 1.85f, 0.98f);
			listener.transform.rotation = Quaternion.Euler (0, 226, 0);
		} else if (val == 4) {
			ball.transform.position = new Vector3 (-1.26f, 0.5f, -1.19f);
			soundEmitter.transform.position = new Vector3 (-1.26f, 1.4f, -1.19f);
			listener.transform.position = new Vector3 (-2.1f, 1.85f, 2.47f);
			listener.transform.rotation = Quaternion.Euler (0, 147, 0);

		} else if (val == 5) {
			ball.transform.position = new Vector3 (-1.73f, 0.5f, -0.55f);
			soundEmitter.transform.position = new Vector3 (-1.73f, 1.4f, -0.55f);
			listener.transform.position = new Vector3 (-4.2f, 1.85f, 2.4f);
			listener.transform.rotation = Quaternion.Euler (0, 122, 0);
		}
	}

	public void chooseAlgorithm (int val){
		if (val == 0) {
			soundEmitter.GetComponent<SDN> ().enabled = true;
			//soundEmitter.GetComponent<WGW> ().enabled = false;
			soundEmitter.GetComponent<generateWGWIR> ().enabled = false;
			soundEmitter.GetComponent<ConvolutionReverbUploadIR> ().enabled = false;
			soundEmitter.GetComponent<ResonanceAudioSource> ().gainDb = 15.0f;
			soundEmitter.GetComponent<adjustParams> ().setWetLevel(0.0f);
		} else if (val == 1) {
			soundEmitter.GetComponent<SDN> ().enabled = false;
			//soundEmitter.GetComponent<WGW> ().enabled = true;
			soundEmitter.GetComponent<generateWGWIR> ().enabled = true;
			soundEmitter.GetComponent<ConvolutionReverbUploadIR> ().enabled = true;
			soundEmitter.GetComponent<ResonanceAudioSource> ().gainDb = 0.0f;
			soundEmitter.GetComponent<adjustParams> ().setWetLevel(100.0f);
			soundEmitter.GetComponent<adjustParams> ().setUseSample(2.0f);
		} else if (val == 2) {
			soundEmitter.GetComponent<SDN> ().enabled = false;
			//soundEmitter.GetComponent<WGW> ().enabled = false;
			soundEmitter.GetComponent<generateWGWIR> ().enabled = false;
			soundEmitter.GetComponent<ConvolutionReverbUploadIR> ().enabled = false;
			soundEmitter.GetComponent<ResonanceAudioSource> ().gainDb = 0.0f;
			soundEmitter.GetComponent<adjustParams> ().setWetLevel(0.0f);
		}
	}

	void SaveInventory ()
	{
		string filePath = getPath ();
		//This is the writer, it writes to the filepath
		StreamWriter writer = new StreamWriter (filePath);

		for (int i = 0; i < array.Length; ++i) {
			writer.WriteLine (array[i]);
		}

		writer.Flush ();
			//This closes the file
		writer.Close ();
	}

	private string getPath ()
	{
		string filename = participantNumber.ToString()+"Participant"+ time +".csv";
		#if UNITY_EDITOR
		return Application.dataPath + "/CSV/" + filename;
		#elif UNITY_ANDROID
		return Application.persistentDataPath+"Saved_Inventory.csv";
		#elif UNITY_IPHONE
		return Application.persistentDataPath+"/"+"Saved_Inventory.csv";
		#else
		return Application.dataPath +"/"+"Saved_Inventory.csv";
		#endif
		}

	public void setScene(bool next){
		nextScene = next;
	}

	private void RandomUnique()
	{
		for (int i = 0; i < 9; i++) {
		randomCaseSwitch [i] = i;
		}
		Shuffle (randomCaseSwitch);
	}

	public void Shuffle(int[] obj)
	{
		for (int i = 0; i < obj.Length; i++) {
			int temp = obj [i];
			int objIndex = UnityEngine.Random.Range (0, obj.Length);
			obj [i] = obj [objIndex];
			obj [objIndex] = temp;
		}
	}

	public int getSoundType(){
		return soundSource;
	}
}
