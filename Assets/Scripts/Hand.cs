using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnCardSelected : UnityEvent<Card>
{

}

public class OnCardDeselected : UnityEvent<Card>
{

}

public class Hand : MonoBehaviour
{
    private List<Card> m_cards = new List<Card>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddCard(Card card) => m_cards.Add(card);
    public Card[] GetCards() => m_cards.ToArray();
    public bool RemoveCard(Card card)
    {
        if (m_cards.Contains(card))
        {
            m_cards.Remove(card);
            return true;
        }

        return false;
    }


}
