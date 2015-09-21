using System;
using UnityEngine;
using Vuforia;

public class DivineTrackableEventHandler : MonoBehaviour, ITrackableEventHandler
{
	public int cardIndex = 0;
	public MenuLogic logic = null;
	private TrackableBehaviour mTrackableBehaviour;

	void Start()
	{
		mTrackableBehaviour = GetComponent<TrackableBehaviour>();
		if (mTrackableBehaviour)
		{
			mTrackableBehaviour.RegisterTrackableEventHandler(this);
		}
	}

	public void OnTrackableStateChanged(
									TrackableBehaviour.Status previousStatus,
									TrackableBehaviour.Status newStatus)
	{
		if (newStatus == TrackableBehaviour.Status.DETECTED ||
			newStatus == TrackableBehaviour.Status.TRACKED ||
			newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
		{
			OnTrackingFound();
		}
		else if (previousStatus == newStatus)
		{

		}
		else
		{
			OnTrackingLost();
		}
	}

	private void OnTrackingFound()
	{
		logic.NewARCardTracked(cardIndex, gameObject);
		Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " found");
	}

	private void OnTrackingLost()
	{
		logic.ARCardTrackedLost();
		Debug.Log("Trackable " + mTrackableBehaviour.TrackableName + " lost");
	}
}