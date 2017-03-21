//-----------------------------------------------------------------------
// <copyright file="MeshOcclusionUIController.cs" company="Google">
//
// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Tango;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text;
using System.Globalization;

public class MeshOcclusionUIController : MonoBehaviour, ITangoLifecycle, ITangoPose
{
	/// <summary>
	/// The object that is used to test occlusion.
	/// </summary>
	[Header ("Marker Objects")]
	public GameObject m_markerObject;

	/// <summary>
	/// The Tango Camera Object.
	/// </summary>
	[Header ("Camera Objects")]
	public audioRec audioRec;

	/// <summary>
	/// The Unity Representation of the Mesh Created by Tango.
	/// </summary>
	[Header ("Mesh Objects")]
	public TangoDynamicMesh m_meshObject;

	/// <summary>
	/// The canvas panel used during mesh construction.
	/// </summary>
	[Header ("UI Elements")]
	public GameObject m_canvasParent;


	public GameObject m_meshBuildPanel;

	/// <summary>
	/// The canvas panel used for interaction after Area Description and mesh have been loaded.
	/// </summary>
	public GameObject m_meshInteractionPanel;

	/// <summary>
	/// The canvas panel used for settings on the landing screen.
	/// </summary>
	public GameObject m_settingsPanel;

	/// <summary>
	/// The debug screen.
	/// </summary>
	public GameObject m_debugPanel;

	/// <summary>
	/// The image overlay shown while waiting to relocalize to Area Description.
	/// </summary>
	public Image m_relocalizeImage;

	/// <summary>
	/// The text overlay that is shown when the Area Description or mesh is being saved.
	/// </summary>
	public Text m_savingText;

	public GameObject m_savingPanel;

	/// <summary>
	/// The parent panel that loads the selected Area Description.
	/// </summary>
	/// 
	[Header ("Area Description Loader")]
	public GameObject m_areaDescriptionLoaderPanel;

	/// <summary>
	/// The prefab of a standard button in the scrolling list.
	/// </summary>
	public GameObject m_listElement;

	/// <summary>
	/// The container panel of the Tango space Area Description scrolling list.
	/// </summary>
	public RectTransform m_listContentParent;

	/// <summary>
	/// Toggle group for the Area Description list.
	/// 
	/// You can only toggle one Area Description at a time. After we get the list of Area Description from Tango,
	/// they are all added to this toggle group.
	/// </summary>
	public ToggleGroup m_toggleGroup;

	[Header ("UI Buttons")]
	/// <summary>
/// The canvas button that changes the mesh to a visible material.
/// </summary>
public GameObject m_viewMeshButton;

	/// <summary>
	/// The canvas button that change the mesh to depth mask.
	/// </summary>
	public GameObject m_hideMeshButton;

	/// <summary>
	/// The canvas button that starts recording audio.
	/// </summary>
	public GameObject m_startRecordButton;

	/// <summary>
	/// The canvas button that stops recording audio.
	/// </summary>
	public GameObject m_stopRecordButton;

	/// <summary>
	/// The toggle within the meshInteractionPanel that brings up settings.
	/// </summary>
	public GameObject m_settingsToggle;
	private bool settingsState = false;

	/// <summary>
	/// The canvas button that shows the debug console.
	/// </summary>
	public GameObject m_showDebug;

	/// <summary>
	/// The canvas button that hides the debug console.
	/// </summary>
	public GameObject m_hideDebug;

	/// <summary>
	/// The button to create new mesh with selected Area Description, available only if an Area Description is selected.
	/// </summary>
	public Button m_createSelectedButton;

	/// <summary>
	/// The button to delete the currently selected area description
	/// </summary>
	public Button m_deleteSelectedButton;

	/// <summary>
	/// The button to export selected area description to .obj
	/// </summary>
	public Button m_exportSelectedButton;

	/// <summary>
	/// The button to begin using an Area Description and mesh. Interactable only when an Area Description with mesh is selected.
	/// </summary>
	public Button m_startGameButton;

	/// <summary>
	/// The reference to the depth mask material to be applied to the mesh.
	/// </summary>
	[Header ("Materials")]
	public Material m_depthMaskMat;

	/// <summary>
	/// The reference to the visible material applied to the mesh.
	/// </summary>
	public Material m_visibleMat;

