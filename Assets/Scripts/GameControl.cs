using UnityEngine;
using System.Collections;

public class GameControl : MonoBehaviour
{
	public Camera MainCamera;
	public RectTransform GameUI;
	public RectTransform GameMenu;

	BiribitManager manager;
	bool showGameMenu = false;

	public void ToggleGameMenu()
	{
		showGameMenu = !showGameMenu;
	}

	void Start ()
	{
		if (MainCamera == null)
			MainCamera = Camera.main;

		manager = BiribitManager.Instance;
	}
	
	void FixedUpdate()
	{
		if (GameUI != null)
		{
			Vector2 pivot = GameUI.pivot;
			if (manager.IsConnected())
				pivot.x = Mathf.Lerp(pivot.x, 1.0f, 0.1f);
			else
				pivot.x = Mathf.Lerp(pivot.x, 0.0f, 0.1f);

			GameUI.pivot = pivot;
		}

		if (GameMenu != null)
		{
			Vector2 pivot = GameMenu.pivot;
			if (showGameMenu)
				pivot.y = Mathf.Lerp(pivot.y, 1.0f, 0.1f);
			else
				pivot.y = Mathf.Lerp(pivot.y, 0.0f, 0.1f);

			GameMenu.pivot = pivot;
		}
	}

	void Update () {
	
	}
}
