using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public interface ARLogicView
{
	void ARCardTrackedLost();
	void NewARCardTracked(int cardIndex, GameObject parent);
}

public class MenuLogic : Silver.UI.TabImmediate, BiribitListener, Divine.State.Listener, ARLogicView
{
	[SerializeField] private CardContentManager contentManager = null;
	[SerializeField] private string address = "thatguystudio.com";
	[SerializeField] private string clientName = "David";
	[SerializeField] private string appId = "divine-and-conquer-0";
	[SerializeField] private bool debug = false;

	public override string TabName()
	{
		return "Divine and Conquer";
	}

	private string password = "";
	private uint connectionId = Biribit.Client.UnassignedId;
	private uint joinedRoomId = Biribit.Client.UnassignedId;
	private int joinedRoomIndex = 0;
	private uint joinedRoomSlot = Biribit.Client.UnassignedId;

	private int m_turnAction = -1;
	private List<int> m_turnExtraParams = new List<int>();

	private class Announcement
	{
		public string message;
		public float timeLeft;

		public Announcement(string _message, float _timeLeft)
		{
			message = _message;
			timeLeft = _timeLeft;
		}
	}

	private const string NoAnnouncement = "No announcement";
	private Queue<Announcement> m_announcement = new Queue<Announcement>();
	private int m_announcementCount = 0;

	private void AddAnnouncement(string _message, float _timeLeft = 15.0f)
	{
		m_announcementCount++;
		string show = "[" + (m_announcementCount) + "] " + _message;
		m_announcement.Enqueue(new Announcement(show, _timeLeft));
	}

	private void UpdateAnnouncement(float deltaTime)
	{
		foreach(Announcement ann in m_announcement)
			ann.timeLeft -= deltaTime;

		while (m_announcement.Count > 0 && m_announcement.Peek().timeLeft <= 0)
			m_announcement.Dequeue();
	}

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

	private int entries_read = 0;

	private List<string> serverInfoStrings = new List<string>();
	private int serverInfoSelected = 0;
	private int slots = 4;
	private List<string> roomStrings = new List<string>();
	private int roomSelected = 0;

	private BiribitManager manager;

	private int m_ARCardIndex = Divine.Card.NoCardIndex;
	private Divine.CardType m_ARCardType = Divine.CardType.None;
	private GameObject m_ARCardParent = null;

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

	Silver.UI.Immediate.LayoutElementDescription buttonLayout = new Silver.UI.Immediate.LayoutElementDescription();

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

		buttonLayout.flexibleWidth = 1;
		buttonLayout.minHeight = 40;

		if (contentManager == null)
			throw new Exception("No contentManager assigned in inspector");
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
			slots = ui.IntField("Slots", slots);
			if (slots < 2) slots = 2;
			if (slots > 4) slots = 4;

