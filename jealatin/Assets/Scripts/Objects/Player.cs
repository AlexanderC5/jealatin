using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{   
    [SerializeField] private Vector2 defaultPos;
    [SerializeField] private Enums.Action defaultFacing = Enums.Action.South;
    [SerializeField] private Enums.Color defaultColor;

    [SerializeField] private bool isDeadIfGreen = true;

    [SerializeField] private Sprite[] colorPickerSprite = new Sprite[2];
    [SerializeField] private Sprite[] expressionsSprites = new Sprite[15];

#region Properties
    private Vector2 pos; // Current x,y location of the player on the grid
    public Vector2 Pos { get => pos; set => pos = value; }
    private Enums.Action facing; // N E S W direction of player sprite
    public Enums.Action Facing { get => facing; set => facing = value; }
    private Enums.Color color; // R Y B color1 of player
    public Enums.Color Color
    {
        get => color;
        set => color = value;
    }
    private bool isDead;
    public bool IsDead { get => isDead; set => isDead = value; }
    private bool isWin;
    public bool IsWin { get => isDead; set => isDead = value; }
#endregion Properties

    private Stack<Enums.Action> actionStack = new Stack<Enums.Action>(); // Stores the player's actions to allow for undoing
    private Stack<Enums.Color> colorStack = new Stack<Enums.Color>(); // Stores the player's color history to allow for undoing
    
    private Coroutine undoDelayCoroutine;


    private Enums.Color bumpColorSelect; // Which color (R,Y,B) was selected via a keyboard press?
    Enums.Color transferableColors; // Which colors (R,Y,B) can be transferred between player and object?

    private SpriteRenderer[] PlayerSprite = new SpriteRenderer[5]; // 0 for slime body, 1 for expressions, 2-4 for color picker
    private Animator PlayerAnimator;

    void Awake()
    {
        PlayerSprite = GetComponentsInChildren<SpriteRenderer>();
        PlayerAnimator = GetComponentInChildren<Animator>();
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
        SetColor(defaultColor, true);
        
        this.FullReset();
        IsDead = false;
        IsWin = false;

        UpdateAnimationSpeed();
        UpdateExpression();
    }

    void Update()
    {
        if (IsWin) return; // Without this, LoadNextScene() will be called every frame
        if (isDeadIfGreen && !IsDead && this.Color == Enums.Color.Green) // Play the death animation if player touches green
        {
            isDead = true;

            DeathAnimation();
        }

        // Player wins if they are out of bounds
        if ((this.Pos.y > GameManager.Instance.StageSize.yMax)
         || (this.Pos.x > GameManager.Instance.StageSize.xMax)
         || (this.Pos.y < GameManager.Instance.StageSize.yMin)
         || (this.Pos.x < GameManager.Instance.StageSize.xMin))
        {
            StopAllCoroutines();
            IsWin = true;
            GameManager.Instance.GameMode = Enums.GameMode.LevelClear;
            Debug.Log("Level cleared!");
            GameManager.Instance.LoadNextScene();
        }

        if (GameManager.Instance.GameMode == Enums.GameMode.NoInteraction) return;

        if (GameManager.Instance.GameMode == Enums.GameMode.Game) // Movement and Undo controls
        {
            if (!isDead) // Can only move if the player is not dead
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) this.Move(Enums.Action.North);
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) this.Move(Enums.Action.East);
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) this.Move(Enums.Action.South);
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) this.Move(Enums.Action.West);
            }
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Backspace)) this.Undo();
            else if (undoDelayCoroutine == null && (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.Backspace))) this.Undo(); // For holding z

            if (Input.GetKeyDown(KeyCode.R))
            {
                FullReset();
            }
        }

        else if (GameManager.Instance.GameMode == Enums.GameMode.ColorSelect) // Select a color controls
        {
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (((transferableColors & Enums.Color.Red) == Enums.Color.None) || this.Color == Enums.Color.Red)
                {
                    // Play Error sfx - can't select this color!
                }
                else bumpColorSelect = Enums.Color.Red;
            }
            if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (((transferableColors & Enums.Color.Yellow) == Enums.Color.None) || this.Color == Enums.Color.Yellow)
                {
                    // Play Error sfx - can't select this color!
                }
                else bumpColorSelect = Enums.Color.Yellow;
            }
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (((transferableColors & Enums.Color.Blue) == Enums.Color.None) || this.Color == Enums.Color.Blue)
                {
                    // Play Error sfx - can't select this color!
                }
                else bumpColorSelect = Enums.Color.Blue;
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) // Cancel the bump action
            {
                transferableColors = (Enums.Color) (-1);
            }
        }
    }

    private void Move(Enums.Action dir)
    {
        UpdateAnimationSpeed();
        PlayerAnimator.ResetTrigger("Up");
        PlayerAnimator.ResetTrigger("Right");
        PlayerAnimator.ResetTrigger("Down");
        PlayerAnimator.ResetTrigger("Left");

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

        if (moveCollision == Enums.Touch.Pushable) // Found a pushable object, push
        {
            StartCoroutine(MoveAction(dir, nextPos, bumpedObject));
            return;
        }

        StartCoroutine(MoveAction(dir, nextPos));
    }

    public Enums.Touch CheckCollision(Vector2 dir, ref GameObject bumpedObject)
    {
        //RaycastHit2D hit1 = Physics2D.Raycast(this.transform.position + (Vector3) dir, dir, 0.1f); // Send out a raycast to check ONE tile ahead of this sprite
        RaycastHit2D hit2 = Physics2D.Raycast(this.transform.position + (Vector3) dir * 2, dir, 0.1f); // Send out a raycast to check TWO tiles ahead of this sprite

        Collider2D hit1 = Physics2D.OverlapPoint((Vector2) this.transform.position + dir);
        //TilemapCollider2D hit1t = 

        //if (hit1.collider == null) { Debug.Log("No collider detected"); return Enums.Touch.None; }
        if (hit1 == null) return Enums.Touch.None;

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
        GameManager.Instance.GameMode = Enums.GameMode.NoInteraction;

        actionStack.Push(moveDir);

        SetPlayerLocation((Vector2) this.transform.position + nextPos, moveDir);
        UpdateExpression();
        
        PlayerAnimator.SetInteger("AnimationType", 1); // 1 = movement-type animation
        PlayerAnimator.SetTrigger(AnimTriggerName(moveDir));

        if (pushableObject != null)
        {
            pushableObject.GetComponent<Object>().MoveObject(moveDir);
            actionStack.Push(Enums.Action.Push);

            yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed / GameManager.Instance.pushSpeedMultiplier);
        }
        else // No pushable object
        {
            yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed);
        }
        PlayerAnimator.SetInteger("AnimationType", 0); // 0 = idle-type animation
        
        GameManager.Instance.GameMode = Enums.GameMode.Game;
    }

    private IEnumerator BumpAction(Enums.Action moveDir, Object bumpedObject)
    {
        transferableColors = this.Color ^ bumpedObject.Color; // Which colors can be transferred between player & object?
        if (transferableColors == Enums.Color.None) // No colors are eligible for a transfer
        {
            yield break;
        }
        if (bumpedObject.Color == Enums.Color.None) // Only the player can transfer colors
        {
            if (this.Color == Enums.Color.Red || this.Color == Enums.Color.Yellow || this.Color == Enums.Color.Blue) yield break;
        }
        
        GameManager.Instance.GameMode = Enums.GameMode.NoInteraction;

        this.Facing = moveDir;
        UpdateExpression();

        // Hide the Sprites for invalid-colors
        PlayerSprite[2].sprite = colorPickerSprite[0];
        PlayerSprite[3].sprite = colorPickerSprite[0];
        PlayerSprite[4].sprite = colorPickerSprite[0];
        if (((transferableColors & Enums.Color.Red) != Enums.Color.None) && this.Color != Enums.Color.Red) PlayerSprite[2].sprite = colorPickerSprite[1];
        if (((transferableColors & Enums.Color.Yellow) != Enums.Color.None) && this.Color != Enums.Color.Yellow) PlayerSprite[3].sprite = colorPickerSprite[1];
        if (((transferableColors & Enums.Color.Blue) != Enums.Color.None) && this.Color != Enums.Color.Blue) PlayerSprite[4].sprite = colorPickerSprite[1];

        // Animate the showing of the color-picking UI
        PlayerAnimator.SetInteger("AnimationType", 3);
        PlayerAnimator.SetTrigger("Down");
        yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed);
        PlayerAnimator.ResetTrigger("Down");
        
        GameManager.Instance.GameMode = Enums.GameMode.ColorSelect; // Keyboard controls selects color instead of moving

        Debug.Log("Waiting for player to choose a transferable color...");

        yield return new WaitUntil(() => ((transferableColors & bumpColorSelect) != Enums.Color.None) || transferableColors == (Enums.Color) (-1)); // Wait for the player to make a selection
        
        GameManager.Instance.GameMode = Enums.GameMode.NoInteraction;

        // Animate the hiding of the color-picking UI
        PlayerAnimator.SetInteger("AnimationType", 4);
        PlayerAnimator.SetTrigger("Down");
        yield return new WaitForSeconds(0.1f / GameManager.Instance.animationSpeed);
        PlayerAnimator.ResetTrigger("Down");

        if (transferableColors != (Enums.Color) (-1)) // If color-transfer was not cancelled:
        {
            actionStack.Push(moveDir); // Add both the moveDir and a 'bump' action
            actionStack.Push(Enums.Action.Bump);

            // bumpedObject.SetColor((Enums.Color) bumpedObject.Color ^ bumpColorSelect, true, true);

            PlayerAnimator.SetInteger("AnimationType", 2); // 2 = bump-type animation
            PlayerAnimator.SetTrigger(AnimTriggerName(moveDir));

            Enums.Color oldPlayerColor = this.Color; // Store the color of the player pre-transfer
            SetColor( this.Color ^ bumpColorSelect, true, false); // Only update the player's color in the code; the visuals are handled by the lerp

            Enums.Color oldBumpedColor = bumpedObject.Color; 
            bumpedObject.SetColor(bumpedObject.Color ^ bumpColorSelect, true, false);

            Enums.Color oldConsumedColor = (Enums.Color) (-1);
            RaycastHit2D hit0 = Physics2D.Raycast(this.transform.position, Vector2.up, 0.1f); // Send out a raycast to check the current tile
            if (hit0.collider != null) // If the player is currently on top of another object (the object has the same color)
            {
                oldConsumedColor = hit0.transform.gameObject.GetComponent<Object>().Color;
                hit0.transform.gameObject.GetComponent<Object>().SetColor( this.Color, true, false);
            }

            for (float f = 0.05f; f <= 1f; f += 0.05f) // Lerp all relevant colors
            {
                yield return new WaitForSeconds(0.2f / 20 / GameManager.Instance.animationSpeed / GameManager.Instance.bumpSpeedMultiplier);
                this.SetLerpColor(oldPlayerColor, this.Color, f);
                bumpedObject.SetLerpColor(oldBumpedColor, bumpedObject.Color, f);
                if ((int)oldConsumedColor != -1)
                {
                    hit0.transform.gameObject.GetComponent<Object>().SetLerpColor(oldConsumedColor, this.Color, f);
                }
                if (f > 0.5f) UpdateExpression(); // Update expression halfway through bump
            }
            PlayerAnimator.SetInteger("AnimationType", 0); // = idle-type animation
        
        }
        else // Add some delay when cancelling so the player doesn't immediately move
        {
            yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed);
        }

        bumpColorSelect = Enums.Color.None;
        transferableColors = 0;
        GameManager.Instance.GameMode = Enums.GameMode.Game;
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
        }
        else if (lastAction == Enums.Action.Push) // Check for a bump or push
        {
            isLastActionPush = true;
            lastAction = actionStack.Pop();
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
        
        // If bump, undo the target object's color stack too. Check for any object on top of the player.
        if (isLastActionBump)
        {
            if (colorStack.Count <= 1) Debug.Log("No colors in the color stack to pop!");
            colorStack.Pop();
            this.SetColor(colorStack.Peek(), false);

            hit.transform.gameObject.GetComponent<Object>().UndoColor();
            
            RaycastHit2D hit0 = Physics2D.Raycast(this.transform.position, dirMoved, 0.1f); // Send out a raycast to check the current tile
            if (hit0.collider != null) // If the player is currently on top of another object (the object has the same color)
            {
                hit0.transform.gameObject.GetComponent<Object>().UndoColor();
            }
        }
        else // If not a bump, undo the move (and push?) action
        {            
            if (isLastActionPush) // If push, undo the target object's move stack too
            {
                hit.transform.gameObject.GetComponent<Object>().UndoMove(dirMoved);
            }

            this.Pos -= dirMoved; // Undo Player move action
        }

        SetPlayerLocation(this.Pos, lastFacingDir); // Update the player's location & facing from the action taken two-turns ago
        PlayerAnimator.SetInteger("AnimationType", 0); // 0 = idle-type animation
        isDead = false; // Player is no longer deada if the undo removes the death

        // Start Undo-Cooldown Coroutine for holding z
        if (undoDelayCoroutine != null) StopCoroutine("UndoDelay"); // Refresh the undo-cooldown timer
        undoDelayCoroutine = StartCoroutine("UndoDelay");
    }

    IEnumerator UndoDelay()
    {
        yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed);
        undoDelayCoroutine = null;
    }

    public void SetColor(Enums.Color color, bool addToColorStack, bool updateColor = true)
    {   
        Color = color; // Set this to false if lerping with the function directly below

        if (addToColorStack) { colorStack.Push(color); }

        if (updateColor)
        {
            PlayerSprite[0].color = GameManager.Instance.Palette[color];
            UpdateExpression();
        }
    }

    public void SetLerpColor(Enums.Color color1, Enums.Color color2, float percent)
    {
        PlayerSprite[0].color = UnityEngine.Color.Lerp(GameManager.Instance.Palette[color1], GameManager.Instance.Palette[color2], percent);
    }

    public void SetPlayerLocation(Vector2 position, Enums.Action facing = Enums.Action.None)
    {
        this.Pos = position;
        transform.position = this.Pos;

        if (facing != Enums.Action.None) this.Facing = facing;
        
        PlayerAnimator.ResetTrigger("Up");
        PlayerAnimator.ResetTrigger("Right");
        PlayerAnimator.ResetTrigger("Down");
        PlayerAnimator.ResetTrigger("Left");

        PlayerAnimator.SetInteger("AnimationType", 0); // 0 = idle animation
        switch (facing)
        {
            case Enums.Action.North:
                PlayerAnimator.SetTrigger("Up");
                break;
            case Enums.Action.East:
                PlayerAnimator.SetTrigger("Right");
                break;
            case Enums.Action.South:
            default:
                PlayerAnimator.SetTrigger("Down");
                break;
            case Enums.Action.West:
                PlayerAnimator.SetTrigger("Left");
                break;
        }
    }

    public void FullReset() // When 'R' key pressed. Cannot be undone
    {
        // Reset player position & facing
        actionStack.Clear();
        PlayerAnimator.ResetTrigger("Up");
        PlayerAnimator.ResetTrigger("Right");
        PlayerAnimator.ResetTrigger("Down");
        PlayerAnimator.ResetTrigger("Left");
        PlayerAnimator.SetInteger("AnimationType", 0);
        SetPlayerLocation(defaultPos, defaultFacing);
        
        // Reset player color & add to colorStack
        colorStack.Clear();
        SetColor(defaultColor, true);

        UpdateExpression();
        isDead = false;
        GameManager.Instance.GameMode = Enums.GameMode.Game;
    }

    private string AnimTriggerName(Enums.Action dir)
    {
        switch (dir)
        {
            case Enums.Action.North:
                return "Up";
            case Enums.Action.East:
                return "Right";
            case Enums.Action.South:
                return "Down";
            case Enums.Action.West:
                return "Left";
            default:
                return "";
        }
    }
    private void UpdateAnimationSpeed()
    {
        PlayerAnimator.speed = GameManager.Instance.animationSpeed;
    }

    private void UpdateExpression()
    {
        int spriteIndex = 0; // 0 = No sprite
        PlayerSprite[1].flipX = false;

        if (this.Facing != Enums.Action.North) // If facing north, leave index at 0
        {
            spriteIndex = (int) this.Color; // Sets sprite to 1-7 (South-facing)

            if (this.Facing != Enums.Action.South) // Check if facing to the side
            {
                if (facing == Enums.Action.West) PlayerSprite[1].flipX = true; // If facing Left, flip the sprite
                spriteIndex += 7;
            }
        }
        
        PlayerSprite[1].sprite = expressionsSprites[spriteIndex];
    }

    private void DeathAnimation()
    {
        StartCoroutine(DeathAnimationCoroutine());
    }

    IEnumerator DeathAnimationCoroutine()
    {
        yield return new WaitUntil(() => PlayerAnimator.GetInteger("AnimationType") == 0); // Only play the animation from the idle state (to not mess up bool vars)

        PlayerAnimator.SetInteger("AnimationType", 5); // 5 = death animation
        PlayerAnimator.SetTrigger("Down"); // trigger the start of the animation
        yield return new WaitForSeconds(0.2f / GameManager.Instance.animationSpeed); // boom
        PlayerAnimator.ResetTrigger("Down");

        Debug.Log("Player is dead - Press z to Undo or r to Restart!");
        
        // TODO: Call UI -> Tell player to Undo
    }
}
