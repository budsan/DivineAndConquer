using System;
using System.Collections.Generic;

namespace Divine
{
	public class Card
	{
		public enum Type
		{
			None,
			FirstOration,
			SecondOration,
			ThirdOration,
			Curse,
			Radar,
			AntiRadar,
			Shuffle,
			RevealCard,
			Bounce
		}

		public const int NoCardIndex = -1;

		public Type type;
		public int belongsTo;
		public int bounceIndex;

		public Card(Type _type)
		{
			type = _type;
			belongsTo = Player.NoPlayerIndex;
			bounceIndex = NoCardIndex;
		}

		public void SwapTypes(Card other)
		{
			Type foo = other.type;
			other.type = type;
			type = foo;
		}

		public int Bounce()
		{
			int result = bounceIndex;
			bounceIndex = NoCardIndex;
			return result;
		}
	}

	public class Player
	{
		public const int NoPlayerIndex = -1;
		public const int handSize = 3;

		public int[] hand;
		public bool[] orations;
		public bool winner;
		public bool needToDraw;

		public Player()
		{
			hand = new int[handSize];
			for (int i = 0; i < handSize; i++)
				hand[i] = Card.NoCardIndex;

			orations = new bool[3];
			for (int i = 0; i < 3; i++)
				orations[i] = false;

			winner = false;
			needToDraw = true;
		}

		public void DrawCard(int playerIndex, int cardIndex, Card card)
		{
			if (card.belongsTo != NoPlayerIndex)
				return;

			for (int i = 0; i < handSize; i++)
				if (hand[i] == Card.NoCardIndex)
				{
					card.belongsTo = playerIndex;
					hand[i] = cardIndex;
					break;
				}

			needToDraw = false;
			for (int i = 0; i < handSize; i++)
				if (hand[i] == Card.NoCardIndex)
				{
					needToDraw = true;
					break;
				}
		}

		public void ThrowCard(int cardIndex, Card card)
		{
			for (int i = 0; i < handSize; i++)
				if (hand[i] == cardIndex)
				{
					hand[i] = Card.NoCardIndex;
					card.belongsTo = NoPlayerIndex;
					needToDraw = true;
				}
		}

		public bool FirstOration()
		{
			if (!orations[0])
			{
				orations[0] = true;
				return true;
			}

			return false;
		}

		public bool SecondOration()
		{
			if (orations[0] && !orations[1])
			{
				orations[1] = true;
				return true;
			}

			return false;
		}

		public bool ThirdOration()
		{
			if (orations[0] && orations[1] && !orations[2])
			{
				orations[2] = true;
				winner = true;
				return true;
			}

			return false;
		}

		public Card.Type GetRadarCardType()
		{
			if (!orations[0])
				return Card.Type.FirstOration;
			else if (orations[0] && !orations[1])
				return Card.Type.SecondOration;
			else if (orations[0] && orations[1] && !orations[2])
				return Card.Type.ThirdOration;
			else
				return Card.Type.None;
		}
	}

	public class State
	{
		private Random m_random = null;
		private List<int> m_randomValues = new List<int>();

		private const int deckSize = 20;
		private Card[] m_deck;
		private Player[] m_players;
		public bool[] m_needToDraw;

		public delegate void RadarDelegate(int playerIndex);
		public event RadarDelegate OnRadar;

		public delegate void RevealCardDelegate(int playerIndex, int cardIndex, Card.Type card);
		public event RevealCardDelegate OnRevealCard;

		public void StartGame(int seed, int playerCount = 4)
		{
			m_random = new Random(seed);
			m_randomValues.Clear();

			m_deck = new Card[deckSize];
			m_deck[0] = new Card(Card.Type.FirstOration);
			m_deck[1] = new Card(Card.Type.SecondOration);
			m_deck[2] = new Card(Card.Type.ThirdOration);
			m_deck[3] = new Card(Card.Type.Curse);
			m_deck[4] = new Card(Card.Type.Radar);
			m_deck[5] = new Card(Card.Type.Radar);
			m_deck[6] = new Card(Card.Type.Radar);
			m_deck[7] = new Card(Card.Type.AntiRadar);
			m_deck[8] = new Card(Card.Type.AntiRadar);
			m_deck[9] = new Card(Card.Type.AntiRadar);
			m_deck[10] = new Card(Card.Type.Shuffle);
			m_deck[11] = new Card(Card.Type.Shuffle);
			m_deck[12] = new Card(Card.Type.Shuffle);
			m_deck[13] = new Card(Card.Type.RevealCard);
			m_deck[14] = new Card(Card.Type.RevealCard);
			m_deck[15] = new Card(Card.Type.RevealCard);
			m_deck[16] = new Card(Card.Type.RevealCard);
			m_deck[17] = new Card(Card.Type.RevealCard);
			m_deck[18] = new Card(Card.Type.RevealCard);
			m_deck[19] = new Card(Card.Type.RevealCard);

			Shuffle(WholeDeck());

			m_players = new Player[playerCount];
			m_needToDraw = new bool[playerCount];
			for (int i = 0; i < m_players.Length; i++)
				m_players[i] = new Player();
		}


		public void DrawCard(int playerIndex, int cardIndex)
		{
			if (!ValidPlayerIndex(playerIndex) ||
				!ValidCardIndex(cardIndex))
				return;

			m_players[playerIndex].DrawCard(playerIndex, cardIndex, m_deck[cardIndex]);
		}

