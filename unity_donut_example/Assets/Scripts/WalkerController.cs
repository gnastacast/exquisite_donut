using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ExquisiteDonut;

[RequireComponent (typeof(ParticleController))]

public class Foot {
	GameObject o;
	// Index of sprinkle a foot is targeting
	public int targetID = 0;
	// Expected ID of that sprinkle (to detect when one is removed)
	public int targetIdx = 0;

	public bool isTracking = false;
	public bool hasTarget = false;

	public Foot(GameObject _o){
		o = _o;
	}

	public Vector3 position{
		get { return o.transform.position; }
		set { o.transform.position = value; }
	}
}

public class WalkerController : MonoBehaviour {

	public GameObject[] feetObjects;
	public Foot[] feet;
	private int numFeet;
	public Transform center;
	ParticleController ctrl;
	private float footSpeed = 10f/60;
	private float maxDistance = 3;
	private float minDistance = 1;
	private Queue<int> footMoveQueue = new Queue<int>();
	private int lastFootMovedIdx = 255;
	private int movingFootIdx = 0;
	
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

	// FixedUpdate is called at a constant framerate set above
	void FixedUpdate(){
		int feetMoved = 0;
		for (int i = 0; i < feet.Length; i++) {
			Foot f = feet [i];
			// If foot is on dot
			if (f.isTracking) {
				int idx = f.targetIdx;
				// Stop tracking if our dot has dissappeared
				if (f.targetID != ctrl.sprinkles [idx].id) {
					f.isTracking = false;
					f.hasTarget = false;
					continue;
				}
				// If there is only one sprinkle, only one foot should move etc.
				if(feetMoved > ctrl.sprinkles.Count) continue;
				feetMoved++;
				// Move to dot's position or stop tracking if distance is too great.
				Vector3 targetPos = ctrl.sprinkles [idx].worldPos - center.position;
				float? dist = CanMove (f.position, targetPos);
				if (dist != null) {
					f.position = ctrl.sprinkles [idx].worldPos;
				}
				else
					f.isTracking = false;
					f.hasTarget = false;
			}
		}
		if (feet [movingFootIdx].hasTarget && ctrl.sprinkles.Count > 0) {
			Vector3 direction = ctrl.sprinkles [movingFootIdx].worldPos - feet [movingFootIdx].position;
			float mag = direction.magnitude;
			if (mag < footSpeed) {
				feet [movingFootIdx].position += direction;
				feet [movingFootIdx].isTracking = true;
			} else
				feet [movingFootIdx].position += direction / mag * footSpeed;
		} else{
			Foot foot = feet [movingFootIdx];
			Vector3 pos = foot.position;
			if (!foot.isTracking) {
				int idx = GetBestDot (pos);
				if (idx != -1) {
					foot.targetIdx = idx;
					foot.targetID = ctrl.sprinkles [idx].id;
					foot.hasTarget = true;
					movingFootIdx = footMoveQueue.Dequeue ();
					footMoveQueue.Enqueue (movingFootIdx);
				}
			}/*
			else {
				int temp = footMoveQueue.Dequeue ();
				footMoveQueue.Enqueue (temp);
				if (footMoveQueue.Peek() == movingFootIdx) {
					temp = footMoveQueue.Dequeue ();
					footMoveQueue.Enqueue (temp);
				}
			}*/
		}
	}

	// Update is called once per frame. You can use this for things that don't have to be 60fps
	void Update(){
		
	}

	// Returns distance if move is possible and null if it is not
	float? CanMove (Vector3 currentPosition, Vector3 target){
		Vector3 directionToTarget = target - currentPosition;
		Vector3 directionFromCenter = target - center.transform.position;
		Debug.DrawLine(center.transform.position,directionFromCenter.normalized*maxDistance + center.transform.position);
		Debug.DrawLine(currentPosition,directionToTarget.normalized*maxDistance + currentPosition);
		// Check if dot is within acceptable distance from center
		float dSqrToTarget = Mathf.Abs(directionFromCenter.sqrMagnitude);
		if (dSqrToTarget > minDistance * minDistance && dSqrToTarget < maxDistance * maxDistance) {
			dSqrToTarget = directionToTarget.sqrMagnitude;
			if (dSqrToTarget < maxDistance * maxDistance)
				return dSqrToTarget;
		}
		float? failVal = null;
		return failVal;

	}

	int GetBestDot (Vector3 currentPosition)
	{
		float closestDistanceSqr = Mathf.Infinity;
		int goalIdx = -1;
		for(int i = 0; i < ctrl.sprinkles.Count; i++)
		{
			Sprinkle p = ctrl.sprinkles [i];
			float? dist = CanMove (currentPosition, p.worldPos);
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
