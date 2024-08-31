using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object : MonoBehaviour 
{
    SpriteRenderer ObjectSprite;
    Animator ObjectAnimator;

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
        ObjectSprite = GetComponentInChildren<SpriteRenderer>();
        ObjectAnimator = GetComponentInChildren<Animator>();
        this.transform.position = new Vector3(Mathf.Round(this.transform.position.x), Mathf.Round(this.transform.position.y), 0);
        defaultPos = this.transform.position;
    }

    void Start()
    {
        SetColor(defaultColor, true, true);
        UpdateAnimationSpeed();
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
        
        // Stop any previous animations
        // ObjectAnimator.SetBool("Move Up", false);
        // ObjectAnimator.SetBool("Move Right", false);
        // ObjectAnimator.SetBool("Move Down", false);
        // ObjectAnimator.SetBool("Move Left", false);
        StartCoroutine(MoveObjectCoroutine(nextDir));
    }

    IEnumerator MoveObjectCoroutine(Vector2 moveDir, bool isAnimated = true)
    {
        this.transform.position += (Vector3) moveDir;

        if (isAnimated) // Incredibly inefficient animation code using booleans but oh well :D
        {
            ObjectAnimator.ResetTrigger("Move Up");
            ObjectAnimator.ResetTrigger("Move Right");
            ObjectAnimator.ResetTrigger("Move Down");
            ObjectAnimator.ResetTrigger("Move Left");
            ObjectAnimator.speed *= GameManager.Instance.pushSpeedMultiplier;
            if (moveDir == Vector2.up)
            {
                ObjectAnimator.SetTrigger("Move Up");
                yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed / GameManager.Instance.pushSpeedMultiplier);
            }
            else if (moveDir == Vector2.right)
            {
                ObjectAnimator.SetTrigger("Move Right");
                yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed / GameManager.Instance.pushSpeedMultiplier);
            }
            else if (moveDir == Vector2.down)
            {
                ObjectAnimator.SetTrigger("Move Down");
                yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed / GameManager.Instance.pushSpeedMultiplier);
            }
            else if (moveDir == Vector2.left)
            {
                ObjectAnimator.SetTrigger("Move Left");
                yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed / GameManager.Instance.pushSpeedMultiplier);
            }
            ObjectAnimator.speed /= GameManager.Instance.pushSpeedMultiplier;
        }
    }

    public void UndoMove(Vector2 dirMoved)
    {
        StartCoroutine(MoveObjectCoroutine(-dirMoved, false));
    }
    
    public void UndoColor()
    {
        this.colorStack.Pop();
        SetColor(this.colorStack.Peek(), false, true);
    }

    public void ResetObjectLocation()
    {
        transform.position = defaultPos;
    }

    private void FullReset() // When 'R' key pressed. Cannot be undone
    {
        // Reset object position
        ResetObjectLocation();
        
        // Reset object color & add to colorStack
        colorStack.Clear();
        SetColor(defaultColor, true, true);
    }

    private void UpdateAnimationSpeed()
    {
        ObjectAnimator.speed = GameManager.Instance.animationSpeed;
    }
}