		public void UseCard(int playerIndex, int cardIndex, params int[] extra)
		{
			if (!ValidPlayerIndex(playerIndex) ||
				!ValidCardIndex(cardIndex))
				return;

			Card card = m_deck[cardIndex];
			Player player = m_players[playerIndex];

			if (card.belongsTo != playerIndex)
				return;

			switch (card.type)
			{
				case Card.Type.FirstOration:
					if (player.FirstOration())
					{
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case Card.Type.SecondOration:
					if (player.SecondOration())
					{
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case Card.Type.ThirdOration:
					if (player.ThirdOration())
					{
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case Card.Type.Curse:
					//Do nothing, you cannot use Curse.
					break;
				case Card.Type.Radar:
					Radar(player);
					player.ThrowCard(cardIndex, card);
					NextTurn();
					break;
				case Card.Type.AntiRadar:
					AntiRadar(playerIndex, player);
					player.ThrowCard(cardIndex, card);
					NextTurn();
					break;
				case Card.Type.Shuffle:
					if (extra == null || extra.Length != 2)
						return;

					int cardTargetIndex1 = extra[0];
					int cardTargetIndex2 = extra[1];
					if (!ValidCardIndex(cardTargetIndex1) ||
						!ValidCardIndex(cardTargetIndex2) ||
						m_deck[cardTargetIndex1].belongsTo == Player.NoPlayerIndex ||
						m_deck[cardTargetIndex2].belongsTo == Player.NoPlayerIndex)
						return;

					m_deck[cardTargetIndex1].SwapTypes(m_deck[cardTargetIndex2]);
					player.ThrowCard(cardIndex, card);
					NextTurn();
					break;
				case Card.Type.RevealCard:
					if (extra == null || extra.Length != 1)
						return;

					int cardTargetIndex = extra[0];
					if (!ValidCardIndex(cardTargetIndex) ||
						m_deck[cardTargetIndex].belongsTo == Player.NoPlayerIndex)
						return;

					EmitRevealCard(playerIndex, cardTargetIndex, m_deck[cardTargetIndex].type);
					player.ThrowCard(cardIndex, card);
					NextTurn();
					break;
				case Card.Type.Bounce:
					//TODO(Bud): Bounce
					break;
			}
		}

		public void ExchangeCard(int playerIndex, int playerCardIndex, int targetCardIndex)
		{
			if (!ValidPlayerIndex(playerIndex) ||
				!ValidCardIndex(playerCardIndex) ||
				!ValidCardIndex(targetCardIndex))
				return;

			Card playerCard = m_deck[playerCardIndex];
			Card targetCard = m_deck[targetCardIndex];
			if (playerCard.belongsTo != playerIndex ||
				targetCard.belongsTo == Player.NoPlayerIndex)
				return;

			Player playerOrigin = m_players[playerIndex];
			Player playerTarget = m_players[targetCard.belongsTo];

			playerOrigin.ThrowCard(playerCardIndex, playerCard);
			playerTarget.ThrowCard(targetCardIndex, targetCard);

			playerOrigin.DrawCard(playerIndex, targetCardIndex, targetCard);
			playerTarget.DrawCard(targetCard.belongsTo, playerCardIndex, playerCard);

			NextTurn();
		}

		public bool[] GetPlayersNeedToDraw()
		{
			for (int i = 0; i < m_players.Length; i++)
				m_needToDraw[i] = m_players[i].needToDraw;

			return m_needToDraw;
		}

		public int GetWinnerPlayerIndex()
		{
			for (int i = 0; i < m_players.Length; i++)
				if (m_players[i].winner)
					return i;

			return Player.NoPlayerIndex;
		}

		//-UTILITY---------------------------------------------------------------//

		private bool ValidPlayerIndex(int playerIndex)
		{
			return playerIndex >= 0 && playerIndex < m_players.Length;
		}

		private bool ValidCardIndex(int cardIndex)
		{
			return cardIndex >= 0 || cardIndex < m_deck.Length;
		}

		private void NextTurn()
		{
			// TODO(Bud): This function
		}

		private int NextRandom()
		{
			int next = m_random.Next();
			m_randomValues.Add(next);
			return next;
		}

		private int[] WholeDeck()
		{
			int[] set = new int[deckSize];
			for (int i = 0; i < deckSize; i++)
				set[i] = i;

			return set;
		}

		private void Shuffle(int[] set)
		{
			for (int i = 0; i < (set.Length - 1); i++)
			{
				int swapWith = i + (NextRandom() % (set.Length - i));
				m_deck[set[i]].SwapTypes(m_deck[set[swapWith]]);
			}
		}

		private void Radar(Player player)
		{
			Card.Type type = player.GetRadarCardType();
			for (int i = 0; i < deckSize; i++)
			{
				Card look = m_deck[i];
				if (look.type == type && look.belongsTo != Player.NoPlayerIndex)
					EmitRadarBeep(look.belongsTo);
			}
		}

		private void AntiRadar(int playerIndex, Player player)
		{
			Card.Type type = player.GetRadarCardType();
			HashSet<int> targets = new HashSet<int>();
			for (int i = 0; i < m_players.Length; i++)
				targets.Add(i);

			targets.Remove(playerIndex);
			for (int i = 0; i < deckSize; i++)
			{
				Card look = m_deck[i];
				if (look.type == type && look.belongsTo != Player.NoPlayerIndex)
					targets.Remove(look.belongsTo);
			}

			int[] finalTargets = new int[targets.Count];
			targets.CopyTo(finalTargets);
			int targetIndex = NextRandom() % finalTargets.Length;
			EmitRadarBeep(finalTargets[targetIndex]);
		}

		private void EmitRadarBeep(int playerIndex)
		{
			if (OnRadar != null)
				OnRadar(playerIndex);
		}

		private void EmitRevealCard(int playerIndex, int cardIndex, Card.Type type)
		{
			if (OnRevealCard != null)
				OnRevealCard(playerIndex, cardIndex, type);
		}
	}
}