	/// <summary>
	/// The tango dynamic mesh used for occlusion.
	/// </summary>
	private TangoDynamicMesh m_tangoDynamicMesh;

	/// <summary>
	/// The loaded mesh reconstructed from the serialized AreaDescriptionMesh file.
	/// </summary>
	private GameObject m_meshFromFile;

	/// <summary>
	/// The Area Description currently loaded in the Tango Service.
	/// </summary>
	private AreaDescription m_curAreaDescription;

	/// <summary>
	/// The AR pose controller.
	/// </summary>
	private TangoARPoseController m_arPoseController;

	/// <summary>
	/// The tango application, to create and clear 3d construction.
	/// </summary>
	private TangoApplication m_tangoApplication;

	/// <summary>
	/// The thread used to save the Area Description.
	/// </summary>
	private Thread m_saveThread;

	/// <summary>
	/// If the interaction is initialized.
	/// 
	/// Note that the initialization is triggered by the relocalization event. We don't want user to place object before
	/// the device is relocalized.
	/// </summary>
	private bool m_initialized = false;

	/// <summary>
	/// The check whether user has selected to create a new mesh or start the game with an existing one.
	/// </summary>
	private bool m_3dReconstruction = false;

	/// <summary>
	/// The check whether the menu is currently open. Determines the back button behavior.
	/// </summary>
	private bool m_menuOpen = true;

	/// <summary>
	/// The UUID of the selected Area Description.
	/// </summary>
	private string m_savedUUID;

	/// <summary>
	/// The path where the generated meshes are saved.
	/// </summary>
	private string m_meshSavePath;

	/// <summary>
	/// Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.
	/// </summary>
	public void Start ()
	{
		m_meshSavePath = Application.persistentDataPath + "/meshes";
		Directory.CreateDirectory (m_meshSavePath);

		m_arPoseController = FindObjectOfType<TangoARPoseController> ();
		m_tangoDynamicMesh = FindObjectOfType<TangoDynamicMesh> ();

		m_areaDescriptionLoaderPanel.SetActive (true);
		m_meshBuildPanel.SetActive (false);
		m_meshInteractionPanel.SetActive (false);
		m_relocalizeImage.gameObject.SetActive (false);

		// Initialize tango application.
		m_tangoApplication = FindObjectOfType<TangoApplication> ();
		if (m_tangoApplication != null) {
			m_tangoApplication.Register (this);
			if (AndroidHelper.IsTangoCorePresent ()) {
				m_tangoApplication.RequestPermissions ();
			}
		}
	}


	/// <summary>
	/// Update is called once per frame.
	/// Return to menu or quit current application when back button is triggered.
	/// </summary>
	public void Update ()
	{
		if (Input.GetKey (KeyCode.Escape)) {
			if (m_menuOpen) {
				// This is a fix for a lifecycle issue where calling
				// Application.Quit() here, and restarting the application
				// immediately results in a deadlocked app.
				AndroidHelper.AndroidQuit ();
			} else {
				reloadLevel ();
			}
		}
	}




	/// <summary>
	/// Application onPause / onResume callback.
	/// </summary>
	/// <param name="pauseStatus"><c>true</c> if the application about to pause, otherwise <c>false</c>.</param>
	public void OnApplicationPause (bool pauseStatus)
	{
		if (pauseStatus && m_initialized) {
			// When application is backgrounded, we reload the level because the Tango Service is disconected. All
			// learned area and placed marker should be discarded as they are not saved.
			#pragma warning disable 618
			Application.LoadLevel (Application.loadedLevel);
			#pragma warning restore 618
		}
	}

	/// <summary>
	/// Unity destroy function.
	/// </summary>
	public void OnDestroy ()
	{
		if (m_tangoApplication != null) {
			m_tangoApplication.Unregister (this);
		}
	}

	/// <summary>
	/// Internal callback when a permissions event happens.
	/// </summary>
	/// <param name="permissionsGranted">If set to <c>true</c> permissions granted.</param>
	public void OnTangoPermissions (bool permissionsGranted)
	{
		if (permissionsGranted) {
			m_tangoApplication.Set3DReconstructionEnabled (false);
			m_arPoseController.gameObject.SetActive (false);
			_PopulateAreaDescriptionUIList ();
		} else {
			AndroidHelper.ShowAndroidToastMessage ("Motion Tracking and Area Learning Permissions Needed");
			Application.Quit ();
		}
	}

