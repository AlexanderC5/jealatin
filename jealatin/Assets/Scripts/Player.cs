using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.Mathematics;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Vector2 = UnityEngine.Vector2;

public class Player : MonoBehaviour
{   
    [SerializeField] private Vector2 defaultPos;
    [SerializeField] private Enums.Action defaultFacing;
    [SerializeField] private Enums.Color defaultColor;

#region Properties
    private Vector2 pos; // Current x,y location of the player on the grid
    public Vector2 Pos { get => pos; set => pos = value; }
    private Enums.Action facing; // N E S W direction of player sprite
    public Enums.Action Facing { get => facing; set => facing = value; }
    private int color; // R Y B color1 of player
    public int Color
    {
        get => color;
        set => color = value;
    }
    private bool isDead;
    public bool IsDead { get => isDead; set => isDead = value; }
#endregion Properties

    private Stack<Enums.Action> actionStack = new Stack<Enums.Action>(); // Stores the player's actions to allow for undoing
    private Stack<Enums.Color> colorStack = new Stack<Enums.Color>(); // Stores the player's color history to allow for undoing
    
    private bool isInputDisabled;
    private Coroutine undoDelayCoroutine;


    private Enums.Color bumpColorSelect; // Which color (R,Y,B) was selected via a keyboard press?
    int transferableColors; // Which colors (R,Y,B) can be transferred between player and object?

    private SpriteRenderer PlayerSprite;

    void Awake()
    {
        PlayerSprite = GetComponentInChildren<SpriteRenderer>();
        if (defaultPos != Vector2.zero) this.transform.position = defaultPos;
        this.transform.position = new Vector3(Mathf.Round(this.transform.position.x), Mathf.Round(this.transform.position.y), 0);
    }
    
    void Start()
    {
         // Sets the default color & updates the player's color properties
        if (defaultColor == Enums.Color.Green)
        {
            Debug.Log("Player cannot start Green-colored. Changing to Blue.");
            defaultColor = Enums.Color.Blue;
        }
        SetColor(defaultColor, true, true);
        
        SetPlayerLocation(this.Pos, defaultFacing); // Update facing-direction of player on Sprite
    }