			if (ui.Button("Create"))
				manager.CreateRoom(connectionId, (byte)slots);
		});

		if (ui.Button("Join random or create"))
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

		Biribit.Room[] roomArray = manager.Rooms(connectionId);
		Biribit.Room rm = roomArray[joinedRoomIndex];

		if (m_game == null || m_game.GetWinnerPlayerIndex() != Divine.Player.NoPlayerIndex)
		{
			if (ui.Button("Leave"))
				manager.JoinRoom(connectionId, 0);

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
						(int)(DateTime.Now.Ticks & 0xFFFFFFFF),
						playerSet.ToArray()
					)
				);

				m_turnAction = -1;
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
			if (m_ARCardIndex == Divine.Card.NoCardIndex)
			{
				if (ui.Button("Leave"))
					manager.JoinRoom(connectionId, 0);

				DrawAnnouncement();
				DrawPlayer(player);
			}

			bool[] draw = m_game.GetPlayersNeedToDraw();
			if (draw[player])
			{
				Divine.PlayerView playerView = m_game.GetPlayer(player);
				if (m_ARCardIndex == Divine.Card.NoCardIndex)
				{
					ui.Separator(1);
					ui.LabelField("You need to draw cards");
					ui.LineSeparator();
					int drawedCardIndex = 0;
					if (DrawNumeric(1, m_game.DeckSize, ref drawedCardIndex, (int index) => {
						return m_game.GetCard(index - 1).BelongsTo == Divine.Player.NoPlayerIndex &&
							playerView.LastCardThrown != (index - 1);
					}))
						manager.SendEntry(connectionId, DivineSerializator.DrawCard(drawedCardIndex - 1));
				}
				else
				{
					Silver.UI.Immediate.FlagMask mask = Silver.UI.Immediate.FlagMask.None;
					bool grabbable = m_game.GetCard(m_ARCardIndex).BelongsTo == Divine.Player.NoPlayerIndex
						&& playerView.LastCardThrown != m_ARCardIndex;

					mask = grabbable ? mask : mask | Silver.UI.Immediate.FlagMask.NoInteractable;

					if (ui.Button("Grab this card", mask))
						manager.SendEntry(connectionId, DivineSerializator.DrawCard(m_ARCardIndex));
				}
				
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
					int playerTurn = m_game.GetWhoseTurnIsIt();
					if (player != playerTurn)
					{
						int turnSlot = m_playerSet[playerTurn];
						int turnClientIndex = manager.RemoteClients(connectionId, joinedRoom.slots[turnSlot]);
						string drawName = (turnClientIndex < 0) ?
							joinedRoom.slots[turnSlot].ToString() :
							manager.RemoteClients(connectionId)[turnClientIndex].name;

						ui.Separator(1);
						ui.LabelField("It's player " + (playerTurn + 1).ToString() + "(" + drawName + ") turn!", 14);
					}
					else
					{
						if (m_ARCardIndex == Divine.Card.NoCardIndex)
						{
							ui.Separator(1);
							ui.LabelField("Turn action");
							ui.LineSeparator();
						}

						DrawTurn(joinedRoom, player);
					}
				}
			}
		}
		else
		{
			if (ui.Button("Leave"))
				manager.JoinRoom(connectionId, 0);

			ui.LabelField("Wait players to finish...");
		}
	}

	public void DrawAnnouncement()
	{
		ui.Separator(1);
		ui.LabelField("Announcements: " + m_announcement.Count, 14);
		ui.LineSeparator();

		if (m_announcement.Count == 0)
			ui.LabelField(NoAnnouncement);
		else
			foreach(Announcement ann in m_announcement)
				ui.LabelField(ann.message, 12);

		UpdateAnnouncement(Time.deltaTime);
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
				ui.LabelField(Enum.GetName(typeof(Divine.CardType), card.Type) +
					" (" + (player.Hand[i] + 1).ToString() + ")", 14);
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

	public void DrawTurnButtonAction(Divine.PlayerView player, int cardIndex, int handIndex)
	{
		Divine.CardView card = m_game.GetCard(cardIndex);
		string buttonName = Enum.GetName(typeof(Divine.CardType), card.Type) +
		" (" + (cardIndex + 1).ToString() + ")";

		Silver.UI.Immediate.FlagMask mask = Silver.UI.Immediate.FlagMask.None;
		if (m_turnAction == handIndex
			|| card.Type == Divine.CardType.Curse
			|| (card.Type == Divine.CardType.FirstOration
			 && player.Orations[0])
			|| (card.Type == Divine.CardType.SecondOration
			 && (!player.Orations[0] || player.Orations[1]))
			|| (card.Type == Divine.CardType.ThirdOration
			&& (!player.Orations[0] || !player.Orations[1] || player.Orations[2])))
			mask |= Silver.UI.Immediate.FlagMask.NoInteractable;

		ui.NextLayoutElement = buttonLayout;
		if (ui.Button(buttonName, mask))
		{
			m_turnAction = handIndex;
			m_turnExtraParams.Clear();
		}
	}

	public void DrawTurn(Biribit.Room joinedRoom, int playerIndex)
	{
		Divine.PlayerView player = m_game.GetPlayer(playerIndex);

		if (m_ARCardIndex == Divine.Card.NoCardIndex)
		{
			ui.BeginHorizontalLayout();

			for (int i = 0; i < player.Hand.Length; i++)
			{
				if (i == 2)
				{
					ui.EndHorizontalLayout();
					ui.BeginHorizontalLayout();
				}

				DrawTurnButtonAction(player, player.Hand[i], i);
			}

			Silver.UI.Immediate.FlagMask mask = Silver.UI.Immediate.FlagMask.None;
			if (m_turnAction == 3)
				mask |= Silver.UI.Immediate.FlagMask.NoInteractable;

			ui.NextLayoutElement = buttonLayout;
			if (ui.Button("Exchange", mask))
			{
				m_turnAction = 3;
				m_turnExtraParams.Clear();
			}

			ui.EndHorizontalLayout();
		}
		else
		{
			if (m_turnAction == -1)
				for (int i = 0; i < player.Hand.Length; i++)
					if(m_ARCardIndex == player.Hand[i])
						DrawTurnButtonAction(player, player.Hand[i], i);
		}

		if (m_turnAction >= 0 && m_turnAction < 3)
		{
			Divine.CardView card = m_game.GetCard(player.Hand[m_turnAction]);
			switch(card.Type)
			{
				case Divine.CardType.FirstOration:
					goto case Divine.CardType.AntiRadar;
				case Divine.CardType.SecondOration:
					goto case Divine.CardType.AntiRadar;
				case Divine.CardType.ThirdOration:
					goto case Divine.CardType.AntiRadar;
				case Divine.CardType.Curse:
					break;
				case Divine.CardType.Radar:
					goto case Divine.CardType.AntiRadar;
				case Divine.CardType.AntiRadar:
					manager.SendEntry(connectionId, 
						DivineSerializator.UseCard(player.Hand[m_turnAction]));
					m_turnAction = -1;
					m_turnExtraParams.Clear();
					break;
				case Divine.CardType.Shuffle:
					goto case Divine.CardType.Bounce;
				case Divine.CardType.RevealCard:
				{
					ui.Separator(1);
					ui.LabelField("Choose card to reveal");
					ui.LineSeparator();

					int choosenCardIndex = 0;
					if (DrawChooseFromAnyPlayer(joinedRoom, ref choosenCardIndex, (int currentPlayer, out bool showTypes) =>
					{
						showTypes = (currentPlayer == playerIndex);
						return showTypes;
					}))
					{
						m_turnExtraParams.Add(choosenCardIndex);
					}

					if (m_turnExtraParams.Count >= 1)
					{
						ui.Separator(1);
						manager.SendEntry(connectionId,
							DivineSerializator.UseCard(player.Hand[m_turnAction], m_turnExtraParams.ToArray()));
						m_turnAction = -1;
						m_turnExtraParams.Clear();
					}
				}
				break;
				case Divine.CardType.Bounce:
				{
					ui.Separator(1);
					if (m_turnExtraParams.Count < 1)
						ui.LabelField("Choose first card");
					else if (m_turnExtraParams.Count < 2)
						ui.LabelField("Choose second card");
					else
					{
						ui.Separator(1);
						manager.SendEntry(connectionId,
							DivineSerializator.UseCard(player.Hand[m_turnAction], m_turnExtraParams.ToArray()));
						m_turnAction = -1;
						m_turnExtraParams.Clear();
					}

					ui.LineSeparator();

					int choosenCardIndex = 0;
					if (DrawChooseFromAnyPlayer(joinedRoom, ref choosenCardIndex,
						(int currentPlayer, out bool showTypes) =>
						{
							showTypes = (currentPlayer == playerIndex);
							return false;
						}))
					{
						m_turnExtraParams.Add(choosenCardIndex);
					}
				}
				break;
			}
		}
		else if (m_turnAction == 3)
		{
			//Exchange
			if (m_turnExtraParams.Count < 1)
			{
				ui.Separator(1);
				ui.LabelField("Choose one card of yours");
				ui.LineSeparator();

				int choosenCardIndex = 0;
				if (DrawChooseCardFromPlayer(player, true, ref choosenCardIndex))
					m_turnExtraParams.Add(choosenCardIndex);
			}
			else if (m_turnExtraParams.Count < 2)
			{
				ui.Separator(1);
				ui.LabelField("Choose one card of your opponents");
				ui.LineSeparator();

				int choosenCardIndex = 0;
				if (DrawChooseFromAnyPlayer(joinedRoom, ref choosenCardIndex,
					(int index, out bool showTypes) => {
						return (showTypes = (index == playerIndex));
					}))
				{
					m_turnExtraParams.Add(choosenCardIndex);
				}
			}
			else
			{
				manager.SendEntry(connectionId, DivineSerializator.ExchangeCard(m_turnExtraParams[0], m_turnExtraParams[1]));
				m_turnAction = -1;
				m_turnExtraParams.Clear();
			}
		}
	}

	public delegate bool DrawChooseFromAnyPlayerDelegate(int playerIndex, out bool showTypes);
	public bool DrawChooseFromAnyPlayer(Biribit.Room joinedRoom, ref int cardIndex, DrawChooseFromAnyPlayerDelegate call)
	{
		bool clicked = false;
		for (int i = 0; i < m_playerSet.Length; i++)
		{
			bool showTypes = false;
			if (call != null)
				if (call(i, out showTypes))
					continue;

			int opponentSlot = m_playerSet[i];
			int opponentClientIndex = manager.RemoteClients(connectionId, joinedRoom.slots[opponentSlot]);
			string drawName = (opponentClientIndex < 0) ?
				joinedRoom.slots[opponentSlot].ToString() :
				manager.RemoteClients(connectionId)[opponentClientIndex].name;

			ui.Separator(1);
			ui.LabelField("Player " + (i + 1).ToString() + "(" + drawName + ") card:", 14);
			ui.LineSeparator();

			Divine.PlayerView opponent = m_game.GetPlayer(i);

			int choosenCardIndex = 0;
			if (DrawChooseCardFromPlayer(opponent, showTypes, ref choosenCardIndex))
			{
				clicked = true;
				cardIndex = choosenCardIndex;
			}
		}

		return clicked;
	}

	public bool DrawChooseCardFromPlayer(Divine.PlayerView player, bool showTypes, ref int cardIndex)
	{
		bool clicked = false;
		ui.BeginHorizontalLayout();
		for (int i = 0; i < player.Hand.Length; i++)
		{
			Divine.CardView card = m_game.GetCard(player.Hand[i]);
			string buttonName = (player.Hand[i] + 1).ToString();
			if (showTypes)
				buttonName = Enum.GetName(typeof(Divine.CardType), card.Type) + " (" + buttonName + ")";

			ui.NextLayoutElement = buttonLayout;
			if (ui.Button(buttonName))
			{
				clicked = true;
				cardIndex = player.Hand[i];
			}
				
		}
		ui.EndHorizontalLayout();

		return clicked;
	}

	public delegate bool DrawNumericEnableDelegate(int value);
	public bool DrawNumeric(int rangeBegin, int rangeEnd, ref int result, DrawNumericEnableDelegate check = null)
	{
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

			ui.NextLayoutElement = buttonLayout;
			if (ui.Button((i + rangeBegin).ToString(), mask))
			{
				result = i + rangeBegin;
				clicked = true;
			}
		}

		ui.EndHorizontalLayout();
		return clicked;
	}

	public void OnRadar(int playerIndex)
	{
		
		Biribit.Room joinedRoom = manager.Rooms(connectionId)[joinedRoomIndex];

		int playerSlot = m_playerSet[playerIndex];
		int clientIndex = manager.RemoteClients(connectionId, joinedRoom.slots[playerSlot]);
		string drawName = (clientIndex < 0) ?
			joinedRoom.slots[playerSlot].ToString() :
			manager.RemoteClients(connectionId)[clientIndex].name;

		AddAnnouncement("Player " + drawName + " emitted radar sound");
	}

	public void OnRevealCard(int playerIndex, int cardIndex, Divine.CardType card)
	{
		int player = FindPlayerIndex((byte)manager.JoinedRoomSlot(connectionId));
		if (player == playerIndex)
		{
			AddAnnouncement("The card " + (cardIndex + 1).ToString() +
				" is " + Enum.GetName(typeof(Divine.CardType), card));
		}
	}

	public void OnEndOfRound(Divine.State.EndOfRoundType type)
	{
		AddAnnouncement("End of round " + m_game.RoundNumber + ".");

		if ((type & Divine.State.EndOfRoundType.UsedFirstOration) != 0)
			AddAnnouncement("First part of the oration sounds.");

		if ((type & Divine.State.EndOfRoundType.UsedSecondOration) != 0)
			AddAnnouncement("Second part of the oration sounds.");

		if ((type & Divine.State.EndOfRoundType.UsedThirdOration) != 0)
			AddAnnouncement("Third part of the oration sounds.");
	}

	private void DebugMessage(string message)
	{
		if(debug)
			AddAnnouncement("[DEBUG]" + message);
	}

	public void NewIncomingEntry(Biribit.Entry entry)
	{
		DivineSerializator.Deserialize(entry.data,
			(int seed, int[] playerSet) => //OnStart
			{
				m_game = new Divine.State();

				m_game.AddListener(this);
				m_game.StartGame(seed, playerSet.Length);
				m_playerSet = playerSet;
				m_announcement.Clear();
				m_announcementCount = 0;

				UpdateARCardTracked();
			},
			(int cardIndex, int[] extraParams) => //OnUseCard
			{
				int player = FindPlayerIndex(entry.slot_id);
				try { m_game.UseCard(player, cardIndex, extraParams); }
				catch (Exception ex) { DebugMessage(ex.Message); }

				UpdateARCardTracked();
			},
			(int cardIndex) => //OnDrawCard
			{
				int player = FindPlayerIndex(entry.slot_id);
				try { m_game.DrawCard(player, cardIndex); }
				catch (Exception ex) { DebugMessage(ex.Message); }

				UpdateARCardTracked();
			},
			(int cardIndex1, int cardIndex2) => //OnExchangeCard
			{
				int player = FindPlayerIndex(entry.slot_id);
				try { m_game.ExchangeCard(player, cardIndex1, cardIndex2); }
				catch (Exception ex) { DebugMessage(ex.Message); }

				UpdateARCardTracked();
			});
	}

	public void ReadForEntries()
	{
		List<Biribit.Entry> entries = manager.JoinedRoomEntries(connectionId);
		for (; entries_read < entries.Count; entries_read++)
			NewIncomingEntry(entries[entries_read]);
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
			contentManager.RestoreAll();
		}
	}

	public void ARCardTrackedLost()
	{
		if (m_ARCardIndex != Divine.Card.NoCardIndex)
		{
			m_ARCardIndex = Divine.Card.NoCardIndex;
			m_ARCardParent = null;
			UpdateARCardTracked();
		}
	}

	public void NewARCardTracked(int cardIndex, GameObject parent)
	{
		Debug.Log("NewARCardTracked " + cardIndex + " " + parent.name);

		if (m_ARCardIndex != Divine.Card.NoCardIndex)
			ARCardTrackedLost();

		m_ARCardIndex = cardIndex;
		m_ARCardParent = parent;

		UpdateARCardTracked();
	}

	public void UpdateARCardTracked()
	{
		if (m_ARCardIndex == Divine.Card.NoCardIndex)
		{
			if (m_ARCardType != Divine.CardType.None)
				contentManager.RestoreCardFromType(m_ARCardType);

			m_ARCardType = Divine.CardType.None;
		}
		else
		{
			Divine.CardType type = Divine.CardType.Unknown;

			if (m_game != null)
			{
				int player = FindPlayerIndex((byte)manager.JoinedRoomSlot(connectionId));
				Divine.CardView card = m_game.GetCard(m_ARCardIndex);
				if (card.BelongsTo == player)
					type = card.Type;
				else
					type = Divine.CardType.Unknown;
			}
				

			if (m_ARCardType != type)
			{
				if (m_ARCardType != Divine.CardType.None)
					contentManager.RestoreCardFromType(m_ARCardType);

				if (type != Divine.CardType.None) {
					GameObject cardObject = contentManager.GetCardFromType(type);
					if (m_ARCardParent != null)
						cardObject.transform.SetParent(m_ARCardParent.transform, false);
				}
				
				m_ARCardType = type;
			}
		}
	}
}
