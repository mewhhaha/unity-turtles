using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnPickCard : UnityEvent<Card, Hand>
{

}

public class OnPickNonColorCard : UnityEvent<Card>
{

}



public class Player : MonoBehaviour
{
    public Board Board;
    
    public UnityAction<Card, Hand> OnPickCard { set => m_onPickCard.AddListener(value); }
    public UnityAction<Card> OnPickNonColorCard { set => m_onPickNonColorCard.AddListener(value); }

    private Hand m_hand;
    private Color Color;
    private OnPickCard m_onPickCard = new OnPickCard();
    private OnPickNonColorCard m_onPickNonColorCard = new OnPickNonColorCard();



    // Start is called before the first frame update
    void Start()
    {
        Board.OnBuilt = InitPlayer;
        var hand = new GameObject("Hand");
        hand.AddComponent<Hand>();
        hand.transform.SetParent(transform);
        hand.transform.localPosition = new Vector3(0, -1f, 4.5f);
        m_hand = hand.GetComponent<Hand>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void PlayCard(Card card)
    {
        if (card.Color == Color.None)
        {
            m_onPickNonColorCard.Invoke(card);
            return;
        }
        
        Board.PlayCard(card);
        var newCard = Board.PickCard();
        m_hand.AddCard(newCard);
        m_onPickCard.Invoke(newCard, m_hand);
        m_hand.RemoveCard(card);
    }

    public void InitPlayer()
    {
        for (int i = 0; i < 5; i++)
        {
            var card = Board.PickCard();
            m_hand.AddCard(card);
            m_onPickCard.Invoke(card, m_hand);
        }
    }

    public Hand GetHand()
    {
        return m_hand;
    }
}