	/// <summary>
	/// This is called when successfully connected to the Tango service.
	/// </summary>
	public void OnTangoServiceConnected ()
	{
	}

	/// <summary>
	/// This is called when disconnected from the Tango service.
	/// </summary>
	public void OnTangoServiceDisconnected ()
	{
	}

	/// <summary>
	/// OnTangoPoseAvailable event from Tango.
	/// 
	/// In this function, we only listen to the Start-Of-Service with respect to Area-Description frame pair. This pair
	/// indicates a relocalization or loop closure event happened, base on that, we either start the initialize the
	/// interaction or begin meshing if applicable.
	/// </summary>
	/// <param name="poseData">Returned pose data from TangoService.</param>
	public void OnTangoPoseAvailable (Tango.TangoPoseData poseData)
	{
		// This frame pair's callback indicates that a loop closure or relocalization has happened. 
		//
		// When learning mode is off, and an Area Description is loaded, this callback indicates a relocalization event. 
		// In our case, when the device is relocalized, user interaction is allowed and meshing starts if applicable.
		if (poseData.framePair.baseFrame ==
		      TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_AREA_DESCRIPTION &&
		      poseData.framePair.targetFrame ==
		      TangoEnums.TangoCoordinateFrameType.TANGO_COORDINATE_FRAME_START_OF_SERVICE &&
		      poseData.status_code == TangoEnums.TangoPoseStatusType.TANGO_POSE_VALID) {
			// When we get the first loop closure/ relocalization event, we initialized all the in-game interactions.
			if (!m_initialized) {
				Debug.Log ("First loop closure/relocalization");

				m_initialized = true;
				m_relocalizeImage.gameObject.SetActive (false);

				if (m_3dReconstruction) {
					m_tangoApplication.Set3DReconstructionEnabled (true);
				} else {
					m_tangoApplication.Set3DReconstructionEnabled (false);
				}
			}
		}
	}

	/// <summary>
	/// From button press: start creating a mesh for occlusion.
	/// 
	/// If an Area Description has been selected, use it and link it to the dynamic mesh.
	/// If no Area Description selected, create one while meshing.
	/// </summary>
	/// <param name="createNew">If set to <c>true</c> create new mesh and new Area Description.</param>
	public void Button_CreateAreaDescriptionMesh (bool createNew)
	{
		m_3dReconstruction = true;
		m_menuOpen = false;

		// Enable the pose controller, but disable the AR screen.
		m_arPoseController.gameObject.SetActive (true);
		m_arPoseController.gameObject.GetComponent<TangoARScreen> ().enabled = false;

		// Need to enable depth to build the mesh.
		m_tangoApplication.m_enableDepth = true;

		// Set UI panel to the mesh construction panel.
		m_areaDescriptionLoaderPanel.SetActive (false);
		m_meshBuildPanel.SetActive (true);
		m_meshInteractionPanel.SetActive (false);

		// Initialize tango application and pose controller depending on whether Area Description has been selected.
		if (createNew) {
			m_curAreaDescription = null;
			m_savedUUID = null;

			m_tangoApplication.m_areaDescriptionLearningMode = true;
			m_arPoseController.m_useAreaDescriptionPose = false;
			m_relocalizeImage.gameObject.SetActive (false);
			m_tangoApplication.Startup (null);
		} else {
			if (!string.IsNullOrEmpty (m_savedUUID)) {
				m_curAreaDescription = AreaDescription.ForUUID (m_savedUUID);
				m_tangoApplication.m_areaDescriptionLearningMode = false;
				m_arPoseController.m_useAreaDescriptionPose = true;
				m_relocalizeImage.gameObject.SetActive (true);
				m_tangoApplication.Startup (m_curAreaDescription);
			} else {
				Debug.LogError ("No Area Description loaded.");
			}
		}
	}

