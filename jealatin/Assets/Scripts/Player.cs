using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Vector2 = UnityEngine.Vector2;

public class Player : MonoBehaviour
{   
    [SerializeField] private Vector2 defaultPos;
    [SerializeField] private Enums.Action defaultFacing;
    [SerializeField] private Enums.Color defaultColor;

    private Vector2 pos; // Current x,y location of the player on the grid
    public Vector2 Pos { get => pos; set => pos = value; }
    private Enums.Action facing; // N E S W direction of player sprite
    public Enums.Action Facing { get => facing; set => facing = value; }
    private Enums.Color color1; // R Y B color1 of player
    public Enums.Color Color1
    {
        get => color1;
        set
        {
            if (color2 == Enums.Color.None && value == Enums.Color.None) return; // Can't remove both colors
            if (color2 == value) return; // Can't set both colors to the save value
        }
    }
    private Enums.Color color2; // R Y B color2 of player
    public Enums.Color Color2
    {
        get => color2;
        set
        {
            if (color1 == Enums.Color.None && value == Enums.Color.None) return; // Can't remove both colors
            if (color1 == value) return; // Can't set both colors to the save value
        }
    }
    private int colorCount;
    public int ColorCount { get => colorCount; private set => colorCount = value; }
    private Stack<Enums.Action> actionStack = new Stack<Enums.Action>(); // Stores the player's actions to allow for undoing
    private Stack<Enums.Color> colorStack = new Stack<Enums.Color>(); // Stores the player's color history to allow for undoing
    private bool isDead;
    public bool IsDead { get => isDead; set => isDead = value; }
    private bool isMoving;
    private Coroutine undoDelayCoroutine;
    private Enums.Color bumpColorSelect;

    private SpriteRenderer PlayerSprite;

    [SerializeField] private float animationSpeed = 1f;

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

        //string a = Enums.RED;

