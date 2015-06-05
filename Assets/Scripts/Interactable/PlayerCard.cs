using UnityEngine;
using System.Collections;

public class PlayerCard : Interactable
{

	Vector3 positionToGo = Vector3.zero;

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		switch (state)
		{
			case SelectionState.Normal:
				positionToGo = Vector3.zero;
				GetComponent<Renderer>().material.SetFloat("_Bright", 1);
				GetComponent<Renderer>().material.SetFloat("_Saturation", 1);
				break;
			case SelectionState.Highlighted:
				positionToGo = new Vector3(0, 0.1f, 0);
				GetComponent<Renderer>().material.SetFloat("_Bright", 1.5f);
				GetComponent<Renderer>().material.SetFloat("_Saturation", 1);
				break;
			case SelectionState.Pressed:
				positionToGo = new Vector3(0, 0.2f, 0);
				GetComponent<Renderer>().material.SetFloat("_Bright", 2);
				GetComponent<Renderer>().material.SetFloat("_Saturation", 1);
				break;
			case SelectionState.Disabled:
				positionToGo = Vector3.zero;
				GetComponent<Renderer>().material.SetFloat("_Saturation", 0);
				break;
			default:
				positionToGo = Vector3.zero;
				break;
		}
	}
	
	void Start () {
	
	}
	
	
	void FixedUpdate ()
	{
		transform.localPosition = Vector3.Lerp(transform.localPosition, positionToGo, 0.1f);
	}
}