	/// <summary>
	/// From button press: start the game by loading the mesh and the Area Description.
	/// 
	/// Generate a new mesh from the saved area definition mesh data linked to the selected Area Description.
	/// </summary>
	public void Button_StartAreaDescriptionMesh ()
	{
		if (string.IsNullOrEmpty (m_savedUUID)) {
			AndroidHelper.ShowAndroidToastMessage ("Please choose an Area Description.");
			return;
		}

		if (!File.Exists (m_meshSavePath + "/" + m_savedUUID + ".obj")) {
			AndroidHelper.ShowAndroidToastMessage ("Please choose an Area Description with mesh data.");
			return;
		}

		m_menuOpen = false;

		// Set UI panel to the mesh interaction panel.
		m_areaDescriptionLoaderPanel.SetActive (false);
		m_meshBuildPanel.SetActive (false);
		m_savingText.text = "Loading Mesh ...";
		m_savingPanel.SetActive (true);
	

		//LOAD OBJ HERE
		loadOBJToUnity();
	}
		
	/// <summary>
	/// From button press: delete all saved meshes associated with area definitions.
	/// </summary>
	public void Button_DeleteAllAreaDescriptionMeshes ()
	{
		string[] filePaths = Directory.GetFiles (m_meshSavePath);
		foreach (string filePath in filePaths) {
			File.Delete (filePath);
		}

		AndroidHelper.ShowAndroidToastMessage ("All Area Description meshes have been deleted.");

		#pragma warning disable 618
		Application.LoadLevel (Application.loadedLevel);
		#pragma warning restore 618
	}

	/// <summary>
	/// Clear the 3D mesh on canvas button press.
	/// </summary>
	public void Button_Clear ()
	{
		m_tangoDynamicMesh.Clear ();
		m_tangoApplication.Tango3DRClear ();

		AndroidHelper.ShowAndroidToastMessage ("Mesh cleared ...");
	}


	/// <summary>
	/// Finalize the 3D mesh on canvas button press.
	/// </summary>
	public void Button_Finalize ()
	{
		m_tangoApplication.Set3DReconstructionEnabled (false);
		m_meshBuildPanel.SetActive (false);
		m_savingPanel.SetActive(true);
		m_savingText.text = ("Saving Mesh");
		StartCoroutine (_DoSaveMeshOverFrames ());
	}

	/// <summary>
	/// Set the marker object at the raycast location when the user has interacted with the image overlay.
	/// </summary>
	/// <param name="data">Event data from canvas event trigger.</param>
	public void Image_PlaceMarker (BaseEventData data)
	{
		PointerEventData pdata = (PointerEventData)data;

		// Place marker object at target point hit by raycast.
		RaycastHit hit;
		if (Physics.Raycast (Camera.main.ScreenPointToRay (pdata.position), out hit, Mathf.Infinity)) {
			m_markerObject.SetActive (true);
			m_markerObject.transform.position = hit.point;
			m_markerObject.transform.up = hit.normal;
		}
	}
		
	/// <summary>
	/// Show the mesh as visible.
	/// </summary>
	public void Button_ViewMesh ()
	{
		//m_meshFromFile.GetComponent<MeshRenderer> ().material = m_visibleMat;
    

		foreach (MeshRenderer mr in m_meshFromFile.GetComponentsInChildren<MeshRenderer>()) {
			mr.material = m_visibleMat;
		}

		m_viewMeshButton.SetActive (false);
		m_hideMeshButton.SetActive (true);
	}

	/// <summary>
	/// Set the mesh as masked.
	/// </summary>
	public void Button_HideMesh ()
	{
		//m_meshFromFile.GetComponent<MeshRenderer> ().material = m_depthMaskMat;
    
		foreach (MeshRenderer mr in  m_meshFromFile.GetComponentsInChildren<MeshRenderer>()) {
			mr.material = m_depthMaskMat;
		}


		m_viewMeshButton.SetActive (true);
		m_hideMeshButton.SetActive (false);
	}
		
	/// <summary>
	/// Exit the game and return to mesh selection.
	/// </summary>
	public void Button_Exit ()
	{
		if (audioRec.recordAudio) {
			audioRec.endRecord ();
		}
		
		#pragma warning disable 618
		Application.LoadLevel (Application.loadedLevel);
		#pragma warning restore 618
	}

