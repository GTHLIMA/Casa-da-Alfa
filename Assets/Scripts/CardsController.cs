using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;

public class CardsController : MonoBehaviour
{
    [Header("Prefab / Grid")]
    [SerializeField] Card cardPrefab;
    [SerializeField] Transform gridTransform;

    [Header("Assets (sprite[i] <-> cardAudios[i])")]
    [SerializeField] Sprite[] sprites;
    [SerializeField] AudioClip[] cardAudios;

    [Header("Config de rounds (pares por round)")]
    [SerializeField] int[] pairsPerRound = { 3, 4, 5, 5 };

    private List<Sprite> spritePairs;
    private List<AudioClip> audioPairs;

    private Card firstSelected;
    private Card secondSelected;

    private int matchCounts;
    private int currentRound = 0;
    private bool canSelect = true;

    private void Start()
    {
        StartRound();
    }

    private void StartRound()
    {
        matchCounts = 0;
        firstSelected = null;
        secondSelected = null;
        canSelect = true;

        ClearGrid();
        PrepareSpritesForRound();
        CreateCards();
    }

    private void ClearGrid()
    {
        foreach (Transform child in gridTransform)
        {
            Destroy(child.gameObject);
        }
    }

    private void PrepareSpritesForRound()
    {
        spritePairs = new List<Sprite>();
        audioPairs = new List<AudioClip>();

        int pairsThisRound = pairsPerRound[Mathf.Clamp(currentRound, 0, pairsPerRound.Length - 1)];

        int startIndex = 0;
        for (int r = 0; r < currentRound; r++) startIndex += pairsPerRound[r];

        for (int i = 0; i < pairsThisRound; i++)
        {
            int idx = startIndex + i;
            if (idx >= sprites.Length)
            {
                Debug.LogWarning($"CardsController: sprite faltando para round {currentRound + 1} no índice {idx}.");
                break;
            }

            spritePairs.Add(sprites[idx]);
            spritePairs.Add(sprites[idx]);

            AudioClip a = (cardAudios != null && idx < cardAudios.Length) ? cardAudios[idx] : null;
            audioPairs.Add(a);
            audioPairs.Add(a);
        }

        ShufflePairs(spritePairs, audioPairs);
    }

    private void CreateCards()
    {
        for (int i = 0; i < spritePairs.Count; i++)
        {
            Card card = Instantiate(cardPrefab, gridTransform);
            card.Initialize(spritePairs[i], (audioPairs != null && i < audioPairs.Count) ? audioPairs[i] : null, this);
        }
    }

    public void SetSelected(Card card)
    {
        if (!canSelect || !card.isSelected) return;
        if (firstSelected == card) return;

        canSelect = false;
        card.Show();

        card.PlayAudio(() =>
        {
            if (firstSelected == null)
            {
                firstSelected = card;
                canSelect = true;
            }
            else if (secondSelected == null)
            {
                secondSelected = card;
                StartCoroutine(CheckMatching(firstSelected, secondSelected));
            }
        });
    }

    private IEnumerator CheckMatching(Card a, Card b)
    {
        yield return new WaitForSeconds(0.3f);

        if (a.iconSprite == b.iconSprite)
        {
            matchCounts++;

            if (matchCounts >= spritePairs.Count / 2)
            {
                PrimeTween.Sequence.Create()
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one * 1.2f, 0.2f, ease: PrimeTween.Ease.OutBack))
                    .Chain(PrimeTween.Tween.Scale(gridTransform, Vector3.one, 0.1f));

                yield return new WaitForSeconds(0.5f);

                currentRound++;
                if (currentRound < pairsPerRound.Length)
                {
                    StartRound();
                    yield break;
                }
                else
                {
                    Debug.Log("CardsController: todos os rounds concluídos!");
                }
            }
        }
        else
        {
            a.Hide();
            b.Hide();
        }

        firstSelected = null;
        secondSelected = null;
        canSelect = true;
    }

    private void ShufflePairs(List<Sprite> spritesList, List<AudioClip> audiosList)
    {
        for (int i = spritesList.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            Sprite temp = spritesList[i];
            spritesList[i] = spritesList[randomIndex];
            spritesList[randomIndex] = temp;

            if (audiosList != null && audiosList.Count == spritesList.Count)
            {
                AudioClip ta = audiosList[i];
                audiosList[i] = audiosList[randomIndex];
                audiosList[randomIndex] = ta;
            }
        }
    }
}
