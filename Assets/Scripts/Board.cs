using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public enum Path { Start=0, One, Two, Three, Four, Five, Six, Seven, Eight, Goal }
public enum Color { None = 0, Red, Blue, Purple, Green, Pink }
public enum CardType { Color=0, Any, Last }
public enum CardEffect { Forward, DoubleForward, Back }

public class OnMove : UnityEvent<Transform, Transform>
{

}

public class OnCardDiscarded : UnityEvent<Card>
{

}

public class OnBuilt : UnityEvent
{

}

public class OnDeckBuilt : UnityEvent<Card[]>
{

}

public class OnCardCreated : UnityEvent<Card>
{

}

public class OnTileCreated : UnityEvent<Transform, float>
{

}

public class OnWinner : UnityEvent<Turtle>
{

}

public class Board : MonoBehaviour
{
    public GameObject StartTile;
    public GameObject GoalTile;
    private OnMove m_onMove = new OnMove();
    private OnBuilt m_onBuilt = new OnBuilt();
    private OnDeckBuilt m_onDeckBuilt = new OnDeckBuilt();
    private OnCardCreated m_onCardCreated = new OnCardCreated();
    private OnCardDiscarded m_onCardDiscarded = new OnCardDiscarded();
    private OnTileCreated m_onTileCreated = new OnTileCreated();
    private OnWinner m_onWinner = new OnWinner();

    public UnityAction<Turtle> OnWinner
    {
        set => m_onWinner.AddListener(value);
    }
    public UnityAction<Transform, Transform> OnMove
    {
        set => m_onMove.AddListener(value);
    }

    public UnityAction OnBuilt
    {
        set => m_onBuilt.AddListener(value);
    }

    public UnityAction<Card[]> OnDeckBuilt
    {
        set => m_onDeckBuilt.AddListener(value);
    }

    public UnityAction<Card> OnCardCreated
    {
        set => m_onCardCreated.AddListener(value);
    }

    public UnityAction<Transform, float> OnTileCreated
    {
        set => m_onTileCreated.AddListener(value);
    }

    public UnityAction<Card> OnCardDiscarded
    {
        set => m_onCardDiscarded.AddListener(value);
    }

    private Tile[] m_path;
    private Dictionary<Color, Turtle> m_turtles;
    private Stack<Card> m_deck;
    private Stack<Card> m_discarded;

    void Start()
    {
        BuildTurtles();
        BuildTiles(8);
        BuildDeck();
        var turtles = FindObjectsOfType<Turtle>();
        var path = FindObjectsOfType<Tile>();

        m_turtles = new Dictionary<Color, Turtle>();
        m_path = path;

        Array.Sort(m_path, (t1, t2) => t1.Position - t2.Position);      
        Array.ForEach(turtles, t => {
            m_turtles.Add(t.Color, t);
            t.transform.SetParent(path[0].transform, false);
            });
     
        StartCoroutine(LateStart());     
 
        IEnumerator LateStart()
        {
            yield return null;
            m_onBuilt.Invoke();
        }

        OnMove = CheckWinner;
        
    }

    private void CheckWinner(Transform turtle, Transform dest)
    {
        var goal = m_path[m_path.Length - 1];
        var winner = goal.GetComponentInChildren<Turtle>();
        if (winner != null)
        {
            m_onWinner.Invoke(winner);
        }
    }

    public Card PickCard()
    {
        return m_deck.Pop();
    }

    public void MoveTurtle(Color color, CardEffect effect)
    {
        int steps = 0;

        switch (effect)
        {
            case CardEffect.Back: steps = -1; break;
            case CardEffect.Forward: steps = 1; break;
            case CardEffect.DoubleForward: steps = 2; break;
        }

        Turtle turtle;
        if (!m_turtles.TryGetValue(color, out turtle))
        {
            Debug.Log("Trying to access turtle that doesn't exist: " + color);
            return;
        }

        int current = (int)GetPosition(turtle);
        int next = Mathf.Clamp(current + steps, 0, m_path.Length - 1);

        if (current == next)
        {
            return;
        }

        Transform last = GetLastTurtle(m_path[next]);
        turtle.transform.SetParent(last);
        m_onMove.Invoke(turtle.transform, last);
    }

    public Path GetPosition(Turtle turtle)
    {
        return go(turtle.transform);

        Path go(Transform current) {
            Transform parent = current.transform.parent;
            Tile tile = parent.GetComponent<Tile>();
            if (tile != null)
            {
                return tile.Position;
            }

            return go(parent);
        }
    }

    public Transform GetLastTurtle(Tile tile)
    {
        return go(tile.transform);

        Transform go(Transform current) {
            Turtle next  = current.GetComponentsInChildren<Turtle>().Where(t => t.transform != current).FirstOrDefault();
            if(next != null)
            {
                return go(next.transform);
            }
            return current;
        }
    }

    public void BuildTurtles()
    {
        var prefabs = Resources.LoadAll<Turtle>("Hexagons/Turtles");
        var indices = Enumerable.Range(0, prefabs.Length).ToList();
        var turtles = prefabs.Zip(indices, (p, i) => Instantiate(p.gameObject, Vector3.up * 0.105f * i, Quaternion.identity)).ToList();
    }