	/// <summary>
	/// Populate a scrolling list with Area Descriptions. Each element will check if there is any associated
	/// mesh data tied to the Area Description by UUID. The Area Description file and linked mesh data are 
	/// loaded when starting the game.
	/// </summary>
	private void _PopulateAreaDescriptionUIList ()
	{
		// Load Area Descriptions.
		foreach (Transform t in m_listContentParent.transform) {
			Destroy (t.gameObject);
		}
    
		// Update Tango space Area Description list.
		AreaDescription[] areaDescriptionList = AreaDescription.GetList ();
    
		if (areaDescriptionList == null) {
			return;
		}
    
		foreach (AreaDescription areaDescription in areaDescriptionList) {
			GameObject newElement = Instantiate (m_listElement) as GameObject;
			MeshOcclusionAreaDescriptionListElement listElement = newElement.GetComponent<MeshOcclusionAreaDescriptionListElement> ();
			listElement.m_toggle.group = m_toggleGroup;
			listElement.m_areaDescriptionName.text = areaDescription.GetMetadata ().m_name;
			listElement.m_areaDescriptionUUID.text = areaDescription.m_uuid;
        
			// Check if there is an associated Area Description mesh.
			bool hasMeshData = File.Exists (m_meshSavePath + "/" + areaDescription.m_uuid + ".obj") ? true : false;
			listElement.m_hasMeshData.gameObject.SetActive (hasMeshData);
        
			// Ensure the lambda makes a copy of areaDescription.
			AreaDescription lambdaParam = areaDescription;
			listElement.m_toggle.onValueChanged.AddListener ((value) => _OnToggleChanged (lambdaParam, value));
			newElement.transform.SetParent (m_listContentParent.transform, false);
		}
	}

	/// <summary>
	/// Callback function when toggle button is selected.
	/// </summary>
	/// <param name="item">Caller item object.</param>
	/// <param name="value">Selected value of the toggle button.</param>
	private void _OnToggleChanged (AreaDescription item, bool value)
	{
		if (value) {
			m_savedUUID = item.m_uuid;
			m_createSelectedButton.interactable = true;
			m_deleteSelectedButton.interactable = true;
			m_curAreaDescription = AreaDescription.ForUUID (m_savedUUID);
			if (File.Exists (m_meshSavePath + "/" + item.m_uuid + ".obj")) {
				m_startGameButton.interactable = true;
				m_exportSelectedButton.interactable = true;
			} else {
				m_startGameButton.interactable = false;
				m_exportSelectedButton.interactable = false;
			}
		} else {
			m_savedUUID = null;
			m_curAreaDescription = null;
			m_createSelectedButton.interactable = false;
			m_deleteSelectedButton.interactable = false;
			m_startGameButton.interactable = false;
			m_exportSelectedButton.interactable = false;

		}
	}
		
	/// <summary>
	/// Convert a unity mesh to an Area Description mesh.
	/// </summary>
	/// <returns>The Area Description mesh.</returns>
	/// <param name="uuid">The Area Description UUID.</param>
	/// <param name="mesh">The Unity mesh.</param>
	private AreaDescriptionMesh _UnityMeshToAreaDescriptionMesh (string uuid, Mesh mesh)
	{
		AreaDescriptionMesh saveMesh = new AreaDescriptionMesh ();
		saveMesh.m_uuid = m_savedUUID;
		saveMesh.m_vertices = mesh.vertices;
		saveMesh.m_triangles = mesh.triangles;
		return saveMesh;
	}

	/// <summary>
	/// Convert an Area Description mesh to a unity mesh.
	/// </summary>
	/// <returns>The unity mesh.</returns>
	/// <param name="saveMesh">The Area Description mesh.</param>
	private Mesh _AreaDescriptionMeshToUnityMesh (AreaDescriptionMesh saveMesh)
	{
		Mesh mesh = new Mesh ();
		mesh.vertices = saveMesh.m_vertices;
		mesh.triangles = saveMesh.m_triangles;
		mesh.RecalculateNormals ();
		return mesh;
	}

	/// <summary>
	/// Serialize an Area Description mesh to file.
	/// </summary>
	/// <param name="saveMesh">The Area Description mesh to serialize.</param>
	private void _SerializeAreaDescriptionMesh (AreaDescriptionMesh saveMesh)
	{
		XmlSerializer serializer = new XmlSerializer (typeof(AreaDescriptionMesh));
		FileStream file = File.Create (m_meshSavePath + "/" + saveMesh.m_uuid);
		serializer.Serialize (file, saveMesh);
		file.Close ();
	}

