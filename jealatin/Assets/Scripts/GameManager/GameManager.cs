using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // Singleton GameManager, refer to it with GameManager.Instance

    public Dictionary<Enums.Color, Color> Palette = new Dictionary<Enums.Color, Color> // Color presets for Player/Object color changing
    {
        {Enums.Color.None, new UnityEngine.Color(0.86f, 0.86f, 0.86f, 1f)},
        {Enums.Color.Blue, new UnityEngine.Color(0.26f, 0.6f, 0.86f, 1f)},
        {Enums.Color.Yellow, new UnityEngine.Color(1f, 0.93f, 0.32f, 1f)},
        {Enums.Color.Green, new UnityEngine.Color(0.27f, 0.76f, 0.34f, 1f)},
        {Enums.Color.Red, new UnityEngine.Color(0.86f, 0.26f, 0.26f, 1f)},
        {Enums.Color.Violet, new UnityEngine.Color(0.6f, 0.26f, 0.86f, 1f)},
        {Enums.Color.Orange, new UnityEngine.Color(1f, 0.70f, 0.33f, 1f)},
        {Enums.Color.Black, new UnityEngine.Color(0.26f, 0.26f, 0.26f, 1f)},
    };

    private Enums.GameMode gameMode;
    public Enums.GameMode GameMode { get => gameMode; set => gameMode = value; }

    private BoundsInt stageSize = new BoundsInt(); // Automatically resize the camera. Defined from StageManager, used by CameraPosition
    public BoundsInt StageSize  { get => stageSize; set => stageSize = value; }

    private float defaultAnimationSpeed;
    private float defaultSoundPitch;
    public float animationSpeed = 1f;
    public float shiftSpeed = 2f;
    public float pushSpeedMultiplier = 0.75f;
    public float bumpSpeedMultiplier = 0.5f;

    [SerializeField] private AudioClip[] sfx;
    private AudioSource sound;

    [SerializeField] private Image[] pauseIcons;
    private Animator MainAnimator;
    private Enums.GameMode storedPauseMode;
    private bool isAnimating;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else if (Instance == null)
        {
            Instance = this;
            defaultAnimationSpeed = animationSpeed;
            defaultSoundPitch = animationSpeed;
            DontDestroyOnLoad(this.gameObject);

            MainAnimator = GetComponentInChildren<Animator>();
            sound = GetComponentInChildren<AudioSource>();
            GameMode = Enums.GameMode.MainMenu;

            // Change blendStyleIndex so an error doesn't pop up for the first frame when there
            //  are two GameManagers in a scene with blendStyleIndex == 0
            GetComponentInChildren<Light2D>().blendStyleIndex = -1;
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (isAnimating) return;

        // Gameplay speed-up by holding shift. Useful for long undos.
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
        {
            animationSpeed = GameManager.Instance.shiftSpeed;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift))
        {
            ResetAnimationSpeed();
        }
        // if (Input.GetKeyDown(KeyCode.P)) Debug.Log("GameMode: " + GameMode);

        // Open pause menu
        if (GameMode == Enums.GameMode.Game)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                TogglePause();
            }
        }
        else if (GameMode == Enums.GameMode.PauseMenu)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)
             || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) TogglePause();

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                GameMode = Enums.GameMode.LevelSelect;
                LoadScene(0);
            }

            if (pauseIcons[2].gameObject.activeSelf && (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))) LoadNextScene();
            if (pauseIcons[0].gameObject.activeSelf && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))) LoadPrevScene();
        }
    }

    public bool canPlayerMove()
    {
        if (gameMode == Enums.GameMode.Game) return true;
        return false;
    }

    public void ResetAnimationSpeed()
    {
        animationSpeed = defaultAnimationSpeed;
    }
    private void UpdateAnimationSpeed()
    {
        MainAnimator.speed = GameManager.Instance.animationSpeed;
    }

    public void LoadNextScene(bool fade = true)
    {
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene >= GetTotalBuildScenes()) // Reached the end of the levels -> go to level select
        {
            if (GameMode == Enums.GameMode.LevelClear) GameMode = Enums.GameMode.LevelSelect;
            else GameMode = Enums.GameMode.MainMenu;
            LoadScene(0);
        }

        else if (!fade) SceneManager.LoadScene(nextScene);
        else StartCoroutine(LoadNextSceneCoroutine(nextScene));
    }

    public void LoadPrevScene(bool fade = true)
    {
        int prevScene = SceneManager.GetActiveScene().buildIndex - 1;

        if (prevScene < 0) prevScene = 0;

        if (!fade) SceneManager.LoadScene(prevScene);
        else StartCoroutine(LoadNextSceneCoroutine(prevScene));
    }

    public void LoadScene(int buildIndex)
    {
        bool fade = true;
        if (!fade) SceneManager.LoadScene(buildIndex);
        else StartCoroutine(LoadNextSceneCoroutine(buildIndex));
    }

    // public void LoadScene(string sceneName, bool fade = true)
    // {
    //     if (fade) isAnimating = true;

    //     int nextScene = SceneManager.GetSceneByName(sceneName).buildIndex;
    //     if (!fade)
    //     {
    //         SceneManager.LoadScene(nextScene);
    //     }
    //     else
    //     {
    //         StopAllCoroutines();
    //         StartCoroutine(LoadNextSceneCoroutine(nextScene));
    //     }
    // }

    public int GetSceneBuildIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }

    public int GetTotalBuildScenes()
    {
        return SceneManager.sceneCountInBuildSettings;
    }

    private IEnumerator LoadNextSceneCoroutine(int buildIndex)
    {
        UpdateAnimationSpeed();
        MainAnimator.ResetTrigger("Trigger");

        MainAnimator.SetInteger("AnimationType", 1); // 1 = Fade to black
        MainAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.666f / animationSpeed);
        MainAnimator.ResetTrigger("Trigger");

        SceneManager.LoadScene(buildIndex);

        MainAnimator.SetInteger("AnimationType", 0); // 0 = Fade from black
        MainAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.666f / animationSpeed);
        MainAnimator.ResetTrigger("Trigger");

        if (buildIndex == 1) GameMode = Enums.GameMode.Cutscene;
        else if (buildIndex > 1) GameMode = Enums.GameMode.Game;

        isAnimating = false;
    }

    public void PlaySound(int clipID)
    {
        sound.Stop();
        sound.pitch = animationSpeed; // Speed up clip if game is sped up
        sound.clip = sfx[clipID];
        Debug.Log("Sfx " + clipID + " Played!");
        sound.Play();
    }
    public void PlaySound(string sfxName)
    {
        int sfxID = 0;
        foreach (var c in sfx)
        {
            if (c.name == sfxName)
            {
                break;
            }
            else sfxID++;
        }
        if (sfxID >= sfx.Length) Debug.Log("Sound " + sfxID + " does not exist!");
        PlaySound(sfxID);
    }

    public void TogglePause()
    {
        if (isAnimating) return;
        // if (GameMode != Enums.GameMode.PauseMenu) storedPauseMode = GameMode;
        if (GameMode != Enums.GameMode.PauseMenu) storedPauseMode = Enums.GameMode.Game;
        isAnimating = true;
        StartCoroutine(TogglePauseCoroutine());
    }

    private IEnumerator TogglePauseCoroutine()
    {
        // if (!MainAnimator.GetCurrentAnimatorStateInfo(0).IsName("Main")
        //    && !MainAnimator.GetCurrentAnimatorStateInfo(0).IsName("Pause On")) yield break;

        UpdateAnimationSpeed();
        if (GameMode == Enums.GameMode.PauseMenu)
        {
            MainAnimator.SetInteger("AnimationType", 0); // Close pause menu
        }
        else
        {
            MainAnimator.SetInteger("AnimationType", 2); // Open pause menu

            foreach (var i in pauseIcons)
            {
                i.gameObject.SetActive(true);
                //i.color = new Color(0.75f, 0.83f, 1f, 0.7f);
            }

            if (GetSceneBuildIndex() >= GetTotalBuildScenes() - 1 - 1) // No more scenes, grey out/disable next
                                                                        // extra -1 due to the end cutscene
            {
                pauseIcons[2].gameObject.SetActive(false);
            }
            else if (GetSceneBuildIndex() == 2) // First gameplay scene, can't go back
            {
                pauseIcons[0].gameObject.SetActive(false);
            }
        }

        MainAnimator.ResetTrigger("Trigger");
        MainAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.34f / animationSpeed);
        MainAnimator.ResetTrigger("Trigger");

        if (GameMode == Enums.GameMode.PauseMenu)
        {
            GameMode = storedPauseMode;
        }
        else
        {
            GameMode = Enums.GameMode.PauseMenu;
        }
        isAnimating = false;
        
        Debug.Log("Toggle Pause Coroutine  - Game mode is now: " + GameMode);
    }
}
