using UnityEngine;
using System.Collections;

public class CopyTransform : MonoBehaviour {
	private Vector3 initHeadPos;
	private Transform headTransform;
	// Use this for initialization
	void Start () {
		headTransform = transform.FindChild ("Armature").FindChild ("Base");
		initHeadPos = headTransform.position;
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		headTransform.position = initHeadPos;
	}
}