    public void BuildDeck()
    {
        var deck = new GameObject("Deck");
        var cards = new List<Card>();
        var cardPrefab = Resources.Load<Card>("Cards/Card");

        m_deck = new Stack<Card>();
        m_discarded = new Stack<Card>();
        deck.transform.position = ToVector((0, -2)) + Vector3.up * 0.21f;

        foreach (Color color in (Color[])Enum.GetValues(typeof(Color)))
        {
            if (color == Color.None) continue;

            CreateCard(1, CardType.Color, color, CardEffect.DoubleForward);
            CreateCard(5, CardType.Color, color, CardEffect.Forward);
            CreateCard(2, CardType.Color, color, CardEffect.Back);
        }

        CreateCard(5, CardType.Any, Color.None, CardEffect.Forward);
        CreateCard(2, CardType.Any, Color.None, CardEffect.Back);

        CreateCard(2, CardType.Last, Color.None, CardEffect.DoubleForward);
        CreateCard(3, CardType.Last, Color.None, CardEffect.Forward);

        System.Random shuffle = new System.Random();
        m_deck = new Stack<Card>(cards.OrderBy(a => shuffle.Next()));

        m_onDeckBuilt.Invoke(m_deck.ToArray());

        void CreateCard(uint amount, CardType type, Color color, CardEffect effect)
        {
            for (int i = 0; i < amount; i++)
            {
                var card = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity);
                card.transform.SetParent(deck.transform);
                card.Color = color;
                card.Type = type;
                card.Effect = effect;
                cards.Add(card);
                m_onCardCreated.Invoke(card);
            }
        }
    }

    public void BuildTiles(int length)
    {
        var occupied = new HashSet<(int, int)>();

        // Build Path

        var downOffset = Vector3.down * 3;
        var pathPrefabs = Resources.LoadAll<GameObject>("Hexagons/Path");
        var grassPrefabs = Resources.LoadAll<GameObject>("Hexagons/Grass");
        var surroundings = new GameObject("Surroundings");
        var path = new GameObject("Path");
        var tileLerpSpacing = 0.1f;
        var grassLerpSpacing = 0.01f;
        var tilePosition = (x: 0, y: 0);

        surroundings.transform.SetParent(transform);
        path.transform.SetParent(transform);
        StartTile = Initialize(StartTile, (0,0), 0, path);

        for (int i = 1; i < length + 1; i++)
        {
            if (i % 2 == 0)
            {
                var rnd = UnityEngine.Random.Range(0, 2) == 0;
                tilePosition.x += rnd ? 0 : 1;
                tilePosition.y += rnd ? 1 : 0; 
            }
            else
            {
                tilePosition.x++;
                tilePosition.y++;
            }

            var prefab = pathPrefabs[i % pathPrefabs.Length];
            var delay = tileLerpSpacing * i;
            var gameObject = Initialize(prefab, tilePosition, delay, path);
            var tile = gameObject.GetComponent<Tile>();
            tile.Position = (Path)i;
        }

        tilePosition.x++;
        tilePosition.y++;
        GoalTile = Initialize(GoalTile, tilePosition, (length + 1) * tileLerpSpacing, path);
        

        // Build Grass
        System.Random shuffle = new System.Random();
        var positions = Enumerable.Range(0, 8 * 8 - 1).Select(v => (x: v / 8, y: v % 8)).OrderBy(a => shuffle.Next()).ToArray();
        
        for (int i = 0; i < positions.Length; i++)
        {
            if (occupied.Contains(positions[i]))
            {
                continue;
            }

            var exist = UnityEngine.Random.Range(0, Mathf.Abs(positions[i].x - positions[i].y)) < 1;

            if (exist)
            {
                var prefab = grassPrefabs[i % grassPrefabs.Length];
                var delay = (length + 1) * tileLerpSpacing + i * grassLerpSpacing;
                Initialize(prefab, positions[i], delay, surroundings);
            }
        }

        Initialize(grassPrefabs[0], (0, -2), 0, surroundings);

        GameObject Initialize(GameObject prefab, (int, int) position, float delay, GameObject parent)
        {
            var gameObject = Instantiate(prefab, ToVector(position) + downOffset, Quaternion.identity);
            gameObject.GetComponent<Renderer>().enabled = false;
            occupied.Add(position);
            gameObject.transform.SetParent(parent.transform);
            m_onTileCreated.Invoke(gameObject.transform, delay);
            return gameObject;
        }
    }

    public void PlayCard(Card card)
    {
        MoveTurtle(card.Color, card.Effect);
        m_discarded.Push(card);
        m_onCardDiscarded.Invoke(card);
    }

    public Vector3 ToVector((int, int) position)
    {
        var vX = new Vector3(0.5f, 0, 0.867f);
        var vY = new Vector3(0.5f, 0, -0.867f);
        var (x, y) = position;
        return x * vX + y * vY;
    }

    public Color[] AllColors() => new Color[] { Color.Blue, Color.Red, Color.Purple, Color.Green, Color.Pink };

    public Color[] LastColors()
    {
        foreach (Tile tile in m_path) {
            var colors = GetTurtleColorsOntile(tile);
            if (colors.Length > 0)
            {
                return colors;
            }
        }
        return new Color[] { };
    }

    private Color[] GetTurtleColorsOntile(Tile tile)
    {
        var colors = new List<Color>();
        var current = tile.gameObject;
        Turtle[] next = null;
        do
        {
            next = current.GetComponentsInChildren<Turtle>().Where(t => t.gameObject != current.gameObject).ToArray();
            if (next.Length > 0) {
                colors.Add(next[0].Color);
                current = next[0].gameObject;    
            }
        }
        while (next.Length > 0);

        return colors.ToArray();
    }

    public Turtle[] GetTurtles() => m_turtles.Select(p => p.Value).ToArray();
}


