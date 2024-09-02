using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private Image[] MenuButtons;
    [SerializeField] private Image[] LevelButtons;
    [SerializeField] private Image[] TitleLetters;
    [SerializeField] private ParticleSystem ZParticles;

    [SerializeField] private Image overlay;

    private Vector2 navVector = Vector2.zero;
    private int curMenuSelect = 0;
    private int curLevelSelect = 0;

    private Coroutine inputDelayCoroutine;
    private float inputDelay = 0.2f;
    public bool inputBlocked = false;

    private Animator TitleAnimator;

    void Awake()
    {
        TitleAnimator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        HighlightMenuOption(0);
        TitleAnimator.ResetTrigger("Trigger");

        if (GameManager.Instance.GameMode == Enums.GameMode.MainMenu)
        {  
            curMenuSelect = 0;
            TitleAnimator.SetInteger("AnimationType", 0);
        }
        else if (GameManager.Instance.GameMode == Enums.GameMode.LevelSelect)
        {
            curMenuSelect = 1;
            ZParticles.Pause();
            ZParticles.Clear();
            TitleAnimator.SetInteger("AnimationType", 1);
        }
    }

    void Update()
    {
        if (!inputBlocked)
        {
            // Set the navigation vector
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) navVector = Vector2.up;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) navVector = Vector2.down;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) navVector = Vector2.right;
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) navVector = Vector2.left;
        
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) navVector.x = 2;
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) navVector.x = -2;

        // Call function based on the game state
        if (GameManager.Instance.GameMode == Enums.GameMode.MainMenu) { SelectMenuOption(); }
        else if (GameManager.Instance.GameMode == Enums.GameMode.LevelSelect) { SelectLevel(); }
        navVector = Vector2.zero;
    }

    private void SelectMenuOption()
    {
        if (navVector == Vector2.zero) return; /// No input detected
        inputDelayCoroutine = StartCoroutine(InputDelay());

        GameManager.Instance.PlaySound("move_sfx");

        switch(curMenuSelect)
        {
            case 0: // Start
                if (navVector == Vector2.right || navVector.x == 2)
                {
                    StartGame();
                }
                if (navVector == Vector2.up) HighlightMenuOption(2);
                if (navVector == Vector2.down) HighlightMenuOption(1);
                break;
            case 1: // Skip Intro
                if (navVector == Vector2.right || navVector.x == 2)
                {
                    ZParticles.Pause();
                    GameManager.Instance.LoadScene(2);
                }
                if (navVector == Vector2.up) HighlightMenuOption(0);
                if (navVector == Vector2.down) HighlightMenuOption(2);
                break;
            case 2: // Level Select
                if (navVector == Vector2.right || navVector.x == 2)
                {
                    ToggleLevelSelect();
                }
                if (navVector == Vector2.up) HighlightMenuOption(1);
                if (navVector == Vector2.down) HighlightMenuOption(0); // Jump over settings
                break;
            case 3: // Exit
                if (navVector == Vector2.right || navVector.x == 2) Application.Quit();
                if (navVector == Vector2.up) HighlightMenuOption(1); // Jump over settings
                if (navVector == Vector2.down) HighlightMenuOption(0);
                break;
            default:
                Debug.Log("Menu option does not exist!");
                break;
        }

        navVector = Vector2.zero;
    }

    private void HighlightMenuOption(int select)
    {
        //Debug.Log("Highlighting menu option " + select);
        for (int i = 0; i < MenuButtons.Length; i++) // These menu options are not selected:
        {
            MenuButtons[i].color = new Color(0.7f, 0.8f, 0.8f, 0.65f);
            MenuButtons[i].gameObject.transform.localScale = Vector3.one;
            MenuButtons[i].GetComponentsInChildren<Image>()[1].enabled = false;
        }
        MenuButtons[select].color = new Color(0.8f, 0.9f, 0.9f, 0.9f);
        MenuButtons[select].gameObject.transform.localScale = new Vector3(1.05f, 1.05f);
        MenuButtons[select].GetComponentsInChildren<Image>()[1].enabled = true;
        curMenuSelect = select;
    }

    private void SelectLevel()
    {
        if (navVector == Vector2.zero) return; /// No input detected
        inputDelayCoroutine = StartCoroutine(InputDelay());

        GameManager.Instance.PlaySound("move_sfx");
        
        if (navVector.x == -2) // Exit level select
        {
            ToggleLevelSelect();
            return;
        }

        if (navVector.x == 2)
        {
            GameManager.Instance.LoadScene(curLevelSelect + 2);
        }

        if (navVector == Vector2.up)
        {
            curLevelSelect -= 4;
            if (curLevelSelect < 0) curLevelSelect = 0;
        }
        else if (navVector == Vector2.right)
        {
            curLevelSelect += 1;
            if (curLevelSelect >= LevelButtons.Length) curLevelSelect = LevelButtons.Length - 1;

        }
        else if (navVector == Vector2.down)
        {
            curLevelSelect += 4;
            if (curLevelSelect >= LevelButtons.Length) curLevelSelect = LevelButtons.Length - 1;
        }
        else if (navVector == Vector2.left)
        {
            curLevelSelect -= 1;
            if (curLevelSelect < 0) curLevelSelect = 0;
        }
        
        navVector = Vector2.zero;
        HighlightLevel(curLevelSelect);
    }
    private void HighlightLevel(int select)
    {
        //Debug.Log("Highlighting menu option " + select);
        for (int i = 0; i < LevelButtons.Length; i++) // These menu options are not selected:
        {
            LevelButtons[i].color = new Color(0.73f, 0.89f, 1f, 0.84f);
            LevelButtons[i].gameObject.transform.localScale = Vector3.one;
        }
        LevelButtons[select].color = new Color(1f, 0.98f, 0.73f, 0.84f);
        LevelButtons[select].gameObject.transform.localScale = new Vector3(1.05f, 1.05f);
        curLevelSelect = select;
    }


    IEnumerator InputDelay()
    {
        if (!inputBlocked)
        {
            inputBlocked = true;
            yield return new WaitForSeconds(inputDelay);
            inputBlocked = false;
        }
    }

    private void ToggleLevelSelect()
    {
        if (inputDelayCoroutine != null) StopCoroutine(inputDelayCoroutine); // Prevent input from unblocking during transition
        
        if (GameManager.Instance.GameMode == Enums.GameMode.MainMenu)
        {
            TitleAnimator.SetInteger("AnimationType", 1);
            ZParticles.Pause();
            ZParticles.Clear();
            GameManager.Instance.GameMode = Enums.GameMode.LevelSelect;
        }
        else
        {
            TitleAnimator.SetInteger("AnimationType", 0);
            ZParticles.Play();
            GameManager.Instance.GameMode = Enums.GameMode.MainMenu;
        }
        Debug.Log("ToggleLevelSelect called " + GameManager.Instance.GameMode);

        StartCoroutine(ToggleLevelSelectCoroutine());
    }

    IEnumerator ToggleLevelSelectCoroutine()
    {
        UpdateAnimationSpeed();
        inputBlocked = true;
        TitleAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.6f / GameManager.Instance.animationSpeed); // 40 frame animation, 60fps -> 0.66 seconds min. 0.6 to give player control a bit early
        TitleAnimator.ResetTrigger("Trigger");
        inputBlocked = false;
    }

    private void StartGame()
    {
        ZParticles.Pause();
        GameManager.Instance.LoadNextScene();
    }

    private void UpdateAnimationSpeed()
    {
        TitleAnimator.speed = GameManager.Instance.animationSpeed;
    }

    // private void FadeToSceneChange(string sceneName)
    // {
    //     ZParticles.Pause();
    //     ZParticles.Clear();
    //     TitleAnimator.SetInteger("AnimationType", -1); // -1 for Fade to Black
    //     GameManager.Instance.LoadNextScene();
    //     StartCoroutine(FadeToSceneChangeCoroutine(sceneName));
    // }

    // IEnumerator FadeToSceneChangeCoroutine(string sceneName)
    // {
    //     inputBlocked = true;
    //     TitleAnimator.SetTrigger("Trigger");
    //     yield return new WaitForSeconds(0.6f / GameManager.Instance.animationSpeed); // 40 frame animation, 60fps -> 0.66 seconds min. 0.6 to give player control a bit early
    //     TitleAnimator.ResetTrigger("Trigger");
    //     inputBlocked = false;

    //     // Change scene to __ in GameManager
    // }
}