	private void _SerializeAreaDescriptionMeshTemp (AreaDescriptionMesh saveMesh)
	{
		XmlSerializer serializer = new XmlSerializer (typeof(AreaDescriptionMesh));
		FileStream file = File.Create (m_meshSavePath + "/" + "tempMesh");
		serializer.Serialize (file, saveMesh);
		file.Close ();
	}


	/// <summary>
	/// Deserialize an Area Description mesh from file.
	/// </summary>
	/// <returns>The loaded Area Description mesh.</returns>
	/// <param name="uuid">The UUID of the associated Area Description.</param>
	private AreaDescriptionMesh _DeserializeAreaDescriptionMesh (string uuid)
	{
		if (File.Exists (m_meshSavePath + "/" + uuid + ".obj")) {
			XmlSerializer serializer = new XmlSerializer (typeof(AreaDescriptionMesh));
			FileStream file = File.Open (m_meshSavePath + "/" + uuid, FileMode.Open);
			AreaDescriptionMesh saveMesh = serializer.Deserialize (file) as AreaDescriptionMesh;
			file.Close ();
			return saveMesh;
		}

		return null;
	}

	/// <summary>
	/// Xml container for vertices and triangles from extracted mesh and linked Area Description.
	/// </summary>
	[XmlRoot ("AreaDescriptionMesh")]
	public class AreaDescriptionMesh
	{
		/// <summary>
		/// The UUID of the linked Area Description.
		/// </summary>
		public string m_uuid;

		/// <summary>
		/// The mesh vertices.
		/// </summary>
		public Vector3[] m_vertices;

		/// <summary>
		/// The mesh triangles.
		/// </summary>
		public int[] m_triangles;
	}


	// ADDED BY ALEX BALDWIN MARCH 2017

	private string removeCharactersForFilePath (string input)
	{
		input = input.Replace (@"/", "");
		input = input.Replace (":", "");
		input = input.Replace (" ", "");
		return input;
	}

	public void Button_placeRandomMarker() {
		RaycastHit hit;
		bool foundSomeWhere = false;
		while (!foundSomeWhere) {
			Vector3 randomVector = new Vector3 (UnityEngine.Random.Range (-1.0f, 1.0f), UnityEngine.Random.Range (-1.0f, 1.0f), UnityEngine.Random.Range (-1.0f, 1.0f));
			Ray randomRay = new Ray (Camera.main.transform.position, randomVector);
			if (Physics.Raycast (randomRay, out hit, Mathf.Infinity)) {
				m_markerObject.SetActive (true);
				m_markerObject.transform.position = hit.point;
				m_markerObject.transform.up = hit.normal;
				foundSomeWhere = true;
			}
		}
	}

	public void Button_StartRecord (){
		m_stopRecordButton.SetActive (true);
		m_startRecordButton.SetActive (false);

		if (!audioRec.recordAudio) {
			audioRec.beginRecord ();
		}
	}

	public void Button_EndRecord (){
		
		if (audioRec.recordAudio) {
			audioRec.endRecord ();
		}

		m_startRecordButton.SetActive (true);
		m_stopRecordButton.SetActive (false);
	}

	public void Button_ShowDebug (){
		
		m_hideDebug.SetActive (true);
		m_showDebug.SetActive (false);
		m_debugPanel.SetActive (true);
		m_meshObject.m_enableDebugUI = true;
	}

	public void Button_HideDebug (){

		m_showDebug.SetActive (true);
		m_hideDebug.SetActive (false);
		m_debugPanel.SetActive (false);
		m_meshObject.m_enableDebugUI = false;
	}
		
	public void toggleSettings (){
		
		settingsState = !settingsState;

		if (settingsState) {
			m_settingsPanel.SetActive (true);
		} else {
			m_settingsPanel.SetActive (false);
		}
	}

	private string waitAndGetUserInput(string placeholderText) {

		TouchScreenKeyboard keyboard = TouchScreenKeyboard.Open (placeholderText, TouchScreenKeyboardType.Default);
		while (!keyboard.done && !keyboard.wasCanceled) {

		}
		return(keyboard.text);
	}

	public void reloadLevel() {
		#pragma warning disable 618
		Application.LoadLevel (Application.loadedLevel);
		#pragma warning restore 618
	}

