using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton GameManager, refer to it with GameManager.Instance

    private Enums.GameMode gameMode;
    public Enums.GameMode GameMode { get => gameMode; set => gameMode = value; }

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
