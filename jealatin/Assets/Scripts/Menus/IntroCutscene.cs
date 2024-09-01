using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroCutscene : MonoBehaviour
{
    private Animator CutsceneAnimator;
    private Image image;
    [SerializeField] private Sprite[] imageSlides;
    private int currentSlide = 0;
    private bool isAnimating = false;

    // Start is called before the first frame update
    void Awake()
    {
        CutsceneAnimator = GetComponentInChildren<Animator>();
        isAnimating = false;
    }

    void Start()
    {
        image = GetComponentInChildren<Image>();
        image.sprite = imageSlides[currentSlide];
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) SlideTransition();
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) SlideTransition();
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) SlideTransition();
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) SlideTransition();
        else if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) SlideTransition();
        else if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) SlideTransition();
    }

    private void SlideTransition()
    {
        if (isAnimating) return;

        GameManager.Instance.PlaySound("move_sfx");

        currentSlide++;
        if (currentSlide >= imageSlides.Length)
        {
            GameManager.Instance.LoadNextScene();
            isAnimating = true;
            return;
        }
        else StartCoroutine(SlideTransitionCoroutine());
    }

    private IEnumerator SlideTransitionCoroutine()
    {
        isAnimating = true;
        UpdateAnimationSpeed();

        CutsceneAnimator.SetInteger("AnimationType", 1); // 1 = Fade to black
        CutsceneAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.333f / GameManager.Instance.animationSpeed);
        CutsceneAnimator.ResetTrigger("Trigger");

        yield return new WaitForSeconds(0);
        NextSlide();
        
        CutsceneAnimator.SetInteger("AnimationType", 0); // 0 = Fade from black
        CutsceneAnimator.SetTrigger("Trigger");
        yield return new WaitForSeconds(0.333f / GameManager.Instance.animationSpeed);
        CutsceneAnimator.ResetTrigger("Trigger");

        isAnimating = false;
    }

    private void NextSlide()
    {
        image.sprite = imageSlides[currentSlide];
    }

    private void UpdateAnimationSpeed()
    {
        CutsceneAnimator.speed = GameManager.Instance.animationSpeed;
    }
}
