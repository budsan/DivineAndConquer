﻿using System;
using System.Collections.Generic;

namespace Divine
{
	public enum CardType : int
	{
		None = -1,
		Unknown = 0,
		FirstOration,
		SecondOration,
		ThirdOration,
		Curse,
		Radar,
		AntiRadar,
		Shuffle,
		RevealCard,
		Bounce,
		Count
	}

	public interface CardView
	{
		CardType Type { get; }
		int BelongsTo { get; }
		int BounceTo { get; }
	}

	internal class Card : CardView
	{
		public const int NoCardIndex = -1;

		internal CardType type;
		internal int belongsTo;
		internal int bounceTo;

		public CardType Type { get { return type; } }
		public int BelongsTo { get { return belongsTo; } }
		public int BounceTo { get { return bounceTo; } }

		internal Card(CardType _type)
		{
			type = _type;
			belongsTo = Player.NoPlayerIndex;
			bounceTo = NoCardIndex;
		}

		internal void SwapTypes(Card other)
		{
			CardType foo = other.type;
			other.type = type;
			type = foo;
		}

		internal int Bounce()
		{
			int result = bounceTo;
			bounceTo = NoCardIndex;
			return result;
		}
	}

	public interface PlayerView
	{
		int[] Hand { get; }
		bool[] Orations { get; }
		int LastCardThrown { get; }
		bool Winner { get; }
		bool NeedToDraw { get; }
	}

	internal class Player : PlayerView
	{
		public const int NoPlayerIndex = -1;
		public const int handSize = 3;

		internal int[] hand;
		internal bool[] orations;
		internal int lastCardThrown;
		internal bool winner;
		internal bool needToDraw;

		public int[] Hand { get { return hand; } }
		public bool[] Orations { get { return orations; } }
		public int LastCardThrown { get { return lastCardThrown; } }
		public bool Winner { get { return winner; } }
		public bool NeedToDraw { get { return needToDraw; } }

		internal Player()
		{
			hand = new int[handSize];
			for (int i = 0; i < handSize; i++)
				hand[i] = Card.NoCardIndex;

			orations = new bool[3];
			for (int i = 0; i < 3; i++)
				orations[i] = false;

			winner = false;
			needToDraw = true;
			lastCardThrown = Card.NoCardIndex;
		}

		internal void DrawCard(int playerIndex, int cardIndex, Card card)
		{
			AssertCardBelongsToPlayer(card, NoPlayerIndex);

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

		internal void ThrowCard(int cardIndex, Card card)
		{
			for (int i = 0; i < handSize; i++)
				if (hand[i] == cardIndex)
				{
					hand[i] = Card.NoCardIndex;
					card.belongsTo = NoPlayerIndex;
					card.bounceTo = Card.NoCardIndex;
					lastCardThrown = cardIndex;
					needToDraw = true;
				}
		}

		private void AssertCardBelongsToPlayer(Card card, int playerIndex)
		{
			if (card.belongsTo != playerIndex)
				throw new Exception("Card doesn't belong to player(" + playerIndex + ")");
		}

		internal bool FirstOration()
		{
			if (!orations[0])
			{
				orations[0] = true;
				return true;
			}

			return false;
		}

		internal bool SecondOration()
		{
			if (orations[0] && !orations[1])
			{
				orations[1] = true;
				return true;
			}

			return false;
		}

		internal bool ThirdOration()
		{
			if (orations[0] && orations[1] && !orations[2])
			{
				orations[2] = true;
				winner = true;
				return true;
			}

			return false;
		}

		internal CardType GetRadarCardType()
		{
			if (!orations[0])
				return CardType.FirstOration;
			else if (orations[0] && !orations[1])
				return CardType.SecondOration;
			else if (orations[0] && orations[1] && !orations[2])
				return CardType.ThirdOration;
			else
				return CardType.Unknown;
		}
	}

	public class State
	{
		[Flags]
		public enum EndOfRoundType
		{
			None = 0,
			UsedFirstOration = 1 << 0,
			UsedSecondOration = 1 << 1,
			UsedThirdOration = 1 << 2
		}

