using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class DivineSerializator
{
	enum Call : int
	{
		Start,
		UseCard,
		DrawCard,
		ExchangeCard
	}

	public delegate void StartDelegate(int seed, int[] playerSet);
    public delegate void UseCardDelegate(int cardIndex, int[] extraParms);
	public delegate void DrawCardDelegate(int cardIndex);
	public delegate void ExchangeCardDelegate(int cardIndex1, int cardIndex2);

	static public byte[] Start(int seed, params int[] playerSet)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(memoryStream);

		writer.Write((int) Call.Start);
		writer.Write(seed);
		writer.Write((int)playerSet.Length);
		for (int i = 0; i < playerSet.Length; i++)
			writer.Write(playerSet[i]);

		return memoryStream.ToArray();
	}

	static public byte[] UseCard(int cardIndex, params int[] extraParms)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(memoryStream);

		writer.Write((int)Call.UseCard);
		writer.Write(cardIndex);

		writer.Write((int)extraParms.Length);
		for (int i = 0; i < extraParms.Length; i++)
			writer.Write(extraParms[i]);

		return memoryStream.ToArray();
	}

	static public byte[] DrawCard(int cardIndex)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(memoryStream);

		writer.Write((int)Call.DrawCard);
		writer.Write(cardIndex);

		return memoryStream.ToArray();
	}

	static public byte[] ExchangeCard(int cardIndex1, int cardIndex2)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(memoryStream);

		writer.Write((int)Call.ExchangeCard);
		writer.Write(cardIndex1);
		writer.Write(cardIndex2);

		return memoryStream.ToArray();
	}

	static public void Deserialize(byte[] serialization, 
		StartDelegate start, 
		UseCardDelegate useCard,
		DrawCardDelegate drawCard,
		ExchangeCardDelegate exchangeCard)
	{
		if (serialization.Length < sizeof(int))
		{
			UnityEngine.Debug.Log("Invalid packet for deserialize.");
			return;
		}

		MemoryStream memoryStream = new MemoryStream(serialization);
		BinaryReader reader = new BinaryReader(memoryStream);

		Call call = (Call) reader.ReadInt32();
		switch(call)
		{
			case Call.Start:
				{
					int seed = reader.ReadInt32();
					int players = reader.ReadInt32();
					int[] playerSet = new int[players];
					for (int i = 0; i < playerSet.Length; i++)
						playerSet[i] = reader.ReadInt32();

					if (start != null)
						start(seed, playerSet);
				}
				break;
			case Call.UseCard:
				{
					int cardIndex = reader.ReadInt32();
					int extraParamsLength = reader.ReadInt32();
					int[] extraParams = new int[extraParamsLength];
					for (int i = 0; i < extraParams.Length; i++)
						extraParams[i] = reader.ReadInt32();

					if (useCard != null)
						useCard(cardIndex, extraParams);
				}
				break;
			case Call.DrawCard:
				{
					int cardIndex = reader.ReadInt32();
					if (drawCard != null)
						drawCard(cardIndex);
				}
				break;
			case Call.ExchangeCard:
				{
					int cardIndex1 = reader.ReadInt32();
					int cardIndex2 = reader.ReadInt32();
					if (exchangeCard != null)
						exchangeCard(cardIndex1, cardIndex2);
				}
				break;
		}
	}

}

