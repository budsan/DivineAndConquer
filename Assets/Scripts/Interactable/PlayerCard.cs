using UnityEngine;
using System.Collections;

public class PlayerCard : Interactable
{
	struct State
	{
		Vector3 position;
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		switch (state)
		{
			case SelectionState.Normal:
				///
				break;
			case SelectionState.Highlighted:
				///
				break;
			case SelectionState.Pressed:
				///
				break;
			case SelectionState.Disabled:
				///
				break;
			default:
				///
				break;
		}
	}
	
	void Start () {
	
	}
	
	
	void Update () {
	
	}
}
