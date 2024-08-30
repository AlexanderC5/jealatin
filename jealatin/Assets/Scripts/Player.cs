using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{    
    private Vector2 pos; // Current x,y location of the player on the grid
    public Vector2 Pos { get => pos; set => pos = value; }
    private Enums.Dir facing; // N E S W direction of player sprite
    public Enums.Dir Facing { get => facing; set => facing = value; }
    private Enums.Color color1; // R Y B color1 of player
    public Enums.Color Color1 { get => color1; set => color1 = value; }
    private Enums.Color color2; // R Y B color2 of player
    public Enums.Color Color2 { get => color2; set => color2 = value; }
    private Stack<Enums.Dir> moveStack; // Stores the player's moves to allow for undoing
    private bool isDead;
    public bool IsDead { get => isDead; set => isDead = value; }

    void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
