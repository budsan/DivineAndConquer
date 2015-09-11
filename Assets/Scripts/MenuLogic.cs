using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class MenuLogic : Silver.UI.TabImmediate, BiribitListener {

	[SerializeField] private string address = "thatguystudio.com";
	[SerializeField] private string clientName = "David";
	[SerializeField] private string appId = "divine-and-conquer-0";

	public override string TabName()
	{
		return "Divine and Conquer";
	}

	private string password = "";
	private uint connectionId = Biribit.Client.UnassignedId;
	private uint joinedRoomId = Biribit.Client.UnassignedId;
	private int joinedRoomIndex = 0;
	private uint joinedRoomSlot = Biribit.Client.UnassignedId;

	private enum GameState
	{
		WaitingPlayers,
		GameRunning
	}

	private GameState gamestate = GameState.WaitingPlayers;
	private int entries_read = 0;

	private List<string> serverInfoStrings = new List<string>();
	private int serverInfoSelected = 0;
	private int slots = 4;
	private List<string> roomStrings = new List<string>();
	private int roomSelected = 0;


	private BiribitManager manager;

	override public void Start()
	{
		base.Start();

		manager = BiribitManager.Instance;
		manager.AddListener(this);
	}

	override public void DrawUI()
	{
		ui.VerticalLayout(() =>
		{
			if (this.connectionId == 0)
				DrawConnect();
			else if (joinedRoomId == Biribit.Client.UnassignedId)
				DrawJoin();
			else
				DrawRoom();
		});
	}

	public void DrawConnect()
	{
		ui.LabelField("Name:");
		clientName = ui.StringField("Name", clientName, Silver.UI.Immediate.FlagMask.NoFieldLabel);
		ui.LabelField("Address:");
		address = ui.StringField("Address", address, Silver.UI.Immediate.FlagMask.NoFieldLabel);
		ui.LabelField("Password:");
		password = ui.StringField("Password", password, Silver.UI.Immediate.FlagMask.NoFieldLabel);
		if (ui.Button("Connect"))
			manager.Connect(address, 0, password);

		Biribit.Native.ServerInfo[] serverInfoArray = manager.ServerInfo;
		if (serverInfoArray.Length > 0)
		{
			serverInfoStrings.Clear();
			foreach (Biribit.Native.ServerInfo serverInfo in serverInfoArray)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(serverInfo.name); builder.Append(", ping "); builder.Append(serverInfo.ping);
				builder.Append(serverInfo.passwordProtected != 0 ? ". Password protected." : ". No password.");
				serverInfoStrings.Add(builder.ToString());
			}
			ui.Separator(1);
			ui.LabelField("Server");
			ui.LineSeparator();
			serverInfoSelected = ui.Popup("Server", serverInfoSelected, serverInfoStrings.ToArray(), Silver.UI.Immediate.FlagMask.NoFieldLabel);
			Biribit.Native.ServerInfo info = serverInfoArray[serverInfoSelected];

			if (ui.Button("Connect selected"))
				manager.Connect(info.addr, info.port);
		}

		ui.Separator(1);
		ui.LineSeparator();
		if (ui.Button("Discover in LAN"))
			manager.DiscoverServersOnLAN();

		if (ui.Button("Clear list of servers"))
			manager.ClearServerList();

		if (ui.Button("Refresh servers"))
			manager.RefreshServerList();
	}

	public void DrawJoin()
	{
		if (ui.Button("Disconnect"))
			manager.Disconnect(connectionId);

		ui.Separator(1);
		ui.LabelField("Create room");
		ui.LineSeparator();
		ui.HorizontalLayout(() =>
		{
			slots = ui.IntField("Num slots", slots);
			if (slots < 2) slots = 2;
			if (slots > 4) slots = 4;

			if (ui.Button("Create"))
				manager.CreateRoom(connectionId, (byte)slots);
		});

		if (ui.Button("Random or create"))
		{
			manager.JoinRandomOrCreateRoom(connectionId, (byte)slots);
		}

		Biribit.Room[] roomArray = manager.Rooms(connectionId);
		if (roomArray.Length > 0)
		{
			roomStrings.Clear();
			foreach (Biribit.Room room in roomArray)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("Room "); builder.Append(room.id);

				if (room.id == joinedRoomId)
				{
					builder.Append(" | Joined: ");
					builder.Append(joinedRoomSlot);
				}

				roomStrings.Add(builder.ToString());
			}

			ui.Separator(1);
			ui.LabelField("Rooms");
			ui.LineSeparator();
			roomSelected = ui.Popup("Room", roomSelected, roomStrings.ToArray(), Silver.UI.Immediate.FlagMask.NoFieldLabel);
			Biribit.Room rm = roomArray[roomSelected];

			if (ui.Button("Join"))
				manager.JoinRoom(connectionId, rm.id);

			if (ui.Button("Refresh rooms"))
				manager.RefreshRooms(connectionId);

			ui.Separator(1);
			ui.LabelField("Room");
			ui.LineSeparator();

			for (int i = 0; i < rm.slots.Length; i++)
			{
				if (rm.slots[i] == Biribit.Client.UnassignedId)
					ui.LabelField("Slot " + i.ToString() + ": Free", 14);
				else
				{
					int pos = manager.RemoteClients(connectionId, rm.slots[i]);
					if (pos < 0)
						ui.LabelField("Slot " + i.ToString() + ": " + rm.slots[i], 14);
					else
						ui.LabelField("Slot " + i.ToString() + ": " + manager.RemoteClients(connectionId)[pos].name, 14);
				}
			}
		}
		else
		{
			if (ui.Button("Refresh rooms"))
				manager.RefreshRooms(connectionId);
		}
	}

	public void DrawRoom()
	{
		manager.JoinedRoomEntries(connectionId);

		if (ui.Button("Leave"))
			manager.JoinRoom(connectionId, 0);

		Biribit.Room[] roomArray = manager.Rooms(connectionId);
		Biribit.Room rm = roomArray[joinedRoomIndex];

		ui.Separator(1);
		ui.LabelField("Room");
		ui.LineSeparator();

		for (int i = 0; i < rm.slots.Length; i++)
		{
			int p = i + 1;
			if (rm.slots[i] == Biribit.Client.UnassignedId)
				ui.LabelField("Player " + p.ToString() + ": - - -", 14);
			else
			{
				int pos = manager.RemoteClients(connectionId, rm.slots[i]);
				if (pos < 0)
					ui.LabelField("Player " + p.ToString() + ": " + rm.slots[i], 14);
				else
					ui.LabelField("Player " + p.ToString() + ": " + manager.RemoteClients(connectionId)[pos].name, 14);
			}
		}
	}

	public void NewIncomingEntry(Biribit.Entry entry)
	{

	}

	public void ReadForEntries()
	{
		List<Biribit.Entry> entries = manager.JoinedRoomEntries(connectionId);
		for (int i = entries_read; i < entries.Count; i++)
		{
			NewIncomingEntry(entries[i]);
		}
	}

	public void OnConnected(uint _connectionId)
	{
		if (connectionId == 0)
		{
			connectionId = _connectionId;
			manager.SetLocalClientParameters(connectionId, clientName, appId);
			manager.RefreshRooms(connectionId);
		}
	}

	public void OnDisconnected(uint _connectionId)
	{
		if (connectionId == _connectionId)
			connectionId = 0;
	}

	public void OnJoinedRoom(uint _connectionId, uint roomId, byte slotId)
	{
		if (connectionId == _connectionId)
		{
			joinedRoomId = roomId;
			joinedRoomIndex = manager.Rooms(connectionId, joinedRoomId);
			joinedRoomSlot = slotId;
			entries_read = 0;

			ReadForEntries();
		}
	}

	public void OnJoinedRoomPlayerJoined(uint _connectionId, uint clientId, byte slotId)
	{
		if (connectionId == _connectionId)
		{
			Biribit.Room[] roomArray = manager.Rooms(connectionId);
			Biribit.Room rm = roomArray[joinedRoomIndex];

			int pos = manager.RemoteClients(connectionId, rm.slots[(int)slotId]);
			string name = (pos < 0) ? rm.slots[(int)slotId].ToString() : manager.RemoteClients(connectionId)[pos].name;
			Debug.Log("Player " + (slotId + 1).ToString() + ": " + name + " joined!");
		}
	}

	public void OnJoinedRoomPlayerLeft(uint _connectionId, uint clientId, byte slotId)
	{
		if (connectionId == _connectionId)
		{
		}
	}

	public void OnBroadcast(Biribit.BroadcastEvent evnt)
	{
		if (connectionId == evnt.connection)
		{
		}
	}

	public void OnEntriesChanged(uint _connectionId)
	{
		if (connectionId == _connectionId)
		{
			manager.JoinedRoomEntries(connectionId);
		}
	}

	public void OnLeaveRoom(uint _connectionId)
	{
		Debug.Log("OnLeaveRoom: " + connectionId + " " + _connectionId);
		if (connectionId == _connectionId)
		{
			joinedRoomId = Biribit.Client.UnassignedId;
			joinedRoomSlot = Biribit.Client.UnassignedId;
		}
	}
}
