using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

public class Player : MonoBehaviour
{    
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
    private Enums.Color bumpColorSelect;

    private SpriteRenderer PlayerSprite;

    [SerializeField] private float animationSpeed = 1f;

    void Awake() {
        PlayerSprite = GetComponentInChildren<SpriteRenderer>();
        this.transform.position = new Vector3(Mathf.Round(this.transform.position.x), Mathf.Round(this.transform.position.y), 0);
    }
    

    void Update()
    {
        if (GameManager.Instance.GameMode == Enums.GameMode.Game) // Movement and Undo controls
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) this.Move(Enums.Action.North);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) this.Move(Enums.Action.East);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) this.Move(Enums.Action.South);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) this.Move(Enums.Action.West);

            if (Input.GetKey(KeyCode.Z)) this.Undo();
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

        return Enums.Touch.Collider; // No bumped objects found -> Just a normal collision
    }

    private IEnumerator MoveAction(Enums.Action moveDir, Vector2 nextPos)
    {
        isMoving = true;

        this.transform.position += new Vector3(nextPos.x, nextPos.y);

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

    }

    private void AbsorbColor(Enums.Color color)
    {
        // Update the color of the Player
        
        colorStack.Push(color);
    }

    public void SetColor(Enums.Color color, bool addToColorStack)
    {
        PlayerSprite.color = GameManager.Instance.Palette[color];
        if (addToColorStack)
        {
            colorStack.Push(color);
        }
    }

    public void ResetActionStack()
    {
        actionStack.Clear();
    }
    public void ResetColorStack()
    {
        colorStack.Clear();
    }
}