	public void Button_DeleteSelectedAreaDescription() {

		if (m_curAreaDescription != null) {

			string name = m_curAreaDescription.GetMetadata ().m_name;
			m_curAreaDescription.Delete ();
			m_createSelectedButton.interactable = false;
			m_deleteSelectedButton.interactable = false;
			m_startGameButton.interactable = false;
			m_exportSelectedButton.interactable = false;
			_PopulateAreaDescriptionUIList ();
			AndroidHelper.ShowAndroidToastMessage ("Deleted Area Descripton " + name);
			m_curAreaDescription = null;
			m_savedUUID = null;
		}
	}


	private IEnumerator _DoSaveMeshOverFrames() {

		int startNoUpdates = m_tangoDynamicMesh.NumQueuedMeshUpdates;

		while (m_tangoDynamicMesh.NumQueuedMeshUpdates != 0) {
			float percentage = Mathf.Abs (((m_tangoDynamicMesh.NumQueuedMeshUpdates / startNoUpdates) / startNoUpdates) * 100.0f);
			m_savingText.text = "Finalising Mesh ... " + percentage.ToString() + "%";
			yield return null;
		}
			
		StringBuilder sb = new StringBuilder();
		int startVertex = 0;
		int numMesh = m_tangoDynamicMesh.m_meshes.Values.Count;
		int count = 1;

		foreach (TangoDynamicMesh.TangoSingleDynamicMesh tmesh in m_tangoDynamicMesh.m_meshes.Values) {

			m_savingText.text = "Saving Submeshes ... " + count.ToString() + " / " + numMesh.ToString();

			Mesh mesh = tmesh.m_mesh;
			int meshVertices = 0;
			sb.Append(string.Format("g {0}\n", tmesh.name));

			// Vertices.
			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				meshVertices++;
				Vector3 v = tmesh.transform.TransformPoint(mesh.vertices[i]);

				// Include vertex colors as part of vertex point for applications that support it.
				if (mesh.colors32.Length > 0)
				{
					float r = mesh.colors32[i].r / 255.0f;
					float g = mesh.colors32[i].g / 255.0f;
					float b = mesh.colors32[i].b / 255.0f;
					sb.Append(string.Format("v {0} {1} {2} {3} {4} {5} 1.0\n", v.x, v.y, -v.z, r, g, b));
				}
				else
				{
					sb.Append(string.Format("v {0} {1} {2} 1.0\n", v.x, v.y, -v.z));
				}
			}
				
			sb.Append("\n");

			yield return null;

			// Normals.
			if (mesh.normals.Length > 0)
			{
				foreach (Vector3 n in mesh.normals)
				{
					sb.Append(string.Format("vn {0} {1} {2}\n", n.x, n.y, -n.z));
				}

				sb.Append("\n");
			}

			// Texture coordinates.
			if (mesh.uv.Length > 0)
			{
				foreach (Vector3 uv in mesh.uv)
				{
					sb.Append(string.Format("vt {0} {1}\n", uv.x, uv.y));
				}

				sb.Append("\n");
			}

			// Faces.
			int[] triangles = mesh.triangles;
			for (int j = 0; j < triangles.Length; j += 3)
			{
				sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[j + 2] + 1 + startVertex, triangles[j + 1] + 1 + startVertex, triangles[j] + 1 + startVertex));
			}

			sb.Append("\n");
			startVertex += meshVertices;
			count++;
			yield return null;
		}

		m_savingText.text = "Saving Mesh To File ... ";
		using (StreamWriter sw = new StreamWriter (m_meshSavePath + "/" + "tempMesh.obj")) {
			sw.AutoFlush = true;
			sw.Write (sb.ToString ());
			sw.Close ();
		}
	
		yield return null;
		StartCoroutine(_DoSaveAreaDescriptionAndReload());
		yield return null;
	}

	private IEnumerator _DoSaveAreaDescriptionAndReload ()
	{
		if (m_saveThread != null) {
			yield break;
		}
			
		// Disable interaction before saving.
		m_initialized = false;

		if (string.IsNullOrEmpty (m_savedUUID)) {
			m_savingText.text = "Saving Area Description ... ";

			m_saveThread = new Thread (delegate() {
				m_curAreaDescription = AreaDescription.SaveCurrent ();

				AreaDescription.Metadata metadata = m_curAreaDescription.GetMetadata ();
				string placeHolderText = "Enter A Meaningful Name For This Area";
				string userInput = waitAndGetUserInput(placeHolderText);

				if(userInput == placeHolderText) {
					userInput = DateTime.Now.ToString();
				}
				metadata.m_name = userInput;
				metadata.m_dateTime = DateTime.Now;
				m_savedUUID = m_curAreaDescription.m_uuid;    
				m_curAreaDescription.SaveMetadata (metadata);

				if(File.Exists(m_meshSavePath + "/" + "tempMesh.obj")) {
					File.Move(m_meshSavePath + "/" + "tempMesh.obj",m_meshSavePath + "/" + m_savedUUID + ".obj");
					}

				reloadLevel();
			
			});
			m_saveThread.Start ();

		} else {
			reloadLevel ();
		}
	}
		
	private bool loadOBJToUnity() {		
		string path = "file://" + m_meshSavePath + "/" + m_savedUUID + ".obj"; 
		Vector3 scale = new Vector3 (1, 1, 1);
		Vector3 translate = Vector3.zero;
		Quaternion rotate = Quaternion.identity;
		bool goPerGroup = true;
		bool subMeshPerGroup = false;
		bool usesRightHanded = true;

		StartCoroutine(DownloadAndImportFile(path, rotate, scale, translate, goPerGroup, subMeshPerGroup, usesRightHanded));
		return true;

	}

	private IEnumerator DownloadAndImportFile(string url, Quaternion rotate, Vector3 scale, Vector3 translate, bool gameObjectPerGrp, bool subMeshPerGrp, bool usesRightHanded) {
		string objString = null;

		yield return StartCoroutine(DownloadFile(url, retval => objString = retval));

		if(objString!=null && objString.Length>0) {
			yield return StartCoroutine(ObjImporter.ImportInBackground(objString, null, null, rotate, scale, translate, retval => m_meshFromFile = retval, gameObjectPerGrp, subMeshPerGrp, usesRightHanded));
			if (m_meshFromFile != null) {

				// rename the object if needed
				if (m_meshFromFile.name == "Imported OBJ file") {
					m_meshFromFile.name = "AreaDescriptonMesh";
				}
				startAfterLoad ();
			} else {
				AndroidHelper.ShowAndroidToastMessage ("Failed to load mesh");
				reloadLevel ();
			}
		}
	}

	private IEnumerator DownloadFile(string url, System.Action<string> result) {
		WWW www = new WWW(url);
		yield return www;
		if(www.error!=null) {
		} else {
		}
		result(www.text);
	}

	private void startAfterLoad() {

		for (int i = 0; i < m_meshFromFile.transform.childCount; i++) {
			GameObject child = m_meshFromFile.transform.GetChild (i).gameObject;
			child.GetComponent<MeshFilter> ().mesh.RecalculateNormals ();
			child.layer = LayerMask.NameToLayer("Occlusion");
			child.GetComponent<MeshRenderer> ().material = m_depthMaskMat;
			child.AddComponent<MeshCollider> ();
		}

		m_meshFromFile.transform.Rotate (new Vector3 (0, 180, 0));

		m_3dReconstruction = true;

		// Enable objects needed to use Area Description and mesh for occlusion.
		m_arPoseController.gameObject.SetActive (true);
		m_arPoseController.m_useAreaDescriptionPose = true;

		// Disable unused components in tango application.
		m_tangoApplication.m_areaDescriptionLearningMode = false;
		m_tangoApplication.m_enableDepth = false;


		// Load Area Description file.
		m_curAreaDescription = AreaDescription.ForUUID (m_savedUUID);

		m_savingPanel.SetActive (false);
		m_meshInteractionPanel.SetActive (true);
		m_relocalizeImage.gameObject.SetActive (true);

		m_tangoApplication.Startup (m_curAreaDescription);
	}

	private void copyMeshToSD() {

		if(File.Exists(m_meshSavePath + "/" + m_savedUUID+ ".obj")) {
			AndroidHelper.ShowAndroidToastMessage("Exporting ...");

			string path = waitAndGetUserInput("/sdcard/scatAR/Meshes");
			File.Copy(m_meshSavePath + "/" + m_savedUUID+ ".obj", path + "/" + m_curAreaDescription.GetMetadata().m_name + ".obj"); 
			AndroidHelper.ShowAndroidToastMessage("Exported " + m_curAreaDescription.GetMetadata().m_name + " to" + path);

		}

	}

}
