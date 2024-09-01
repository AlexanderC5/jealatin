using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    private Player player;
    [SerializeField] private Vector2 MapSize; // Automatically resize the camera
    [SerializeField] private Vector3 CameraOffset; // Manual camera offset in inspector (probably unnecessary)

    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
    }

    void Start()
    {
        player.FullReset();
    }
}
