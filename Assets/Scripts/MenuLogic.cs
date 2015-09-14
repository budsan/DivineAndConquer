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

	private Divine.State m_game = null;
	private int[] m_playerSet = new int[0];
	private int FindPlayerIndex(byte slotId)
	{
		int player = Divine.Player.NoPlayerIndex;
		for (int i = 0; i < m_playerSet.Length; i++)
			if (slotId == m_playerSet[i])
			{
				player = i;
				break;
			}
				

		return player;
	}

	private enum GameState
	{
		WaitingPlayers,
		GameRunning
	}

	private GameState gamestate = GameState.WaitingPlayers;
	private int entries_read = 1;

	private List<string> serverInfoStrings = new List<string>();
	private int serverInfoSelected = 0;
	private int slots = 4;
	private List<string> roomStrings = new List<string>();
	private int roomSelected = 0;

	private BiribitManager manager;

	static private string[] randomNames = {
		"Jesus",
		"David",
		"Goliath",
		"Isaac",
		"Abraham",
		"Adam",
		"Eva",
		"Maria"
	};

	override public void Start()
	{
		base.Start();

		manager = BiribitManager.Instance;
		manager.AddListener(this);

		clientName = PlayerPrefs.GetString("clientName");
		if (string.IsNullOrEmpty(clientName))
		{
			System.Random ran = new System.Random();
			clientName = randomNames[ran.Next() % randomNames.Length];
		}
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

		if (m_game == null || m_game.GetWinnerPlayerIndex() != Divine.Player.NoPlayerIndex)
		{
			if (m_game != null)
			{
				int winnerPlayer = m_game.GetWinnerPlayerIndex();
				int winnerSlot = m_playerSet[winnerPlayer];
				int winnerClientIndex = manager.RemoteClients(connectionId, rm.slots[winnerSlot]);
				string winnerName = (winnerClientIndex < 0) ?
					rm.slots[winnerSlot].ToString() :
					manager.RemoteClients(connectionId)[winnerClientIndex].name;
				ui.LabelField("Player " + (winnerPlayer + 1).ToString() + "(" + winnerName + ") has win!");
			}

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

			if (ui.Button("Start Game"))
			{
				List<int> playerSet = new List<int>();
				for(int i = 0; i < rm.slots.Length && playerSet.Count < 4; i++)
					if (rm.slots[i] != Biribit.Client.UnassignedId)
						playerSet.Add(i);

				manager.SendEntry(connectionId, 
					DivineSerializator.Start(
						(int)(new DateTime().Ticks & 0xFFFFFFFF),
						playerSet.ToArray()
					)
				);
			}
		}
		else
		{
			DrawGame(rm);
		}
	}

	public void DrawGame(Biribit.Room joinedRoom)
	{
		int player = FindPlayerIndex((byte)manager.JoinedRoomSlot(connectionId));
		if (player != Divine.Player.NoPlayerIndex)
		{
			DrawPlayer(player);

			bool[] draw = m_game.GetPlayersNeedToDraw();
			if (draw[player])
			{
				ui.Separator(1);
				ui.LabelField("You need to draw cards");
				ui.LineSeparator();
				int drawedCardIndex = 0;
				if (DrawNumeric(1, m_game.DeckSize, ref drawedCardIndex, (int index) => {
					return m_game.GetCard(index - 1).BelongsTo == Divine.Player.NoPlayerIndex;
				}))
					manager.SendEntry(connectionId, DivineSerializator.DrawCard(drawedCardIndex-1));
			}
			else
			{
				bool first = true;
				for (int i = 0; i < draw.Length; i++)
				{
					if (draw[i])
					{
						if (first)
							first = false;

						int drawSlot = m_playerSet[i];
						int drawClientIndex = manager.RemoteClients(connectionId, joinedRoom.slots[drawSlot]);
						string drawName = (drawClientIndex < 0) ?
							joinedRoom.slots[drawSlot].ToString() :
							manager.RemoteClients(connectionId)[drawClientIndex].name;

						ui.LabelField("Player " + (i + 1).ToString() + "(" + drawName + ") needs to draw!");
					}
				}

				if (first)
				{
					ui.Separator(1);
					ui.LabelField("Turn action");
					ui.LineSeparator();

					int playerTurn = m_game.GetWhoseTurnIsIt();
					if (player != playerTurn)
					{
						int turnSlot = m_playerSet[playerTurn];
						int turnClientIndex = manager.RemoteClients(connectionId, joinedRoom.slots[turnSlot]);
						string drawName = (turnClientIndex < 0) ?
							joinedRoom.slots[turnSlot].ToString() :
							manager.RemoteClients(connectionId)[turnClientIndex].name;

						ui.LabelField("It's player " + (playerTurn + 1).ToString() + "(" + drawName + ") turn!", 14);
					}
					else
					{
						DrawTurn();
					}
				}
			}
		}
		else
		{
			ui.LabelField("Wait players to finish...");
		}
	}

	public void DrawPlayer(int playerIndex)
	{
		Divine.PlayerView player = m_game.GetPlayer(playerIndex);

		ui.Separator(1);
		ui.LabelField("Hand");
		ui.LineSeparator();
		for (int i = 0; i < player.Hand.Length; i++)
		{
			if (player.Hand[i] == Divine.Card.NoCardIndex)
				ui.LabelField("- Empty", 14);
			else
			{
				Divine.CardView card = m_game.GetCard(player.Hand[i]);
				string bounceStr = (card.BounceTo == Divine.Card.NoCardIndex) ?
					"" : "Bounce to card " + (card.BounceTo + 1).ToString();

				ui.LabelField(Enum.GetName(typeof(Divine.CardType), card.Type) +
					" (" + (player.Hand[i] + 1).ToString() + ")" + bounceStr, 14);
			}
		}

		ui.Separator(1);
		ui.LabelField("Orations");
		ui.LineSeparator();
		ui.HorizontalLayout(() => {
			for (int i = 0; i < player.Orations.Length; i++)
				ui.Toggle((i + 1).ToString(), player.Orations[i]);
		});
	}

	public void DrawTurn()
	{

	}

	public delegate bool DrawNumericEnableDelegate(int value);

	public bool DrawNumeric(int rangeBegin, int rangeEnd, ref int result, DrawNumericEnableDelegate check = null)
	{
		Silver.UI.Immediate.LayoutElementDescription layout = new Silver.UI.Immediate.LayoutElementDescription();
		layout.flexibleWidth = 1;
		layout.minHeight = 40;

		bool clicked = false;
		for(int i = 0; i <= (rangeEnd - rangeBegin); i++)
		{
			if ((i % 3) == 0)
			{
				if (i != 0)
					ui.EndHorizontalLayout();
				ui.BeginHorizontalLayout();
			}

			Silver.UI.Immediate.FlagMask mask = Silver.UI.Immediate.FlagMask.None;
			if (check != null)
				mask = check(i + rangeBegin) ? mask : mask | Silver.UI.Immediate.FlagMask.NoInteractable;

			ui.NextLayoutElement = layout;
			if (ui.Button((i + rangeBegin).ToString(), mask))
			{
				result = i + rangeBegin;
				clicked = true;
			}
		}

		ui.EndHorizontalLayout();
		return clicked;
	}

	public void NewIncomingEntry(Biribit.Entry entry)
	{
		DivineSerializator.Deserialize(entry.data,
			(int seed, int[] playerSet) => //OnStart
			{
				m_game = new Divine.State();
				m_game.StartGame(seed, playerSet.Length);
				m_playerSet = playerSet;
			},
			(int cardIndex, int[] extraParams) => //OnUseCard
			{
				int player = FindPlayerIndex(entry.slot_id);
				m_game.UseCard(player, cardIndex, extraParams);
			},
			(int cardIndex) => //OnDrawCard
			{
				int player = FindPlayerIndex(entry.slot_id);
				m_game.DrawCard(player, cardIndex);
			},
			(int cardIndex1, int cardIndex2) => //OnExchangeCard
			{
				int player = FindPlayerIndex(entry.slot_id);
				m_game.ExchangeCard(player, cardIndex1, cardIndex2);
			});
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

			PlayerPrefs.SetString("clientName", clientName);
			PlayerPrefs.Save();
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
			entries_read = 1;

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
			ReadForEntries();
		}
	}

	public void OnLeaveRoom(uint _connectionId)
	{
		Debug.Log("OnLeaveRoom: " + connectionId + " " + _connectionId);
		if (connectionId == _connectionId)
		{
			joinedRoomId = Biribit.Client.UnassignedId;
			joinedRoomSlot = Biribit.Client.UnassignedId;
			m_game = null;
		}
	}
}