		public interface Listener
		{
			void OnRadar(int playerIndex);
			void OnRevealCard(int playerIndex, int cardIndex, CardType card);
			void OnEndOfRound(EndOfRoundType type);
		}

		private HashSet<Listener> m_listeners = new HashSet<Listener>();
		public void AddListener(Listener listener) { m_listeners.Add(listener); }
		public void DelListener(Listener listener) { m_listeners.Remove(listener); }

		private Random m_random = new Random();
		private List<int> m_randomValues = new List<int>();

		private const int deckSize = 20;
		public int DeckSize { get { return deckSize; } }

		private Card[] m_deck = new Card[0];
		private Player[] m_players = new Player[0];

		private int[] m_playersTurn = new int[0];
		private int m_playerTurnIndex = 0;
		private int m_roundNumber = 0;
		public int RoundNumber { get { return m_roundNumber; } }

		private bool[] m_needToDraw = new bool[0];
		private EndOfRoundType m_endOfRoundType = EndOfRoundType.None;

		public CardView GetCard(int cardIndex) { return m_deck[cardIndex]; }
		public PlayerView GetPlayer(int playerIndex) { return m_players[playerIndex]; }

		public void StartGame(int seed, int playerCount = 4)
		{
			m_random = new Random(seed);
			m_randomValues.Clear();

			m_deck = new Card[deckSize];
			m_deck[0] = new Card(CardType.FirstOration);
			m_deck[1] = new Card(CardType.SecondOration);
			m_deck[2] = new Card(CardType.ThirdOration);
			m_deck[3] = new Card(CardType.Curse);
			m_deck[4] = new Card(CardType.Radar);
			m_deck[5] = new Card(CardType.Radar);
			m_deck[6] = new Card(CardType.Radar);
			m_deck[7] = new Card(CardType.AntiRadar);
			m_deck[8] = new Card(CardType.AntiRadar);
			m_deck[9] = new Card(CardType.AntiRadar);
			m_deck[10] = new Card(CardType.Shuffle);
			m_deck[11] = new Card(CardType.Shuffle);
			m_deck[12] = new Card(CardType.Shuffle);
			m_deck[13] = new Card(CardType.RevealCard);
			m_deck[14] = new Card(CardType.RevealCard);
			m_deck[15] = new Card(CardType.RevealCard);
			m_deck[16] = new Card(CardType.RevealCard);
			m_deck[17] = new Card(CardType.Bounce);
			m_deck[18] = new Card(CardType.Bounce);
			m_deck[19] = new Card(CardType.Bounce);

			Shuffle(WholeDeck());

			m_players = new Player[playerCount];
			m_needToDraw = new bool[playerCount];
			m_playersTurn = new int[playerCount];
			for (int i = 0; i < m_players.Length; i++)
			{
				m_players[i] = new Player();
				m_playersTurn[i] = i;
			}

			ShufflePlayers(m_playersTurn);
			m_playerTurnIndex = 0;
			m_endOfRoundType = EndOfRoundType.None;
			m_roundNumber = 0;
		}

		public void DrawCard(int playerIndex, int cardIndex)
		{
			AssertValidPlayerIndex(playerIndex);
			AssertValidCardIndex(cardIndex);

			Player player = m_players[playerIndex];
			if (player.lastCardThrown != cardIndex)
				player.DrawCard(playerIndex, cardIndex, m_deck[cardIndex]);
		}

