using UnityEngine;
using System.Collections;

public class GameControl : MonoBehaviour
{
	public Camera MainCamera;
	public RectTransform GameUI;
	public RectTransform GameMenu;
	public GameObject Playground;

	RotateCamera m_rotateCamera = null;
	BiribitManager m_manager;
	bool m_showGameMenu = false;
	int m_followingPlayer = -1;

	Transform m_playerContainer = null;
	Transform m_cameraPosition = null;

	public void ToggleGameMenu()
	{
		m_showGameMenu = !m_showGameMenu;
	}

	void Start ()
	{
		if (MainCamera == null)
			MainCamera = Camera.main;

		if (MainCamera != null)
			m_rotateCamera = MainCamera.GetComponent<RotateCamera>();

		m_manager = BiribitManager.Instance;
	}
	
	void FixedUpdate()
	{
		if (GameUI != null)
		{
			Vector2 pivot = GameUI.pivot;
			if (m_manager.IsConnected() && m_manager.HasJoinedRoom())
				pivot.x = Mathf.Lerp(pivot.x, 1.0f, 0.1f);
			else
				pivot.x = Mathf.Lerp(pivot.x, 0.0f, 0.1f);

			GameUI.pivot = pivot;
		}

		if (GameMenu != null)
		{
			Vector2 pivot = GameMenu.pivot;
			if (m_showGameMenu)
				pivot.y = Mathf.Lerp(pivot.y, 1.0f, 0.1f);
			else
				pivot.y = Mathf.Lerp(pivot.y, 0.0f, 0.1f);

			GameMenu.pivot = pivot;
		}

		if (m_cameraPosition != null)
		{
			MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, m_cameraPosition.position, 0.1f);
			MainCamera.transform.rotation = Quaternion.Lerp(MainCamera.transform.rotation, m_cameraPosition.rotation, 0.1f);
		}
		else
		{
			Vector3 initPos = new Vector3(0, 3, 0);
			MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, initPos, 0.1f);
		}
	}

	void Update ()
	{
		bool connected = m_manager.IsConnected();
		if (m_rotateCamera != null)
			m_rotateCamera.enabled = !connected;

		if (connected)
		{
			uint localPlayer = m_manager.GetLocalPlayer();
			if (m_followingPlayer != localPlayer)
			{
				if (Playground != null)
				{
					string playerName = "Player" + (localPlayer + 1);
					Debug.Log("Finding " + playerName);
					m_playerContainer = Playground.transform.Find("Player " + (localPlayer + 1));
					if (m_playerContainer != null)
						m_cameraPosition = m_playerContainer.Find("CameraPosition");
					else
						m_cameraPosition = null;
				}
			}
		}
		else
		{
			m_followingPlayer = -1;
			m_playerContainer = null;
			m_cameraPosition = null;
		}
		
	}
}
