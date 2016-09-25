using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExquisiteDonut;

[RequireComponent (typeof(ParticleController))]

public class Foot {
	GameObject o;
	// Index of sprinkle a foot is targeting
	public int targetID = -1;
	// Expected ID of that sprinkle (to detect when one is removed)
	public int targetIdx = -1;

	public bool isTracking = false;

	public Vector3 startPos;

	public Foot(GameObject _o){
		o = _o;
		startPos = _o.transform.position;
	}

	public Vector3 position{
		get { return o.transform.position; }
		set { o.transform.position = value; }
	}
}

public class WalkerController : MonoBehaviour {

	public GameObject[] feetObjects;
	public GameObject chest;
	public Foot[] feet;
	private int numFeet;
	public Transform center;
	ParticleController ctrl;
	public float footSpeed = 60f/60;
	public float maxCenterDistance = 3;
	public float minStepDistance = 1;
	public float maxStepDistance = 5;
	private Queue<int> footMoveQueue = new Queue<int>();
	private int lastFootMovedIdx = 255;
	private int movingFootIdx = -1;
	
	void Awake() {

	}

	// Use this for initialization
	void Start () {
		numFeet = feetObjects.Length;
		feet = new Foot[numFeet];
		for(int i = 0; i<numFeet; i++){
			footMoveQueue.Enqueue(i);
			feet [i] = new Foot (feetObjects [i]);
		}
		ctrl = GetComponent<ParticleController>();
	}
	// Update is called once per frame. You can use this for things that don't have to be 60fps
	void Update(){


	}
	// FixedUpdate is called at a constant framerate set above
	void FixedUpdate(){
		int feetMoved = 0;
		for (int i = 0; i < feet.Length; i++) {
			int numSprinkles = ctrl.sprinkles.Count;
			feetMoved++;
			if (feetMoved > numSprinkles)
				continue;
			int idx = feet [i].targetIdx;
			if (idx >= 0) {
				if (feet [i].targetID != ctrl.sprinkles [feet [i].targetIdx].id)
					idx = ctrl.sprinkles.IndexByID (feet [i].targetID);
			}
			if (idx < 0) {
				feet [i].isTracking = false;
				if (!isAllowedToMove (i))
					continue;
				idx = GetBestDot (feet [i]);
				if (idx < 0)
					continue;
				feet [i].targetID = ctrl.sprinkles [idx].id;
				feet [i].targetIdx = idx;
			} else{
				feet [i].targetIdx = idx;
				Vector3 target = ctrl.sprinkles [feet [i].targetIdx].worldPos;
				if (CanMove (feet [i], target) == null) {
					feet [i].targetIdx = -1;
					feet [i].isTracking = false;
				}
			}
		}

		Vector3 averageFeet = new Vector3();
		int stableFeet = 0;
		// Move particles with targets
		for (int i = 0; i < feet.Length; i++) {
			Foot f = feet [i];
			averageFeet += f.position;
			stableFeet++;
			int idx = f.targetIdx;
			// If foot is on dot
			if (idx >= 0) {
				Vector3 direction = ctrl.sprinkles [idx].worldPos - f.position;
				if (direction.magnitude < footSpeed) {
					averageFeet += f.position;
					stableFeet++;
					f.position = ctrl.sprinkles [idx].worldPos;
					f.isTracking = true;
					continue;
				} else {
					f.position += direction.normalized * footSpeed;
					f.isTracking = false;
					continue;
				}
			} else {
				Vector3 direction = chest.transform.position - f.position;
				direction.Set(direction.x, 0, direction.z);
				f.position += direction * footSpeed / 20f;
			}
		}
		// Move chest
		if(stableFeet > 0)
			averageFeet *= 1f/stableFeet;
		averageFeet.Set(averageFeet.x, 2, averageFeet.z);
		chest.transform.position += (averageFeet - chest.transform.position)/10f;
	}

	bool isAllowedToMove(int idx){
		bool retval;
		int numNotTracking = 0;
		for(int i = 1; i < feet.Length; i+=2){
			int nextIndex = (i+idx) % feet.Length;
			Foot f = feet [nextIndex];
			if (!f.isTracking && f.targetIdx != -1) {
				numNotTracking++;
			}
		}
		if(numNotTracking > 0) return false;
		return true;
	}

	// Returns distance if move is possible and null if it is not
	float? CanMove (Foot f, Vector3 target, float minStep = 0, bool debugMode = false){
		float? failVal = null;
		Vector3 currentPosition = f.position;
		Vector3 directionToTarget = target - currentPosition;
		//Vector3 directionFromCenter = target - (f.startPos+chest.transform.position);
		Vector3 directionFromCenter = target - (f.startPos+center.transform.position);
		if (debugMode) {
			Debug.DrawLine (center.transform.position, directionFromCenter.normalized * maxCenterDistance + center.transform.position);
			Debug.DrawLine (currentPosition, directionToTarget.normalized * maxCenterDistance + currentPosition);
		}
		// Check if dot is within acceptable distance from center
		float dSqrToTarget = Mathf.Abs(directionFromCenter.sqrMagnitude);
		if (dSqrToTarget < maxCenterDistance * maxCenterDistance) {
			if (dSqrToTarget < maxStepDistance * maxStepDistance && dSqrToTarget > minStep * minStep)
				return dSqrToTarget;
		}
		return failVal;

	}

	int GetBestDot (Foot f)
	{
		float closestDistanceSqr = Mathf.Infinity;
		int goalIdx = -1;
		for(int i = 0; i < ctrl.sprinkles.Count; i++)
		{
			Sprinkle p = ctrl.sprinkles [i];
			float mag = Vector3.Magnitude (p.worldPos - f.position);
			float t = mag / footSpeed;
			Vector3 deltaPos = p.worldVel * t + p.worldAcc * t * t *.5f;
			float? dist = CanMove (f, p.worldPos+deltaPos,minStepDistance);
			//float? dist = CanMove (f, p.worldPos,minStepDistance);
			if (dist != null && dist<closestDistanceSqr){
				closestDistanceSqr = (float)dist;
				goalIdx = i;
			}
		}
		return goalIdx;
	}

	float IntegrateMotion (float timeout){
		for (float t = 0f; t < timeout; t += 1 / 60) {

		}
		return -1f;
	}
}
