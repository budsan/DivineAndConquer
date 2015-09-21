using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


class CardContentManager : MonoBehaviour
{
	[SerializeField] private GameObject Unknown;
	[SerializeField] private GameObject FirstOration;
	[SerializeField] private GameObject SecondOration;
	[SerializeField] private GameObject ThirdOration;
	[SerializeField] private GameObject Curse;
	[SerializeField] private GameObject Radar;
	[SerializeField] private GameObject AntiRadar;
	[SerializeField] private GameObject Shuffle;
	[SerializeField] private GameObject RevealCard;
	[SerializeField] private GameObject Bounce;

	private GameObject[] Cards;

	public void Awake()
	{
		Cards = new GameObject[(int) Divine.CardType.Count];
		Cards[(int)Divine.CardType.Unknown] = Unknown;
		Cards[(int)Divine.CardType.FirstOration] = FirstOration;
		Cards[(int)Divine.CardType.SecondOration] = SecondOration;
		Cards[(int)Divine.CardType.ThirdOration] = ThirdOration;
		Cards[(int)Divine.CardType.Curse] = Curse;
		Cards[(int)Divine.CardType.Radar] = Radar;
		Cards[(int)Divine.CardType.AntiRadar] = AntiRadar;
		Cards[(int)Divine.CardType.Shuffle] = Shuffle;
		Cards[(int)Divine.CardType.RevealCard] = RevealCard;
		Cards[(int)Divine.CardType.Bounce] = Bounce;
	}

	public void Start()
	{
		RestoreAll();
	}

	public GameObject GetCardFromType(Divine.CardType type)
	{
		if (type == Divine.CardType.None)
			Debug.Log("Divine.CardType.None is not a valid type");

		GameObject card = Cards[(int)type];
		card.SetActive(true);
		return card;
	}

	public void RestoreCardFromType(Divine.CardType type)
	{
		if (type == Divine.CardType.None)
			Debug.Log("Divine.CardType.None is not a valid type");

		GameObject card = Cards[(int)type];
		if (card != null)
			Restore(card);
	}

	public void RestoreAll()
	{
		foreach(GameObject card in Cards)
			if (card != null)
				Restore(card);
	}

	private void Restore(GameObject card)
	{
		card.transform.SetParent(transform, false);
		card.transform.localPosition = Vector3.zero;
		card.transform.localScale = Vector3.one;
		card.transform.localRotation = Quaternion.identity;
		card.SetActive(false);
	}
}