    void Update()
    {
        if (isInputDisabled) return;

        if (GameManager.Instance.GameMode == Enums.GameMode.Game) // Movement and Undo controls
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) this.Move(Enums.Action.North);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) this.Move(Enums.Action.East);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) this.Move(Enums.Action.South);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) this.Move(Enums.Action.West);

            if (Input.GetKeyDown(KeyCode.Z)) this.Undo();
            else if (Input.GetKey(KeyCode.Z) && undoDelayCoroutine == null) this.Undo(); // For holding z

            if (Input.GetKeyDown(KeyCode.R))
            {
                    this.FullReset();
            }

            // TEMPORARY COLOR-SWITCHING CODE
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                this.SetColor(Enums.Color.Orange, true);
                actionStack.Push(Enums.Action.North);
                actionStack.Push(Enums.Action.Bump);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                this.SetColor(Enums.Color.Violet, true);
                actionStack.Push(Enums.Action.South);
                actionStack.Push(Enums.Action.Bump);
            }
        }

        else if (GameManager.Instance.GameMode == Enums.GameMode.ColorSelect) // Select a color controls
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (((transferableColors & (int) Enums.Color.Red) == 000) || this.Color == (int) Enums.Color.Red)
                {
                    // Play Error sfx - can't select this color!
                }
                else bumpColorSelect = Enums.Color.Red;
            }
            if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (((transferableColors & (int) Enums.Color.Yellow) == 000) || this.Color == (int) Enums.Color.Yellow)
                {
                    // Play Error sfx - can't select this color!
                }
                else bumpColorSelect = Enums.Color.Yellow;
            }
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (((transferableColors & (int) Enums.Color.Blue) == 000) || this.Color == (int) Enums.Color.Blue)
                {
                    // Play Error sfx - can't select this color!
                }
                else bumpColorSelect = Enums.Color.Blue;
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Z)) // Cancel the bump action
            {
                transferableColors = -1;
            }
        }
    }

    private void Move(Enums.Action dir)
    {
        // Set nextPos to the next location the player will move to
        Vector2 nextPos = Vector2.zero;
        switch (dir)
        {
            case Enums.Action.North:
                nextPos = Vector2.up;
                break;
            case Enums.Action.East:
                nextPos = Vector2.right;
                break;
            case Enums.Action.South:
                nextPos = Vector2.down;
                break;
            case Enums.Action.West:
                nextPos = Vector2.left;
                break;
        }

        // Check collision; do not move if collider detected
        GameObject bumpedObject = null; // Store any object that is bumped for a future bump check
        Enums.Touch moveCollision = CheckCollision(nextPos, ref bumpedObject);

        if (moveCollision == Enums.Touch.Collider) return; // Found a collider, do not move

        if (moveCollision == Enums.Touch.ColorChange) // Found a bumpable object, bump
        {
            StartCoroutine(BumpAction(dir, bumpedObject.GetComponent<Object>()));
            return;
        }

        // TODO: Animation for 'Enums.Touch.Consume'

        if (moveCollision == Enums.Touch.Pushable) // Found a pushable object, push
        {
            StartCoroutine(MoveAction(dir, nextPos, bumpedObject));
            return;
        }

        // TODO: Animation for 'Enums.Touch.Pushable'

        StartCoroutine(MoveAction(dir, nextPos));
    }

    public Enums.Touch CheckCollision(Vector2 dir, ref GameObject bumpedObject)
    {
        RaycastHit2D hit1 = Physics2D.Raycast(this.transform.position + (Vector3) dir, dir, 0.1f); // Send out a raycast to check ONE tile ahead of this sprite
        RaycastHit2D hit2 = Physics2D.Raycast(this.transform.position + (Vector3) dir * 2, dir, 0.1f); // Send out a raycast to check TWO tiles ahead of this sprite

        if (hit1.collider == null) return Enums.Touch.None;

        bumpedObject = hit1.transform.gameObject; // Store the bumped object

        if (bumpedObject.gameObject.tag == "Object") // If the 1-tile raycast finds an object with the "Object" tag
        {
            if (hit2.collider == null) // If there's a collider in the tile behind the bumped object
            {
                return Enums.Touch.Pushable;
            }
            if (this.Color == bumpedObject.GetComponent<Object>().Color) // If the bumped object is the same color as the player
            {
                return Enums.Touch.Consume;
            }
            return Enums.Touch.ColorChange;
        }
        else
        {
            return Enums.Touch.Collider; // No bumped objects found -> Just a normal collision
        }
    }

    private IEnumerator MoveAction(Enums.Action moveDir, Vector2 nextPos, GameObject pushableObject = null)
    {
        isInputDisabled = true;

        actionStack.Push(moveDir);

        //this.transform.position += new Vector3(nextPos.x, nextPos.y);
        SetPlayerLocation((Vector2) this.transform.position + nextPos, moveDir);
        if (pushableObject != null)
        {
            pushableObject.GetComponent<Object>().MoveObject(moveDir);
            actionStack.Push(Enums.Action.Push);
        }

        // TODO: Aniamte
        
        Debug.Log(actionStack.Count + " actions taken");

        yield return new WaitForSeconds(0.2f * GameManager.Instance.animationSpeed);
        isInputDisabled = false;
    }

    private IEnumerator BumpAction(Enums.Action moveDir, Object bumpedObject)
    {
        transferableColors = (int) (this.Color ^ bumpedObject.Color); // Which colors can be transferred between player & object?
        if (transferableColors == 000) // No colors are eligible for a transfer
        {
            yield break;
        }
        
        isInputDisabled = true;
        GameManager.Instance.GameMode = Enums.GameMode.ColorSelect; // Keyboard controls selects color instead of moving

        // TODO: Animate the UI displaying

        yield return new WaitForSeconds(0.2f * GameManager.Instance.animationSpeed);
        
        isInputDisabled = false;

        Debug.Log("Waiting for player to choose a transferable color...");

        yield return new WaitUntil(() => ((transferableColors & (int) bumpColorSelect) != 000) || transferableColors == -1); // Wait for the player to make a selection
        
        if (transferableColors != -1) // If not cancelled:
        {
            actionStack.Push(moveDir); // Add both the moveDir and a 'bump' action
            actionStack.Push(Enums.Action.Bump);
            this.SetColor((Enums.Color) this.Color ^ bumpColorSelect, true, true);
            bumpedObject.SetColor((Enums.Color) bumpedObject.Color ^ bumpColorSelect, true, true);
        }
        
        // TODO: Animate the UI hiding

        isInputDisabled = true;

        yield return new WaitForSeconds(0.2f * GameManager.Instance.animationSpeed);

        // TODO: Animate the slime consuming the object and changing colors

        bumpColorSelect = Enums.Color.None;
        transferableColors = 0;
        GameManager.Instance.GameMode = Enums.GameMode.Game;
        isInputDisabled = false;
    }
    
    private void Undo()
    {
        if (actionStack.Count == 0) return; // Make sure the undo-stack is not empty

        Enums.Action lastAction = actionStack.Pop();
        Debug.Log("Undo #" + actionStack.Count + ": " + lastAction);

        bool isLastActionBump = false;
        bool isLastActionPush = false;
        if (lastAction == Enums.Action.Bump) // Check for a bump or push
        {
            isLastActionBump = true;
            lastAction = actionStack.Pop();
            Debug.Log("Undo #" + actionStack.Count + ": " + lastAction);
        }
        else if (lastAction == Enums.Action.Push) // Check for a bump or push
        {
            isLastActionPush = true;
            lastAction = actionStack.Pop();
            Debug.Log("Undo #" + actionStack.Count + ": " + lastAction);
        }
        
        Enums.Action lastFacingDir = Enums.Action.None; // Check 2+ moves ago for the player direction
        Stack<Enums.Action> tempActionStorage = new Stack<Enums.Action>(); // Create temporary stack to store non-move actions
        if (actionStack.Count > 0) lastFacingDir = actionStack.Peek();
        while (actionStack.Count > 0 && (lastFacingDir == Enums.Action.Bump || lastFacingDir == Enums.Action.None))
        {
            tempActionStorage.Push(actionStack.Pop());
            lastFacingDir = actionStack.Peek();
        }
        while (tempActionStorage.Count > 0) actionStack.Push(tempActionStorage.Pop());

        // What direction did the player move last?
        Vector2 dirMoved = Vector2.zero;
        switch (lastAction)
        {
            case Enums.Action.North:
                dirMoved = Vector2.up;
                break;
            case Enums.Action.East:
                dirMoved = Vector2.right;
                break;
            case Enums.Action.South:
                dirMoved = Vector2.down;
                break;
            case Enums.Action.West:
                dirMoved = Vector2.left;
                break;
        }

        // Find the object being targetted by the bump or push
        RaycastHit2D hit = Physics2D.Raycast(this.transform.position + (Vector3) dirMoved, dirMoved, 0.1f); // Send out a raycast to check ONE tile ahead of this sprite
        
        // If bump, undo the target object's color stack too
        if (isLastActionBump)
        {
            if (colorStack.Count <= 1) Debug.Log("No colors in the color stack to pop!");
            colorStack.Pop();
            this.SetColor(colorStack.Peek(), false, true);

            hit.transform.gameObject.GetComponent<Object>().UndoColor();
        }
        else // If not a bump, undo the move (and push?) action
        {            
            if (isLastActionPush) // If push, undo the target object's move stack too
            {
                hit.transform.gameObject.GetComponent<Object>().UndoMove(dirMoved);
            }

            this.Pos = this.Pos - dirMoved; // Undo Player move action
        }

        this.Facing = lastFacingDir; // Update the player's facing from the action taken two-turns ago
        SetPlayerLocation();

        // Start Undo-Cooldown Coroutine for holding z
        if (undoDelayCoroutine != null) StopCoroutine("UndoDelay"); // Refresh the undo-cooldown timer
        undoDelayCoroutine = StartCoroutine("UndoDelay");
    }

    IEnumerator UndoDelay()
    {
        yield return new WaitForSeconds(0.2f * GameManager.Instance.animationSpeed);
        undoDelayCoroutine = null;
    }

    public void SetColor(Enums.Color color, bool addToColorStack, bool updateColor = false)
    {
        // if (updateColor)
        // {
        //     Color1 = Enums.Color.None;
        //     Color2 = Enums.Color.None;

        //     if (color == Enums.Color.Violet)
        //     {
        //         Color1 = Enums.Color.Red;
        //         Color2 = Enums.Color.Blue;
        //     }
        //     else if (color == Enums.Color.Orange)
        //     {
        //         Color1 = Enums.Color.Red;
        //         Color2 = Enums.Color.Yellow;
        //     }
        //     else if (color == Enums.Color.Green)
        //     {
        //         Color1 = Enums.Color.Yellow;
        //         Color2 = Enums.Color.Blue;
        //     }
        //     else Color1 = color;
        // }

        if (updateColor) { Color = (int) color; }
        
        if (addToColorStack) { colorStack.Push(color); }

        PlayerSprite.color = GameManager.Instance.Palette[color];
    }

    public void SetPlayerLocation(Vector2 position, Enums.Action facing = Enums.Action.None)
    {
        this.Pos = position;
        if (facing != Enums.Action.None) this.Facing = facing;
        SetPlayerLocation();
    }
    public void SetPlayerLocation()
    {
        transform.position = this.Pos;
        // TODO: Update facing direction
    }

    public void FullReset() // When 'R' key pressed. Cannot be undone
    {
        // Reset player position & facing
        actionStack.Clear();
        SetPlayerLocation(defaultPos, defaultFacing);
        
        // Reset player color & add to colorStack
        colorStack.Clear();
        SetColor(defaultColor, true, true);
    }
}
