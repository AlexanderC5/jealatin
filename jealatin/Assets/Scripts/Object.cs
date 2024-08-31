using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object : MonoBehaviour 
{
    SpriteRenderer ObjectSprite;

    [SerializeField] private Enums.Color defaultColor;
    private Vector2 defaultPos;

    private int color; // R Y B color1 of object
    public int Color
    {
        get => color;
        private set => color = value;
    }
    private Stack<Enums.Color> colorStack = new Stack<Enums.Color>(); // Stores the object's color history to allow for undoing

    void Awake()
    {
        ObjectSprite = GetComponent<SpriteRenderer>();
        this.transform.position = new Vector3(Mathf.Round(this.transform.position.x), Mathf.Round(this.transform.position.y), 0);
        defaultPos = this.transform.position;
    }

    void Start()
    {
        SetColor(defaultColor, true, true);
    }

    void Update()
    {
        if (GameManager.Instance.GameMode == Enums.GameMode.Game && Input.GetKeyDown(KeyCode.R))
        {
            this.FullReset();
        }
    }

    public void SetColor(Enums.Color color, bool addToColorStack, bool updateColor = false)
    {
        if (updateColor) { Color = (int) color; }
        
        if (addToColorStack) { colorStack.Push(color); }

        ObjectSprite.color = GameManager.Instance.Palette[color];
    }

    public bool CanBeColored()
    {
        if (color != (int) Enums.Color.Black) return true;
        return false;
    }

    public bool CanGiveColor()
    {
        if (color != (int) Enums.Color.None) return true;
        return false;
    }

    public void MoveObject(Enums.Action moveDir)
    {
        Vector2 nextDir = Vector2.zero;
        switch (moveDir)
        {
            case Enums.Action.North:
                nextDir = Vector2.up;
                break;
            case Enums.Action.East:
                nextDir = Vector2.right;
                break;
            case Enums.Action.South:
                nextDir = Vector2.down;
                break;
            case Enums.Action.West:
                nextDir = Vector2.left;
                break;
        }
        StartCoroutine(MoveObjectCoroutine(nextDir));
    }

    IEnumerator MoveObjectCoroutine(Vector2 moveDir)
    {
        this.transform.position += (Vector3) moveDir;
        yield return new WaitForSeconds(0.2f);
    }

    public void UndoMove(Vector2 dirMoved)
    {
        StartCoroutine(MoveObjectCoroutine(-dirMoved));
    }
    
    public void UndoColor()
    {
        this.colorStack.Pop();
        SetColor(this.colorStack.Peek(), false, true);
    }

    public void ResetObjectLocation()
        {
            transform.position = defaultPos;
            // TODO: Update facing direction
        }

    private void FullReset() // When 'R' key pressed. Cannot be undone
    {
        // Reset object position
        ResetObjectLocation();
        
        // Reset object color & add to colorStack
        colorStack.Clear();
        SetColor(defaultColor, true, true);
    }
}
