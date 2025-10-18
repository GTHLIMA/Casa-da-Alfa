using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BalloonClickable : MonoBehaviour
{
    public SpriteRenderer balloonSpriteRenderer; // main balloon sprite
    public SpriteRenderer innerSyllableRenderer; // the small sprite that shows syllable and cycles

    [Tooltip("Sprites that will be shown sequentially when the player taps the balloon.")]
    public Sprite[] syllableStepSprites; // first is initial, last leads to pop
    public AudioClip[] syllableClips; // play when advancing steps

    public Sprite[] popAnimation;
    public float popFrameRate = 0.05f;

    public int currentStep = 0;

    public event Action onFinalPop;

    private bool isPopping = false;

    public float upSpeed = 0.02f;

    private void Start()
    {
        if (innerSyllableRenderer == null) Debug.LogWarning("innerSyllableRenderer not set");
        UpdateInnerSprite();
    }

    private void Update()
    {
        transform.Translate(0, upSpeed * Time.deltaTime, 0);
        if (transform.position.y > 10f) Destroy(gameObject);

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) HandleTouch(Input.mousePosition);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) HandleTouch(Input.GetTouch(0).position);
#endif
    }

    void HandleTouch(Vector2 screenPos)
    {
        Vector2 world = Camera.main.ScreenToWorldPoint(screenPos);
        if (GetComponent<Collider2D>().OverlapPoint(world))
        {
            AdvanceStep();
        }
    }

    public void SetSyllableSprite(Sprite s)
    {
        if (syllableStepSprites == null || syllableStepSprites.Length == 0) return;
        syllableStepSprites[0] = s;
        currentStep = 0;
        UpdateInnerSprite();
    }

    void UpdateInnerSprite()
    {
        if (innerSyllableRenderer != null && syllableStepSprites != null && syllableStepSprites.Length > 0)
        {
            innerSyllableRenderer.sprite = syllableStepSprites[Mathf.Clamp(currentStep, 0, syllableStepSprites.Length - 1)];
        }
    }

    void AdvanceStep()
    {
        if (isPopping) return;

        // play associated syllable audio
        if (currentStep < syllableClips.Length && syllableClips[currentStep] != null)
        {
            var mm = MainGameManager.Instance;
            if (mm != null && mm.syllableSource != null) mm.syllableSource.PlayOneShot(syllableClips[currentStep]);
        }

        currentStep++;
        if (currentStep >= syllableStepSprites.Length)
        {
            // final pop
            StartCoroutine(PopSequence());
        }
        else
        {
            UpdateInnerSprite();
        }
    }

    IEnumerator PopSequence()
    {
        isPopping = true;

        // play pop SFX
        if (MainGameManager.Instance != null && MainGameManager.Instance.sfxSource != null)
        {
            // TODO: assign a pop clip on MainGameManager or via inspector
        }

        // play pop animation frames if any
        var sr = balloonSpriteRenderer;
        if (popAnimation != null && popAnimation.Length > 0 && sr != null)
        {
            foreach (var f in popAnimation)
            {
                sr.sprite = f;
                yield return new WaitForSeconds(popFrameRate);
            }
        }

        // notify manager
        onFinalPop?.Invoke();

        Destroy(gameObject);
    }
}

