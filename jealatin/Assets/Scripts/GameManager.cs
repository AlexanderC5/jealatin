using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton GameManager, refer to it with GameManager.Instance

    public Dictionary<Enums.Color, Color> Palette = new Dictionary<Enums.Color, Color> // Color presets for Player/Object color changing
    {
        {Enums.Color.None, new UnityEngine.Color(0.86f, 0.86f, 0.86f)},
        {Enums.Color.Blue, new UnityEngine.Color(0.26f, 0.6f, 0.86f)},
        {Enums.Color.Yellow, new UnityEngine.Color(1f, 0.93f, 0.32f)},
        {Enums.Color.Green, new UnityEngine.Color(0.27f, 0.76f, 0.34f)},
        {Enums.Color.Red, new UnityEngine.Color(0.86f, 0.26f, 0.26f)},
        {Enums.Color.Violet, new UnityEngine.Color(0.6f, 0.26f, 0.86f)},
        {Enums.Color.Orange, new UnityEngine.Color(1f, 0.70f, 0.33f)},
        {Enums.Color.Black, new UnityEngine.Color(0.26f, 0.26f, 0.26f)},
    };

    private Enums.GameMode gameMode;
    public Enums.GameMode GameMode { get => gameMode; set => gameMode = value; }

    public float animationSpeed = 1f;
    public float pushSpeedMultiplier = 0.75f;

    void Awake()
    {
        if (Instance != null)
            Destroy(Instance);
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool canPlayerMove()
    {
        if (gameMode == Enums.GameMode.Game) return true;
        return false;
    }
}
