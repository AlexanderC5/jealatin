using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

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
    public float animationSpeed = 1f;
    public float shiftSpeed = 2f;
    public float pushSpeedMultiplier = 0.75f;
    public float bumpSpeedMultiplier = 0.5f;

    
    private Animator MainAnimator;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else if (Instance == null)
        {
            Instance = this;
            defaultAnimationSpeed = animationSpeed;
            DontDestroyOnLoad(this.gameObject);

            MainAnimator = GetComponentInChildren<Animator>();
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

    public void LoadNextScene(bool fade = false)
    {
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextScene > SceneManager.sceneCount) // Reached the end of the levels -> go to level select
        {
            if (GameMode == Enums.GameMode.LevelClear) GameMode = Enums.GameMode.LevelSelect;
            else GameMode = Enums.GameMode.MainMenu;
            LoadScene(0);
        }

        else if (!fade) SceneManager.LoadScene(nextScene);
        else StartCoroutine(LoadNextSceneCoroutine(nextScene));
    }

    public void LoadScene(int buildIndex, bool fade = true)
    {
        if (!fade) SceneManager.LoadScene(buildIndex);
        else StartCoroutine(LoadNextSceneCoroutine(buildIndex));
    }

    public void LoadScene(string sceneName, bool fade = true)
    {
        int nextScene = SceneManager.GetSceneByName(sceneName).buildIndex;
        if (!fade) SceneManager.LoadScene(nextScene);
        else StartCoroutine(LoadNextSceneCoroutine(nextScene));
    }

    private IEnumerator LoadNextSceneCoroutine(int buildIndex)
    {
        UpdateAnimationSpeed();

        MainAnimator.SetInteger("AnimationType", 1); // 1 = Fade to black
        MainAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.666f / animationSpeed);
        MainAnimator.ResetTrigger("Trigger");

        if (buildIndex == 1) GameMode = Enums.GameMode.Cutscene;
        else if (buildIndex > 1) GameMode = Enums.GameMode.Game;
        SceneManager.LoadScene(buildIndex);

        MainAnimator.SetInteger("AnimationType", 0); // 0 = Fade from black
        MainAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.666f / animationSpeed);
        MainAnimator.ResetTrigger("Trigger");
    }
}
