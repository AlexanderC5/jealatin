using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    private Player player;
    [SerializeField] private Enums.Color playerColor;
    [SerializeField] private Vector2 MapSize; // Automatically resize the camera
    [SerializeField] private Vector3 CameraOffset; // Manual camera offset in inspector (probably unnecessary)

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    void Start()
    {
        SetPlayerColor();
    }

    private void SetPlayerColor()
    {
        if (playerColor == Enums.Color.Green)
        {
            Debug.Log("Player cannot start Green-colored. Changing to Blue.");
            playerColor = Enums.Color.Blue;
        }
        if (playerColor == Enums.Color.Orange)
        {
            player.Color1 = Enums.Color.Red;
            player.Color2 = Enums.Color.Yellow;
        }
        else if (playerColor == Enums.Color.Violet)
        {
            player.Color1 = Enums.Color.Red;
            player.Color2 = Enums.Color.Blue;
        }
        else
        {
            player.Color1 = playerColor;
            player.Color2 = Enums.Color.None;
        }
        player.SetColor(playerColor, false); // Setting the default color is not undo-able
    }
}