		public void UseCard(int playerIndex, int cardIndex, params int[] extra)
		{
			AssertValidPlayerIndex(playerIndex);
			AssertValidCardIndex(cardIndex);
			AssertPlayerTurnUnequal(playerIndex);

			Card card = m_deck[cardIndex];
			Player player = m_players[playerIndex];

			AssertCardBelongsToPlayer(cardIndex, playerIndex);
			
			switch (card.type)
			{
				case CardType.FirstOration:
					if (player.FirstOration())
					{
						m_endOfRoundType |= EndOfRoundType.UsedFirstOration;
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case CardType.SecondOration:
					if (player.SecondOration())
					{
						m_endOfRoundType |= EndOfRoundType.UsedSecondOration;
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case CardType.ThirdOration:
					if (player.ThirdOration())
					{
						m_endOfRoundType |= EndOfRoundType.UsedThirdOration;
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case CardType.Curse:
					//Do nothing, you cannot use Curse.
					break;
				case CardType.Radar:
					Radar(player);
					player.ThrowCard(cardIndex, card);
					NextTurn();
					break;
				case CardType.AntiRadar:
					AntiRadar(playerIndex, player);
					player.ThrowCard(cardIndex, card);
					NextTurn();
					break;
				case CardType.Shuffle:
					{
						AssertExtraParamsDontMatch(extra, 2);

						int cardTargetIndex1 = extra[0];
						int cardTargetIndex2 = extra[1];

						AssertValidCardIndex(cardTargetIndex1);
						AssertValidCardIndex(cardTargetIndex2);
						AssertCardDoesntBelongToPlayer(cardTargetIndex1, Player.NoPlayerIndex);
						AssertCardDoesntBelongToPlayer(cardTargetIndex2, Player.NoPlayerIndex);

						cardTargetIndex1 = GetCardWithBounce(cardTargetIndex1);
						cardTargetIndex2 = GetCardWithBounce(cardTargetIndex2);

						m_deck[cardTargetIndex1].SwapTypes(m_deck[cardTargetIndex2]);
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case CardType.RevealCard:
					{
						AssertExtraParamsDontMatch(extra, 1);

						int cardTargetIndex = extra[0];
						AssertValidCardIndex(cardTargetIndex);
						AssertCardDoesntBelongToPlayer(cardTargetIndex, Player.NoPlayerIndex);

						cardTargetIndex = GetCardWithBounce(cardTargetIndex);

						EmitRevealCard(playerIndex, cardTargetIndex, m_deck[cardTargetIndex].type);
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
				case CardType.Bounce:
					{
						AssertExtraParamsDontMatch(extra, 2);

						int cardTargetIndex1 = extra[0];
						int cardTargetIndex2 = extra[1];

						AssertValidCardIndex(cardTargetIndex1);
						AssertValidCardIndex(cardTargetIndex2);
						AssertCardDoesntBelongToPlayer(cardTargetIndex1, Player.NoPlayerIndex);
						AssertCardDoesntBelongToPlayer(cardTargetIndex2, Player.NoPlayerIndex);

						cardTargetIndex1 = GetCardWithBounce(cardTargetIndex1);
						cardTargetIndex2 = GetCardWithBounce(cardTargetIndex2);

						m_deck[cardTargetIndex1].bounceTo = cardTargetIndex2;
						player.ThrowCard(cardIndex, card);
						NextTurn();
					}
					break;
			}
		}

		public void ExchangeCard(int playerIndex, int playerCardIndex, int targetCardIndex)
		{
			AssertValidPlayerIndex(playerIndex);
			AssertValidCardIndex(playerCardIndex);
			AssertValidCardIndex(targetCardIndex);
			AssertPlayerTurnUnequal(playerIndex);

			Card playerCard = m_deck[playerCardIndex];
			Card targetCard = m_deck[targetCardIndex];

			AssertCardBelongsToPlayer(playerCardIndex, playerIndex);
			AssertCardDoesntBelongToPlayer(targetCardIndex, Player.NoPlayerIndex);

			int otherPlayer = targetCard.belongsTo;
			Player playerOrigin = m_players[playerIndex];
			Player playerTarget = m_players[otherPlayer];
			
			playerOrigin.ThrowCard(playerCardIndex, playerCard);
			playerTarget.ThrowCard(targetCardIndex, targetCard);

			playerOrigin.DrawCard(playerIndex, targetCardIndex, targetCard);
			playerTarget.DrawCard(otherPlayer, playerCardIndex, playerCard);

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

		public int GetWhoseTurnIsIt()
		{
			return m_playersTurn[m_playerTurnIndex];
		}

		//-UTILITY---------------------------------------------------------------//

		private void AssertValidPlayerIndex(int playerIndex)
		{
			if (!ValidPlayerIndex(playerIndex))
				throw new Exception("InvalidPlayerIndex(" + playerIndex + ")");
		}

		private bool ValidPlayerIndex(int playerIndex)
		{
			return playerIndex >= 0 && playerIndex < m_players.Length;
		}

		private void AssertValidCardIndex(int cardIndex)
		{
			if (!ValidCardIndex(cardIndex))
				throw new Exception("InvalidCardIndex(" + cardIndex + ")");
		}

		private bool ValidCardIndex(int cardIndex)
		{
			return cardIndex >= 0 || cardIndex < m_deck.Length;
		}

		private void AssertPlayerTurnUnequal(int playerIndex)
		{
			if (playerIndex != m_playersTurn[m_playerTurnIndex])
				throw new Exception("PlayerTurnUnequal(" +
					playerIndex + " index != " + m_playersTurn[m_playerTurnIndex] + " turn)");
		}

		private void AssertCardDoesntBelongToPlayer(int cardIndex, int playerIndex)
		{
			if (m_deck[cardIndex].belongsTo == playerIndex)
				throw new Exception("Card(" + cardIndex + ") belongs to player(" + playerIndex + ")");
		}

		private void AssertCardBelongsToPlayer(int cardIndex, int playerIndex)
		{
			if (m_deck[cardIndex].belongsTo != playerIndex)
				throw new Exception("Card(" + cardIndex + ") Doesn't belong to player(" + playerIndex + ")");
		}

		private void AssertExtraParamsDontMatch(int[] extra, int Length)
		{
			if (extra == null)
				throw new Exception("ExtraParamsDontMatch(null)");

			if (extra.Length != Length)
				throw new Exception("ExtraParamsDontMatch(" + extra.Length + " != " + Length + ")");
		}

		private int GetCardWithBounce(int cardIndex)
		{
			AssertValidCardIndex(cardIndex);

			int currentCardIndex = cardIndex;
			while (true)
			{
				int next = m_deck[currentCardIndex].Bounce();
				if (next != Card.NoCardIndex && m_deck[next].belongsTo != Player.NoPlayerIndex)
					currentCardIndex = next;
				else
					break;
			}

			return currentCardIndex;
		}

		private void NextTurn()
		{
			m_playerTurnIndex++;

			if (m_playerTurnIndex >= m_playersTurn.Length)
			{
				var endOfTurnType = m_endOfRoundType;

				ShufflePlayers(m_playersTurn);
				m_playerTurnIndex = 0;
				m_roundNumber++;
				m_endOfRoundType = EndOfRoundType.None;

				EmitEndOfTurn(endOfTurnType);
			}
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

		private void ShufflePlayers(int[] playerTurns)
		{
			for (int i = 0; i < (playerTurns.Length - 1); i++)
			{
				int swapWith = i + (NextRandom() % (playerTurns.Length - i));
				int foo = playerTurns[i];
				playerTurns[i] = playerTurns[swapWith];
				playerTurns[swapWith] = foo;
			}
		}

		private void Radar(Player player)
		{
			CardType type = player.GetRadarCardType();
			for (int i = 0; i < deckSize; i++)
			{
				Card look = m_deck[i];
				if (look.type == type && look.belongsTo != Player.NoPlayerIndex)
					EmitRadarBeep(look.belongsTo);
			}
		}

		private void AntiRadar(int playerIndex, Player player)
		{
			CardType type = player.GetRadarCardType();
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

			if (targets.Count > 0)
			{
				int[] finalTargets = new int[targets.Count];
				targets.CopyTo(finalTargets);
			
				int targetIndex = NextRandom() % finalTargets.Length;
				EmitRadarBeep(finalTargets[targetIndex]);
			}
			
		}

		private void EmitRadarBeep(int playerIndex)
		{
			foreach (Listener listener in m_listeners)
				listener.OnRadar(playerIndex);
		}

		private void EmitRevealCard(int playerIndex, int cardIndex, CardType type)
		{
			foreach (Listener listener in m_listeners)
				listener.OnRevealCard(playerIndex, cardIndex, type);
		}

		private void EmitEndOfTurn(EndOfRoundType endOfRoundType)
		{
			foreach (Listener listener in m_listeners)
				listener.OnEndOfRound(endOfRoundType);
		}
	}
}
