using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Linq;

public enum GameInput { Main, ColorPick, Winner }
public enum Layer { Player = 9, ColorPick = 10 }


public class OnDeckFinished : UnityEvent
{

}

public class View : MonoBehaviour
{
    public static GameInput GameInput = GameInput.Main;

    public Board Board;
    public Player Player;

    private bool DeckDone = false;
    private Card m_colorPickerCard;

    private Dictionary<Color, Card> m_colorPickSet;

    private Coroutine m_coroutineHighlightCard;
    private Coroutine m_coroutineHandMovement;
    private Coroutine m_coroutineColorPickerMovement;
    private Coroutine m_coroutineColorPickerHighlight;
    private Card m_lastSelectedCard;

    private OnDeckFinished m_onDeckFinished = new OnDeckFinished();
    
    // Start is called before the first frame update
    void Start()
    {
        m_colorPickSet = new Dictionary<Color, Card>();
        Init();
        Board.OnMove = MoveTurtle;
        Board.OnDeckBuilt = PlaceCardsInDeck;
        Board.OnCardCreated = InitializeCard;
        Board.OnTileCreated = MoveInTile;
        Board.OnCardDiscarded = DiscardCard;
        Board.OnWinner = ShowWinnerDialog;
        Board.OnTurtleCreated = InitializeTurtle;
        Player.OnPickCard = MoveCardToHand;
        Player.OnPickNonColorCard = ShowColorPickDialog;
        
    }

    // Update is called once per frame
    void Update()
    {
        switch (GameInput)
        {
            case GameInput.Main: MainInput(); break;
            case GameInput.ColorPick: ColorPickInput(); break;
            case GameInput.Winner: break;
        }
    }

    private void Init()
    {
        GameObject colorPicker = new GameObject("ColorPicker");
        colorPicker.transform.SetParent(transform);
        colorPicker.transform.localRotation = Quaternion.identity;
        colorPicker.transform.localPosition = new Vector3(-5f, 0, 7f);

        Card cardPrefab = Resources.Load<Card>("Cards/Card");

        for (int i = 0; i < Enum.GetNames(typeof(Color)).Length; i++)
        {
            if ((Color)i == Color.None) continue;

            CreateCard((Color)i);

        }

        void CreateCard(Color color)
        {
            
            var card = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
            card.transform.SetParent(colorPicker.transform);
            card.Color = color;
            card.gameObject.layer = (int)Layer.ColorPick;
            card.enabled = false;
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;
            m_colorPickSet.Add(color, card);
        }
    }

    private void MainInput()
    {
        Card selectedCard;
        if (IsMouseCursorOnCard(out selectedCard, Layer.Player))        {
            if (Input.GetMouseButtonDown(0) && DeckDone)
            {
                Player.PlayCard(selectedCard);
                return;
            }

            if (m_lastSelectedCard == null || m_lastSelectedCard.gameObject != selectedCard.gameObject)
            {
                HighLightCard(selectedCard);
                m_lastSelectedCard = selectedCard;
            }
        }
        else if (m_lastSelectedCard != null)
        {
            RestoreHand();
            m_lastSelectedCard = null;
        }
    }

