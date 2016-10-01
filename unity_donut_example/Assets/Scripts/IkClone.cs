using UnityEngine;
using System.Collections;

public class IkClone : MonoBehaviour {
	public Transform[] ikLegs = new Transform[4];
	public Transform[] realLegs = new Transform[4];
	private Transform[,,] pairs = new Transform[4, 3, 2];
	// Use this for initialization
	void Start () {
		for (int i = 0; i<4; i++) {
			Transform ik = ikLegs [i].GetChild(0);
			Transform real = realLegs [i].GetChild(0);
			pairs [i, 0, 0] = real;
			pairs [i, 0, 1] = ik;
			pairs [i, 1, 0] = real.GetChild (0);
			pairs [i, 1, 1] = ik.GetChild (0);
			pairs [i, 2, 0] = real.GetChild (0).GetChild (0);
			pairs [i, 2, 1] = ik.GetChild (0).GetChild (0);
		}
	}
	
	// Update is called once per frame
	void Update () {
		for (int i = 0; i < 4; i++) {
			for (int j = 0; j < 3; j++) {
				pairs [i, j, 0].position = pairs [i, j, 1].position;
				pairs [i, j, 0].rotation = pairs [i, j, 1].rotation;
				pairs [i, j, 0].Rotate(new Vector3(90,0,0));
			}
		}
	}
}