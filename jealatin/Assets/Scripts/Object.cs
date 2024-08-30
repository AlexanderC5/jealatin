using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Object : MonoBehaviour 
{
    SpriteRenderer ObjectSprite;
    private Stack<Enums.Color> colorStack; // Stores the player's moves to allow for undoing

    void Awake() {
        ObjectSprite = GetComponent<SpriteRenderer>();
    }

    // Edit SpriteRenderer Color property
    void UpdateColor() {
        // Use colors stored in data structure
        // ObjectSprite.color = COLOR;
    }
}