        if (GameManager.Instance.GameMode == Enums.GameMode.ColorSelect) // Select a color controls
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha1)) bumpColorSelect = Enums.Color.Red;
            if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Alpha2)) bumpColorSelect = Enums.Color.Green;
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha3)) bumpColorSelect = Enums.Color.Blue;
            
            if (Input.GetKeyDown(KeyCode.Escape)) // Cancel the bump action
            {
                bumpColorSelect = Enums.Color.Green; // Green isn't a choice - this is just some other random color to signal that the bump was cancelled
            }
        }
    }

    private void Move(Enums.Action dir)
    {
        if (isMoving) return;

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
        
        Enums.Touch moveCollision = CheckCollision(nextPos, bumpedObject);

        if (moveCollision == Enums.Touch.Collider) return; // Found a collider, do not move

        if (moveCollision == Enums.Touch.Object) { StartCoroutine(BumpAction(dir, bumpedObject)); } // Found an object, bump

        StartCoroutine(MoveAction(dir, nextPos));
    }

    public Enums.Touch CheckCollision(Vector2 dir, GameObject bumpedObject)
    {
        // Send out a raycast from the center of this sprite, 1.1f units in the direction of dir
        RaycastHit2D[] hit = Physics2D.RaycastAll(this.transform.position, dir, 1.1f);

        if (hit.Length == 0) return Enums.Touch.None;

        foreach (var h in hit) // Loop through each collider hit by the raycast
        {
            if (h.transform.gameObject.tag == "Object") // If it finds an object with the "Object" tag
            {
                bumpedObject = transform.gameObject; // Store the bumped object
                return Enums.Touch.Object;
            }
        }

        Debug.Log("Collision with: " + hit[0].transform.name);
        return Enums.Touch.Collider; // No bumped objects found -> Just a normal collision
    }

    private IEnumerator MoveAction(Enums.Action moveDir, Vector2 nextPos)
    {
        isMoving = true;

        //this.transform.position += new Vector3(nextPos.x, nextPos.y);
        SetPlayerLocation((Vector2) this.transform.position + nextPos, moveDir);

        // TODO: Aniamte
        
        actionStack.Push(moveDir);
        Debug.Log(actionStack.Count + " actions taken");

        yield return new WaitForSeconds(0.2f * animationSpeed);
        isMoving = false;
    }

    private IEnumerator BumpAction(Enums.Action moveDir, GameObject bumpedObject)
    {
        isMoving = true;

        // TODO: Bumped object management
        //  bumpedObject.GetComponent<Object>().GetColor();
        
        // compare color - is a bump possible? If yes, bring up UI showing your options for the bump. Switch game mode.
        //  else return, set isMoving to false

        // TODO: Animate the UI displaying

        yield return new WaitForSeconds(0.2f * animationSpeed);
        
        yield return new WaitUntil(() => bumpColorSelect != Enums.Color.None); // Wait for the player to make a selection

        if (bumpColorSelect != Enums.Color.Green) // If not cancelled:
        {
            actionStack.Push(moveDir); // Add both the moveDir and a 'bump' action
            actionStack.Push(Enums.Action.Bump);
            this.AbsorbColor(bumpColorSelect);
        }
        
        // TODO: Animate the UI hiding

        yield return new WaitForSeconds(0.2f * animationSpeed);

        bumpColorSelect = Enums.Color.None;
        isMoving = false;
    }
    
    private void Undo()
    {
        if (actionStack.Count == 0) return; // Make sure the undo-stack is not empty

        bool wasLastActionBump = false;

        Enums.Action lastAction = actionStack.Pop();
        Debug.Log("Undo #" + actionStack.Count + ": " + lastAction);

        if (lastAction == Enums.Action.Bump) // Check for a bump
        {
            wasLastActionBump = true;
            lastAction = actionStack.Pop();
            Debug.Log("Undo #" + actionStack.Count + ": " + lastAction);
        }
        
        Enums.Action lastLastAction = Enums.Action.None; // Check two moves ago for the player direction
        Stack<Enums.Action> tempActionStorage = new Stack<Enums.Action>(); // Create temporary stack to store non-move actions
        if (actionStack.Count > 0) lastLastAction = actionStack.Peek();
        while (actionStack.Count > 0 && (lastLastAction == Enums.Action.Bump || lastLastAction == Enums.Action.None))
        {
            tempActionStorage.Push(actionStack.Pop());
            lastLastAction = actionStack.Peek();
        }
        while (tempActionStorage.Count > 0) actionStack.Push(tempActionStorage.Pop());

        if (wasLastActionBump)
        {
            if (colorStack.Count <= 1) Debug.Log("No colors in the color stack to pop!");
            // Call the object and undo oncee in the color stack
            colorStack.Pop();
            this.SetColor(colorStack.Peek(), false);
        }
        else // If not a bump, undo the move action
        {
            // Undo move action
            Vector2 posToMove = Vector2.zero;
            switch (lastAction)
            {
                case Enums.Action.North:
                    posToMove = Vector2.down;
                    break;
                case Enums.Action.East:
                    posToMove = Vector2.left;
                    break;
                case Enums.Action.South:
                    posToMove = Vector2.up;
                    break;
                case Enums.Action.West:
                    posToMove = Vector2.right;
                    break;
            }
            this.Pos = this.Pos + posToMove;
        }

        this.Facing = lastLastAction; // Update the player's facing from the action taken two-turns ago
        SetPlayerLocation();

        // Start Undo-Cooldown Coroutine for holding z
        if (undoDelayCoroutine != null) StopCoroutine("UndoDelay"); // Refresh the undo-cooldown timer
        undoDelayCoroutine = StartCoroutine("UndoDelay");
    }

    IEnumerator UndoDelay()
    {
        yield return new WaitForSeconds(0.2f * animationSpeed);
        undoDelayCoroutine = null;
    }

    private void AbsorbColor(Enums.Color color)
    {
        // Update the color of the Player

        colorStack.Push(color);
    }

    public void SetColor(Enums.Color color, bool addToColorStack, bool updateColor = false)
    {
        if (updateColor)
        {
            Color1 = Enums.Color.None;
            Color2 = Enums.Color.None;

            if (color == Enums.Color.Violet)
            {
                Color1 = Enums.Color.Red;
                Color2 = Enums.Color.Blue;
            }
            else if (color == Enums.Color.Orange)
            {
                Color1 = Enums.Color.Red;
                Color2 = Enums.Color.Yellow;
            }
            else if (color == Enums.Color.Green)
            {
                Color1 = Enums.Color.Yellow;
                Color2 = Enums.Color.Blue;
            }
            else Color1 = color;
        }

        PlayerSprite.color = GameManager.Instance.Palette[color];
        if (addToColorStack)
        {
            colorStack.Push(color);
        }
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

    public void ResetActionStack()
    {
        actionStack.Clear();
    }
    public void ResetColorStack()
    {
        colorStack.Clear();
    }

    private void FullReset() // When 'R' key pressed. Cannot be undone
    {
        // Reset player position & facing
        ResetActionStack();
        SetPlayerLocation(defaultPos, defaultFacing);
        
        // Reset player color & add to colorStack
        ResetColorStack();
        SetColor(defaultColor, true, true);
    }
}