    private void ColorPickInput()
    {
        Card selectedCard;

        if (IsMouseCursorOnCard(out selectedCard, Layer.ColorPick))
        {
            if (Input.GetMouseButtonDown(0))
            {
                m_colorPickerCard.Color = selectedCard.Color;
                Player.PlayCard(m_colorPickerCard);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            HideColorPickDialog();
            GameInput = GameInput.Main;
        }
    }

    private void MoveHand(float to)
    {
        if (m_coroutineHandMovement != null)
        {
            StopCoroutine(m_coroutineHandMovement);
        }

        var hand = Player.GetHand();

        m_coroutineHandMovement = StartCoroutine(go());

        IEnumerator go()
        {
            for(float time = 0; time < 1; time += Time.deltaTime)
            {
                var position = hand.transform.localPosition;
                position.y = Mathf.Lerp(position.y, to, time);
                hand.transform.localPosition = position;
                yield return null;
            }

            var finalPosition = hand.transform.localPosition;
            finalPosition.y = to;
            hand.transform.localPosition = finalPosition;
        }
    }

    private void ShowWinnerDialog(Turtle turtle)
    {
        GameInput = GameInput.Winner;

        StartCoroutine(go());
        StartCoroutine(fadeOtherTurtles());
        StartCoroutine(rotateAround());

        var hand = Player.GetHand();
        hand.gameObject.SetActive(false);
        

        IEnumerator go()
        {
            var camera = Camera.main;
            camera.transform.LookAt(turtle.transform);
            
            for (float time = 0; time < 1; time += Time.deltaTime)
            {
                Vector3 direction = Vector3.Normalize(turtle.transform.position - camera.transform.position);
                camera.transform.LookAt(turtle.transform);
                camera.transform.position = Vector3.Lerp(camera.transform.position, turtle.transform.position - direction * 2, time);
                yield return null;
            }
        }

        IEnumerator rotateAround()
        {
            var camera = Camera.main;
            while (true)
            {
                camera.transform.LookAt(turtle.transform);
                camera.transform.RotateAround(turtle.transform.position, Vector3.up, Time.deltaTime * 20);
                yield return null;
            }
            
        }

        IEnumerator fadeOtherTurtles()
        {
            var turtles = Board.GetTurtles();
            for (float time = 0; time < 1; time += Time.deltaTime)
            {
                foreach (Turtle t in turtles)
                {
                    if (t.gameObject != turtle.gameObject)
                    {
                        var material = t.GetComponent<Renderer>().material;
                        var color = material.color;
                        color.a = Mathf.Lerp(1, 0, time);
                        material.color = color;
                    }
                }

                yield return null;
            }
        }
    }
    private void ShowColorPickDialog(Card card)
    {
        Color[] colors;

        switch (card.Type)
        {
            case CardType.Last: colors = Board.LastColors(); break;
            default: colors = Board.AllColors(); break;
        }

        Card[] cards = new Card[colors.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            Color color = colors[i];
            Card c;
            if (!m_colorPickSet.TryGetValue(color, out c))
            {
                Debug.Log("Couldn't find color pick card of color " + color);
                return;
            }
            c.transform.localPosition = Vector3.zero;
            c.transform.localRotation = Quaternion.identity;
            c.gameObject.SetActive(true);
            c.Effect = card.Effect;
            c.Type = card.Type;
            InitializeCard(c);
            cards[i] = c;
        }

        if (m_coroutineColorPickerMovement != null)
        {
            StopCoroutine(m_coroutineColorPickerMovement);
        }

        m_coroutineColorPickerMovement = StartCoroutine(go());

        m_colorPickerCard = card;
        MoveHand(-1.5f);
        GameInput = GameInput.ColorPick;

        IEnumerator go()
        {
            var start = new Vector3(5 - (colors.Length - 1 ) * 0.5f, 0, 0);
            var offset = new Vector3(1f, 0, 0);
            var rotation = Quaternion.Euler(0, 180, 0);
            for (float time = 0; time < 1; time += Time.deltaTime)
            {
                for(int i = 0; i < cards.Length; i++)
                {
                    var c = cards[i];
                    var dest = start + offset * i;
                    c.transform.localPosition = Vector3.Lerp(c.transform.localPosition, dest, time);
                    c.transform.localRotation = Quaternion.Lerp(c.transform.localRotation, rotation, time);
                }
                yield return null;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                var c = cards[i];
                var dest = start + offset * i;
                c.transform.localPosition = dest;
                c.transform.localRotation = rotation;
            }
        }
    }

    private void HideColorPickDialog()
    {
        foreach (KeyValuePair<Color, Card> pair in m_colorPickSet)
        {
            var card = pair.Value;
            card.gameObject.SetActive(false);
        }
        MoveHand(-1.0f);
    }
   
    private void HighLightCard(Card selectedCard)
    {
        if (m_coroutineHighlightCard != null) StopCoroutine(m_coroutineHighlightCard);
        m_coroutineHighlightCard = StartCoroutine(go());

        IEnumerator go()
        {
            var cards = Player.GetHand().GetComponentsInChildren<Card>();
            var offset = 0.4f;

            foreach (Card card in cards)
            {
                var position = card.transform.localPosition;
                position.z = 0;
                card.transform.localPosition = position;                
            }

            for (float time = 0; time < 1; time += Time.deltaTime * 10)
            {
                var position = selectedCard.transform.localPosition;
                position.z = Mathf.Lerp(0, offset, time);
                selectedCard.transform.localPosition = position;
                yield return null;
            }
        }
    }

    private void RestoreHand()
    {
        if (m_coroutineHighlightCard != null) StopCoroutine(m_coroutineHighlightCard);
        m_coroutineHighlightCard = StartCoroutine(go());

        IEnumerator go()
        {
            var cards = Player.GetHand().GetComponentsInChildren<Card>();

            for (float time = 0; time < 1; time += Time.deltaTime * 10)
            {
                foreach (Card card in cards)
                {
                    var position = card.transform.localPosition;
                    position.z = Mathf.Lerp(position.z, 0, time);
                    card.transform.localPosition = position;
                }
                yield return null;
            }

            foreach (Card card in cards)
            {
                var position = card.transform.localPosition;
                position.z = 0;
                card.transform.localPosition = position;
            }
        }
    }

    private bool IsMouseCursorOnCard(out Card card, Layer layer)
    {
        card = null;
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << (int)layer))
        {
            return false;            
        }

