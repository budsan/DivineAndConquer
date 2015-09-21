using UnityEngine;
using System.Collections;

public class ARSwitch : MonoBehaviour
{
	[SerializeField] private MenuLogic logic;
	[SerializeField] private GameObject ARObject;
	[SerializeField] private GameObject NonARObject;
	private bool ObjectState;

	public void Start()
	{
		if (ARObject == null || NonARObject == null)
			return;

		if ((ARObject.activeSelf && NonARObject.activeSelf)
			|| (!ARObject.activeSelf && !NonARObject.activeSelf))
			SetState(true);
	}

	private void SetState(bool state)
	{
		ARObject.SetActive(!state);
		NonARObject.SetActive(state);

		if (NonARObject.activeSelf)
			logic.ARCardTrackedLost();
	}

	public void Toggle()
	{
		if (ARObject == null || NonARObject == null)
			return;

		ObjectState = !NonARObject.activeSelf;
		SetState(ObjectState);
	}

	void OnApplicationFocus(bool pauseStatus)
	{
		/*
#if UNITY_ANDROID
		if (pauseStatus)
		{
			ObjectState = NonARObject.activeSelf;
			SetState(true);
		}
		else
		{
			SetState(ObjectState);
		}
#endif
		*/
	}
}
