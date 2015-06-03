using UnityEngine;
using System.Collections;

public class RotateCamera : MonoBehaviour
{
	public float SpeedInverse = 0.01f;

	void Update () {
		transform.Rotate(Vector3.up, 2 * Mathf.PI * SpeedInverse);
	}
}