        card = hit.transform.gameObject.GetComponent<Card>();

        return card != null;
    }

    private void MoveInTile(Transform tile, float delay)
    {
        StartCoroutine(go());

        IEnumerator go ()
        {
            yield return new WaitForSeconds(delay);

            var destination = tile.transform.position;
            destination.y = 0;

            //Set the main Color of the Material to green
            var renderer = tile.GetComponent<Renderer>();
            var color = renderer.material.color;
            color.a = 0;
            renderer.material.color = color;

            renderer.enabled = true;

            for (float time = 0; time < 1; time += Time.deltaTime)
            {
                color = renderer.material.color;
                color.a = Mathf.Lerp(color.a, 1, time);
                renderer.material.color = color;
                tile.transform.position = Vector3.Lerp(tile.transform.position, destination, time);
                yield return null;
            }

            tile.transform.position = destination;
        }
    }

    private void InitializeCard(Card card)
    {
        var frontRenderer = card.GetComponentsInChildren<MeshRenderer>().First(c => c.name == "Front");
        var turtleMaterial = frontRenderer.materials[1];
        var effectMaterial= frontRenderer.materials[2];

        turtleMaterial.color = ToUnityColor(card.Color);

        Texture plus_one = card.Type == CardType.Last? Resources.Load<Texture>("Cards/Materials/plus_one_last") : Resources.Load<Texture>("Cards/Materials/plus_one");
        Texture plus_two = card.Type == CardType.Last ? Resources.Load<Texture>("Cards/Materials/plus_two_last") : Resources.Load<Texture>("Cards/Materials/plus_two");
        Texture minus_one = Resources.Load<Texture>("Cards/Materials/minus_one");

        switch (card.Effect)
        {
            case CardEffect.Forward: effectMaterial.SetTexture("_MainTex", plus_one); break;
            case CardEffect.DoubleForward: effectMaterial.SetTexture("_MainTex", plus_two); break;
            case CardEffect.Back: effectMaterial.SetTexture("_MainTex", minus_one);  break;
        }
        
    }

    private void PlaceCardsInDeck(Card[] cards)
    {
        StartCoroutine(go());

        IEnumerator go()
        {

            var cardThickness = 0.005f;
            Coroutine last = null;
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].transform.localPosition += Vector3.up * 10;
                last = StartCoroutine(FlyIn(cards[i], i));
            }

            StartCoroutine(PlaySounds());

            yield return last;

            DeckDone = true;
            m_onDeckFinished.Invoke();
            
            IEnumerator FlyIn(Card card, int i)
            {
                var destination = Vector3.up * cardThickness * i;
                var rotation = new Vector3(90, 0, 0);
                var delay = 0.01f * i;

                yield return new WaitForSeconds(delay);

                for (var time = 0.0f; time < 1; time += Time.deltaTime)
                {
                    card.transform.localPosition = Vector3.Lerp(card.transform.localPosition, destination, time);
                    card.transform.localEulerAngles = Vector3.Lerp(card.transform.localEulerAngles, rotation, time);
                    yield return null;
                }

                card.transform.localPosition = destination;
                card.transform.localEulerAngles = rotation;
            }

            IEnumerator PlaySounds()
            {
                for (int i = 0; i < 7; i++)
                {
                    var audio = Board.GetComponent<AudioSource>();
                    audio.Play();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
    }

    private void MoveCardToHand(Card card, Hand hand)
    {
        
        StartCoroutine(go());

        IEnumerator go()
        {
            while(!DeckDone)
            {
                yield return null;
            }

            var audio = Board.GetComponent<AudioSource>();
            audio.Play();

            DeckDone = false;
            var cards = hand.GetComponentsInChildren<Card>();

            card.transform.SetParent(hand.transform);
            var cardSpacing = 0.7f;
            var rightCardPosition = (cardSpacing / 2) * cards.Length;
            var rotation = Quaternion.Euler(-90, 180, 0);
            var maxOffset = Vector3.right * (rightCardPosition - cards.Length * cardSpacing);

            for (var time = 0.0f; time < 0.7f; time += Time.deltaTime)
            {
                
                card.transform.localPosition = Vector3.Lerp(card.transform.localPosition, maxOffset, time);
                card.transform.localRotation = Quaternion.Lerp(card.transform.localRotation, rotation, time);

                for (int i = 0; i < cards.Length; i++)
                {
                    var position = cards[i].transform.localPosition;
                    position.x = Mathf.Lerp(position.x, rightCardPosition - i * cardSpacing, time);
                    cards[i].transform.localPosition = position;
                }
                yield return null;
            }
            
            card.transform.localPosition = maxOffset;
            card.transform.localRotation = rotation;
            for (int i = 0; i < cards.Length; i++)
            {
                var position = cards[i].transform.localPosition;
                position.x = rightCardPosition - i * cardSpacing;
                cards[i].transform.localPosition = position;
            }

            card.gameObject.layer = (int)Layer.Player;

            DeckDone = true;
        }
    }

    private void MoveTurtle(Transform turtle, Transform tile)
    {
        
        StartCoroutine(go());    

        IEnumerator go()
        {
            RaycastHit hit;

            Vector3 start = turtle.position;
            if (!Physics.Raycast(tile.position + Vector3.up, Vector3.down, out hit, Mathf.Infinity))
            {
                Debug.Log("Didn't hit anything!");
                yield break;
            }

            var dest = hit.point + new Vector3(0, 0.15f, 0);


            for (float i = 0; i < 1; i += Time.deltaTime * 4)
            {
                turtle.position = Vector3.Lerp(start, dest, i) + (Vector3.up * Mathf.Sin(i * Mathf.PI) / 4);
                yield return null;
            }

            turtle.position = dest;
        }
    }

    private void DiscardCard(Card card)
    {
        card.gameObject.layer = 0;
        card.transform.SetParent(Board.transform);
        card.gameObject.SetActive(false);
    }

    private void InitializeTurtle(Turtle turtle)
    {
        var material = turtle.GetComponent<Renderer>().material;

        material.color = ToUnityColor(turtle.Color);
    }

    private UnityEngine.Color ToUnityColor(Color color)
    {
        switch (color)
        {
            case Color.Red: return UnityEngine.Color.red; 
            case Color.Blue: return UnityEngine.Color.blue; 
            case Color.Purple: return new UnityEngine.Color(0.5f, 0, 0.5f, 1); 
            case Color.Pink: return new UnityEngine.Color(1, 0.498f, 0.611f, 1); 
            case Color.Green: return UnityEngine.Color.green; 
            default: return UnityEngine.Color.white;
        }
    }
}
