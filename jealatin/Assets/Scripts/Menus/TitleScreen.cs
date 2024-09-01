using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TitleScreen : MonoBehaviour
{
    [SerializeField] private Image[] MenuButtons;
    [SerializeField] private Image[] TitleLetters;

    [SerializeField] private Image overlay;

    private Vector2 navVector = Vector2.zero;
    private int curMenuSelect = 0;

    private Coroutine inputDelayCoroutine;
    private float inputDelay = 0.25f;
    public bool inputBlocked = false;

    private Animator TitleAnimator;

    void Awake()
    {
        TitleAnimator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        HighlightMenuOption(0);
        GameManager.Instance.GameMode = Enums.GameMode.MainMenu;
        //overlay.color = new Color(0f, 0f, 0f, 0f);
        TitleAnimator.ResetTrigger("Trigger");
        TitleAnimator.SetInteger("AnimationType", 0);
    }

    void Update()
    {
        if (inputBlocked) return;            

        // Set the navigation vector
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) navVector = Vector2.up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) navVector = Vector2.down;
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) navVector = Vector2.right;
        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) navVector = Vector2.left;
        
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) navVector = Vector2.positiveInfinity;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) navVector = Vector2.negativeInfinity;

        // Call function based on the game state
        if (GameManager.Instance.GameMode == Enums.GameMode.MainMenu)
        {
            SelectMenuOption();
        }
        navVector = Vector2.zero;
    }

    private void SelectMenuOption()
    {
        if (navVector == Vector2.zero) return; /// No input detected

        inputDelayCoroutine = StartCoroutine(MenuInputDelay());

        if (navVector == Vector2.left || navVector == Vector2.negativeInfinity) // Highlight exit
        {
            HighlightMenuOption(MenuButtons.Length - 1); // Select exit
            return;
        }

        switch(curMenuSelect)
        {
            case 0: // Start
                if (navVector == Vector2.right || navVector == Vector2.positiveInfinity)
                {
                    // GameManager.Instance.musicPlayer.StopMusic();
                    //GameManager.Instance.ChangeToScene("Stage 1");
                }
                if (navVector == Vector2.up) HighlightMenuOption(3);
                if (navVector == Vector2.down) HighlightMenuOption(1);
                break;
            case 1: // Level Select
                if (navVector == Vector2.right || navVector == Vector2.positiveInfinity)
                {
                    ToggleLevelSelect();
                }
                if (navVector == Vector2.up) HighlightMenuOption(0);
                if (navVector == Vector2.down) HighlightMenuOption(2);
                break;
            case 2: // Settings
                if (navVector == Vector2.right || navVector == Vector2.positiveInfinity)
                {
                    Debug.Log("No settings yet");
                }
                if (navVector == Vector2.up) HighlightMenuOption(1);
                if (navVector == Vector2.down) HighlightMenuOption(3);
                break;
            case 3: // Exit
                if (navVector == Vector2.right || navVector == Vector2.positiveInfinity) Application.Quit();
                if (navVector == Vector2.up) HighlightMenuOption(2);
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
            MenuButtons[i].color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            MenuButtons[i].gameObject.transform.localScale = Vector3.one;
        }
        MenuButtons[select].color = new Color(0.66f, 0.73f, 0.8f, 0.9f);
        MenuButtons[select].gameObject.transform.localScale = new Vector3(1.05f, 1.05f);
        curMenuSelect = select;
    }

    IEnumerator MenuInputDelay()
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
            GameManager.Instance.GameMode = Enums.GameMode.LevelSelect;
        }
        else
        {
            TitleAnimator.SetInteger("AnimationType", 0);
            GameManager.Instance.GameMode = Enums.GameMode.MainMenu;
        }

        StartCoroutine(ToggleLevelSelectCoroutine());
    }

    IEnumerator ToggleLevelSelectCoroutine()
    {
        TitleAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.6f / GameManager.Instance.animationSpeed); // 40 frame animation, 60fps -> 0.66 seconds min. 0.6 to give player control a bit early
        TitleAnimator.ResetTrigger("Trigger");
    }

    private void UpdateAnimationSpeed()
    {
        TitleAnimator.speed = GameManager.Instance.animationSpeed;
    }
}