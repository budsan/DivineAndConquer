using UnityEngine;
using System.Collections;

public class MenuControl : MonoBehaviour
{
	public UnityEngine.UI.InputField fieldName = null;
	public UnityEngine.UI.InputField fieldAddress = null;
	public UnityEngine.UI.Button findLanButton = null;
	public UnityEngine.UI.Button directConnectButton = null;
	public UnityEngine.UI.Text statusText = null;
	public string SceneToLoad = "";

	enum ConnectingState
	{
		Nothing,
		Start,
		FindingInLAN,
		Refreshing,
		Connecting,
		Disconnecting,
		JoiningRoom,
	};

	ConnectingState c_state = ConnectingState.Nothing;
	float c_timestamp;
	string clientname;
	string address;
	const string appid = "divine-and-conquer";
	bool connected = false;

	void Start()
	{

	}

	void Update()
	{
		float now = Time.time;

		BiribitClient client = Biribit.Instance;
		BiribitClient.ServerConnection[] con = client.GetConnections();
		BiribitClient.ServerInfo[] info = client.GetDiscoverInfo();

		connected = con.Length > 0;
		fieldAddress.interactable =
			fieldName.interactable = 
			directConnectButton.interactable = 
			findLanButton.interactable = !connected && c_state == ConnectingState.Nothing;

		if (connected)
		{
			statusText.text = "Connected!";
			if (!string.IsNullOrEmpty(SceneToLoad))
				Application.LoadLevel(SceneToLoad);
		}
		else
		{
			switch (c_state)
			{
				case ConnectingState.Nothing:
					statusText.text = "";
					break;
				case ConnectingState.Start:
					statusText.text = "Starting connection";
					break;
				case ConnectingState.FindingInLAN:
					statusText.text = "Finding servers in LAN";
					break;
				case ConnectingState.Refreshing:
					statusText.text = "Finding lowest ping server";
					break;
				case ConnectingState.Connecting:
					statusText.text = "Connecting server";
					break;
				case ConnectingState.Disconnecting:
					statusText.text = "Disconnecting";
					break;
				case ConnectingState.JoiningRoom:
					statusText.text = "Joining random room";
					break;
			}
		}
		
		//Connect Logic
		if (c_state != ConnectingState.Nothing)
		{
			if (connected)
			{
				BiribitClient.RemoteClient[] clients = client.GetRemoteClients(con[0].id);
				BiribitClient.Room[] rooms = client.GetRooms(con[0].id);
				uint joinedRoom = client.GetJoinedRoomId(con[0].id);

				if (c_state == ConnectingState.Connecting)
				{
					client.SetLocalClientParameters(con[0].id, clientname, appid);
					client.JoinRandomOrCreateRoom(con[0].id, 4);
					c_state = ConnectingState.JoiningRoom;
					c_timestamp = now;
				}

				if (c_state == ConnectingState.JoiningRoom)
				{
					if ((now - c_timestamp) > 3)
					{
						c_state = ConnectingState.Disconnecting;
					}
					else if (joinedRoom != BiribitClient.UnassignedId)
					{
						c_state = ConnectingState.Nothing;
					}
				}

				if (c_state == ConnectingState.Disconnecting)
				{
					client.Disconnect(con[0].id);
				}
				else
				{
					c_state = ConnectingState.Nothing;
				}
			}
			else
			{ // not connected
				if (c_state == ConnectingState.Start)
				{
					if (string.IsNullOrEmpty(address))
					{
						client.DiscoverOnLan();
						c_state = ConnectingState.FindingInLAN;
					}
					else
					{
						client.Connect(address, 0);
						c_state = ConnectingState.Connecting;
					}
					c_timestamp = now;
				}

				if (c_state == ConnectingState.FindingInLAN)
				{
					if ((now - c_timestamp) > 2)
					{
						c_timestamp = now;
						client.RefreshDiscoverInfo();
						c_state = ConnectingState.Refreshing;
					}
				}

				if (c_state == ConnectingState.Refreshing)
				{
					if ((now - c_timestamp) > 1)
					{
						if (info.Length == 0)
						{
							c_state = ConnectingState.Nothing;
						}
						else
						{
							//Finding last ping server
							uint min_ping_i = 0;
							uint min_ping = info[min_ping_i].ping;
							for (uint i = 1; i < info.Length; i++)
							{
								if (info[i].ping < min_ping && info[i].passwordProtected == 0)
								{
									min_ping = info[i].ping;
									min_ping_i = i;
								}
							}

							//And finally connecting
							BiribitClient.ServerInfo min = info[min_ping_i];
							client.Connect(min.addr, min.port);
							c_timestamp = now;
							c_state = ConnectingState.Connecting;
						}
					}
				}

				if (c_state == ConnectingState.Connecting)
				{
					if ((now - c_timestamp) > 3)
					{
						c_state = ConnectingState.Nothing;
					}
				}

				if (c_state == ConnectingState.Disconnecting)
				{
					c_state = ConnectingState.Nothing;
				}
			}
		}
	}

	public void Connect()
	{
		if (fieldName != null && !string.IsNullOrEmpty(fieldName.text))
		{
			clientname = fieldName.text;
			address = "";

			if (c_state == ConnectingState.Nothing)
				c_state = ConnectingState.Start;
		}
	}

	public void ConnectDirect()
	{
		if (fieldName != null && !string.IsNullOrEmpty(fieldName.text) &&
			fieldAddress != null && !string.IsNullOrEmpty(fieldAddress.text))
		{
			clientname = fieldName.text;
			address = fieldAddress.text;

			if (c_state == ConnectingState.Nothing)
				c_state = ConnectingState.Start;
		}
	}

	void Disconnect()
	{
		c_state = ConnectingState.Disconnecting;
	}

}
