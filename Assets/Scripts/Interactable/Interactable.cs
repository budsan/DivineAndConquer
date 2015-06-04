using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Interactable 
	:
	MonoBehaviour,
	IMoveHandler,
	IPointerDownHandler, IPointerUpHandler,
	IPointerEnterHandler, IPointerExitHandler,
	ISelectHandler, IDeselectHandler
{

	// List of all the interactable objects currently active in the scene
	private static List<Interactable> s_List = new List<Interactable>();
	public static List<Interactable> allInteractables { get { return s_List; } }

	[Tooltip("Can the Interactable be interacted with?")]
	[SerializeField]
	private bool m_Interactable = true;

	protected enum SelectionState
	{
		Normal,
		Highlighted,
		Pressed,
		Disabled
	}

	private SelectionState m_CurrentSelectionState;

	private bool isPointerInside { get; set; }
	private bool isPointerDown { get; set; }
	private bool hasSelection { get; set; }

	protected Interactable()
	{
	}

	public virtual bool IsInteractable()
	{
		return m_Interactable;
	}

	public virtual bool IsActive()
	{
		return enabled && isActiveAndEnabled && gameObject.activeInHierarchy;
	}

	protected virtual void OnEnable()
	{
		s_List.Add(this);
		var state = SelectionState.Normal;

		if (hasSelection)
			state = SelectionState.Highlighted;

		m_CurrentSelectionState = state;
		InternalEvaluateAndTransitionToSelectionState(true);
	}

	private void OnSetProperty()
	{
#if UNITY_EDITOR
            if (!Application.isPlaying)
                InternalEvaluateAndTransitionToSelectionState(true);
            else
#endif
		InternalEvaluateAndTransitionToSelectionState(false);
	}

	protected virtual void OnDisable()
	{
		s_List.Remove(this);
		InstantClearState();
	}

	protected SelectionState currentSelectionState
	{
		get { return m_CurrentSelectionState; }
	}

	protected virtual void InstantClearState()
	{
		isPointerInside = false;
		isPointerDown = false;
		hasSelection = false;
	}

	protected virtual void DoStateTransition(SelectionState state, bool instant)
	{
	}

	public Interactable FindInteractable(Vector3 dir)
	{
		dir = dir.normalized;
		Vector3 localDir = Quaternion.Inverse(transform.rotation) * dir;
		Vector3 pos = transform.position;
		float maxScore = Mathf.NegativeInfinity;
		Interactable bestPick = null;
		for (int i = 0; i < s_List.Count; ++i)
		{
			Interactable sel = s_List[i];

			if (sel == this || sel == null)
				continue;

			if (!sel.IsInteractable())
				continue;

			var selRect = sel.transform as RectTransform;
			Vector3 selCenter = selRect != null ? (Vector3)selRect.rect.center : Vector3.zero;
			Vector3 myVector = sel.transform.TransformPoint(selCenter) - pos;

			// Value that is the distance out along the direction.
			float dot = Vector3.Dot(dir, myVector);

			// Skip elements that are in the wrong direction or which have zero distance.
			// This also ensures that the scoring formula below will not have a division by zero error.
			if (dot <= 0)
				continue;

			// This scoring function has two priorities:
			// - Score higher for positions that are closer.
			// - Score higher for positions that are located in the right direction.
			// This scoring function combines both of these criteria.
			// It can be seen as this:
			//   Dot (dir, myVector.normalized) / myVector.magnitude
			// The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
			// The second part scores lower the greater the distance is by dividing by the distance.
			// The formula below is equivalent but more optimized.
			//
			// If a given score is chosen, the positions that evaluate to that score will form a circle
			// that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
			// From the position pos, blow up a circular balloon so it grows in the direction of dir.
			// The first Interactable whose center the circular balloon touches is the one that's chosen.
			float score = dot / myVector.sqrMagnitude;

			if (score > maxScore)
			{
				maxScore = score;
				bestPick = sel;
			}
		}
		return bestPick;
	}

	void Navigate(AxisEventData eventData, Interactable inter)
	{
		if (inter != null && inter.IsActive())
			eventData.selectedObject = inter.gameObject;
	}

	public virtual Interactable FindInteractableOnLeft()
	{
		return FindInteractable(transform.rotation * Vector3.left);
	}

	public virtual Interactable FindInteractableOnRight()
	{
		return FindInteractable(transform.rotation * Vector3.right);
	}

	public virtual Interactable FindInteractableOnUp()
	{

		return FindInteractable(transform.rotation * Vector3.up);
	}

	public virtual Interactable FindInteractableOnDown()
	{
		return FindInteractable(transform.rotation * Vector3.down);
	}

	public virtual void OnMove(AxisEventData eventData)
	{
		switch (eventData.moveDir)
		{
			case MoveDirection.Right:
				Navigate(eventData, FindInteractableOnRight());
				break;

			case MoveDirection.Up:
				Navigate(eventData, FindInteractableOnUp());
				break;

			case MoveDirection.Left:
				Navigate(eventData, FindInteractableOnLeft());
				break;

			case MoveDirection.Down:
				Navigate(eventData, FindInteractableOnDown());
				break;
		}
	}

	protected bool IsHighlighted(BaseEventData eventData)
	{
		if (!IsActive())
			return false;

		if (IsPressed())
			return false;

		bool selected = hasSelection;
		if (eventData is PointerEventData)
		{
			var pointerData = eventData as PointerEventData;
			selected |=
				(isPointerDown && !isPointerInside && pointerData.pointerPress == gameObject)
				|| (!isPointerDown && isPointerInside && pointerData.pointerPress == gameObject) 
				|| (!isPointerDown && isPointerInside && pointerData.pointerPress == null);
		}
		else
		{
			selected |= isPointerInside;
		}
		return selected;
	}

	protected bool IsPressed()
	{
		if (!IsActive())
			return false;

		return isPointerInside && isPointerDown;
	}

	protected void UpdateSelectionState(BaseEventData eventData)
	{
		if (IsPressed())
		{
			m_CurrentSelectionState = SelectionState.Pressed;
			return;
		}

		if (IsHighlighted(eventData))
		{
			m_CurrentSelectionState = SelectionState.Highlighted;
			return;
		}

		m_CurrentSelectionState = SelectionState.Normal;
	}


	private void EvaluateAndTransitionToSelectionState(BaseEventData eventData)
	{
		if (!IsActive())
			return;

		UpdateSelectionState(eventData);
		InternalEvaluateAndTransitionToSelectionState(false);
	}

	private void InternalEvaluateAndTransitionToSelectionState(bool instant)
	{
		var transitionState = m_CurrentSelectionState;
		if (IsActive() && !IsInteractable())
			transitionState = SelectionState.Disabled;
		DoStateTransition(transitionState, instant);
	}

	public virtual void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		if (IsInteractable())
			EventSystem.current.SetSelectedGameObject(gameObject, eventData);

		isPointerDown = true;
		EvaluateAndTransitionToSelectionState(eventData);
	}

	public virtual void OnPointerUp(PointerEventData eventData)
	{
		if (eventData.button != PointerEventData.InputButton.Left)
			return;

		isPointerDown = false;
		EvaluateAndTransitionToSelectionState(eventData);
	}

	public virtual void OnPointerEnter(PointerEventData eventData)
	{
		isPointerInside = true;
		EvaluateAndTransitionToSelectionState(eventData);
	}

	public virtual void OnPointerExit(PointerEventData eventData)
	{
		isPointerInside = false;
		EvaluateAndTransitionToSelectionState(eventData);
	}

	public virtual void OnSelect(BaseEventData eventData)
	{
		hasSelection = true;
		EvaluateAndTransitionToSelectionState(eventData);
	}

	public virtual void OnDeselect(BaseEventData eventData)
	{
		hasSelection = false;
		EvaluateAndTransitionToSelectionState(eventData);
	}

	public virtual void Select()
	{
		if (EventSystem.current.alreadySelecting)
			return;

		EventSystem.current.SetSelectedGameObject(gameObject);
	}
}
