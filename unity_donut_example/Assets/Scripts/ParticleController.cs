﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExquisiteDonut;

[RequireComponent (typeof (Osc))]
[RequireComponent (typeof (UDPPacketIO))]

public class ParticleController : MonoBehaviour {
	// Required for OSC Donut
	private DonutCop cop;
	public SprinkleManager sprinkles;

	// Variables for projecting dots onto plane
	int w;
	int h;
	RaycastHit hit;
	Ray ray;
	private Camera cam;

	// Variables to instantiate and keep track of game objects
	public GameObject dot;
	private GameObject[] dots;

	// Counter for generating random particles
	private int counter;
	private Osc osc;
	private float maxY;

	public int numSprinkles;

	void Awake() {
		// Camera params
		cam = Camera.main;
		w = cam.pixelWidth;

		// Game settings
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = -1;
		float targetFramerate = 60;
		Time.fixedDeltaTime = 1F / targetFramerate;
		maxY = (float)cam.pixelHeight / cam.pixelWidth;

	}

	// Use this for initialization
	void Start () {
		// Required for OSC Donut
		string RemoteIP = "10.0.0.255"; //127.0.0.1 signifies a local host (if testing locally
		int SendToPort = 9000; //the port you will be sending from
		int ListenerPort = 9000; //the port you will be listening on
		UDPPacketIO udp = GetComponent<UDPPacketIO>();
		udp.init(RemoteIP, SendToPort, ListenerPort);
		osc = GetComponent<Osc>();
		osc.init(udp);
		cop = new DonutCop (osc);

		sprinkles = new SprinkleManager (cop.maxSprinkles());

		// Preallocate game objects for speed
		dots = new GameObject[cop.maxSprinkles()*2];
		for (int i = 0; i < dots.Length; i++) {
			dots [i] = Instantiate(dot);
			dots [i].transform.position.Set (0, 1000, 0);
			//dots[i].GetComponent<Renderer>().material.color =  Random.ColorHSV(0f, 1f, 1, 1, 1, 1);
		}

		// Counter for generating random sprinkles
		counter = 0;
	}

	// FixedUpdate is called at a constant framerate set above
	void FixedUpdate(){
		// Delete sprinkles set for removal
		sprinkles.ClearRemoved ();
		// Required for OSC Donut
		cop.Update (sprinkles.Count);
		// Add new sprinkles from OSC
		while(cop.HasNewSprinkles()){
			// Make new sprinkles
			Sprinkle p = cop.GetNextSprinkle ();
			sprinkles.Add(p);
		}

		// Make random sprinkles
		if (counter % 60 == 0) {
			ProduceRandomSprinkle ();
		}
		counter++;

		// Check if sprinkle is out of bounds or malformed from a bad message
		for (int i = 0;  i < sprinkles.Count; i++) {
			Sprinkle p = sprinkles [i];
			try{
				if(p.pos.x > 1 || p.pos.x < 0){
					sprinkles.Remove(p);
					cop.BroadcastSprinkle (p);
				}
				p.Update(cop.maxVelocity(),cop.maxAcceleration());
				SprinklePhysics(p);
			}
			catch{
				sprinkles.Remove (p);
				Debug.Log ("Caught malformed sprinkle at" + i);
			}
		}
		// Disable and hide all the dots that there are no sprinkles for
		for (int i = 0; i< dots.Length; i++) {
			dots[i].SetActive (false);
		}
		int activeDots = 0;
		// Enable and draw all the dots that have sprinkles attached
		for (int i = 0; i < dots.Length && i < sprinkles.Count; i++) {
			Sprinkle p = sprinkles [sprinkles.Count -1 - i];
			dots[p.id].SetActive (true);
			DrawSprinkle (p);
			activeDots++;
		}

		// Change public variable for editor window
		numSprinkles = sprinkles.Count;
		/*
		int activeDots = 0;
		for (int i = 0; i< dots.Length; i++) {
			if (dots [i].activeInHierarchy)
				activeDots++;
		}*/
		if(counter % 10 == 0) Debug.Log ("Error: " + (numSprinkles - activeDots) + " NumSprinkles: " + numSprinkles);
	}

	// Update is called once per frame. You can use this for things that don't have to be 60fps
	void Update(){
		// Fix resizing issues in editor
		w = cam.pixelWidth;
		h = cam.pixelHeight;
		maxY = (float)cam.pixelHeight / cam.pixelWidth;
	}

	// Testing function to initialize random sprinkles
	void ProduceRandomSprinkle(){
		Vector2 pos = new Vector2 (0, Random.value*maxY);
		Vector2 vel = new Vector2 ((Mathf.RoundToInt(Random.value)-.5f)*cop.maxVelocity(),0);//Random.Range(0.005f,0.01f),0);
		Vector2 acc = new Vector2(0,.001f*(Random.value*vel.y));
		Sprinkle p = new Sprinkle(pos,vel,acc, 0, 0);
		if (cop.AllowedToCreateSprinkle(sprinkles.Count)){
			sprinkles.Add (p);
			cop.MentionNewSprinkle ();
		}
	}

	void DrawSprinkle(Sprinkle p, bool debugMode = true) {
		int layerMask = 1 << 8;
		// Calculate position in world space
		Vector3 screenPos = new Vector3 (p.pos.x * w, (maxY-p.pos.y) * w, 0);
		ray = cam.ScreenPointToRay (screenPos);
		if (Physics.Raycast (ray, out hit, Mathf.Infinity, layerMask)) {
			//Debug.DrawLine(ray.origin, hit.point, new Color (0, 1, 0));
			p.worldPos = hit.point;
		}
		// Calculate velocity in world space
		Vector3 screenVel = screenPos + new Vector3(p.vel.x * w, -p.vel.y * w, 0);
		ray = cam.ScreenPointToRay (screenVel);
		if (Physics.Raycast (ray, out hit, Mathf.Infinity, layerMask)) {
			p.worldVel = hit.point - p.worldPos;
			if(debugMode) Debug.DrawLine(p.worldPos, p.worldPos + p.worldVel*10, new Color (1, 1, 0));
		}
		// Calculate velocity in world space
		Vector3 screenAcc = screenPos + new Vector3(p.acc.x * w, -p.acc.y * w, 0);
		ray = cam.ScreenPointToRay (screenAcc);
		if (Physics.Raycast (ray, out hit, Mathf.Infinity, layerMask)) {
			p.worldAcc = hit.point - p.worldPos;
			if(debugMode) Debug.DrawLine(p.worldPos, p.worldPos + p.worldAcc*500, new Color (1, 0, 0));
		}
		dots[p.id].transform.position = p.worldPos;
	}

	void EnableDot(int idx){
		dots[idx].SetActive (true);
	}

	void DisableDot(int idx){
		dots[idx].SetActive (true);
	}

	void SprinklePhysics(Sprinkle p) {
		// Reverse velocity if position is out of bounds
		if(p.pos.y<0){
			p.vel.y = Mathf.Abs(p.vel.y);
			p.acc.y = Mathf.Abs (p.acc.y) / 2;
		}
		else if(p.pos.y>maxY){
			p.vel.y = Mathf.Abs(p.vel.y)*-1;
			p.acc.y = Mathf.Abs (p.acc.y) / -2;
		}
		else{
			//p.acc.y = .0002f*(1-p.pos.y-maxY/2.0f);
			//p.acc.y = .001f*(Random.value-.5f);
			//p.acc.x = .0001f*(Random.value-.5f);
		}
	}

}
