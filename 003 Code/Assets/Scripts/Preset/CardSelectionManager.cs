using UnityEngine;
using System.Collections.Generic;

public class CardSelectionManager : MonoBehaviour
{
    [SerializeField] private int maxSelection = 3;
    private List<GameObject> selectedCards = new List<GameObject>();

    public bool CanSelectMoreCards => selectedCards.Count < maxSelection;
    public int SelectedCardCount => selectedCards.Count;
    public List<GameObject> SelectedCards => selectedCards;

    public void SelectCard(GameObject card)
    {
        if (!CanSelectMoreCards) return;

        Outline outline = card.GetComponent<Outline>();
        if (outline == null)
        {
            outline = card.AddComponent<Outline>();
        }
        outline.OutlineColor = Color.yellow;
        outline.OutlineWidth = 5f;

        selectedCards.Add(card);
    }

    public void ClearSelection()
    {
        foreach (var card in selectedCards)
        {
            if (card != null)
            {
                Destroy(card.GetComponent<Outline>());
            }
        }
        selectedCards.Clear();
    }
} 