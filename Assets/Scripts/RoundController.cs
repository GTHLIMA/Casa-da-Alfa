using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundController : MonoBehaviour
{
    [System.Serializable]
    public class SyllableData
    {
        public string syllableName;      // Ex: "BA"
        public Sprite syllableImage;     // Imagem da sílaba (ex: BA.png)
        public AudioClip syllableAudio;  // Áudio da sílaba (ex: "BA de...")
        public OptionData correctOption; // A figura correta que começa com a sílaba
    }

    [System.Serializable]
    public class OptionData
    {
        public string optionName;        // Nome da figura (ex: "Bala", "Casa")
        public Sprite optionImage;       // Imagem da figura
        public AudioClip optionAudio;    // Som do nome da figura
    }

    [Header("Referências de UI")]
    public Image syllablePanel;          // Painel que mostra a imagem da sílaba
    public Transform optionsParent;      // Pai dos 4 quadrados de opções
    public GameObject optionPrefab;      // Prefab de cada botão de opção

    [Header("Banco de Dados")]
    public List<SyllableData> syllables; // Lista de sílabas (BA, CA, DA...)
    public List<OptionData> allOptions;  // Todas as opções disponíveis (para distratores)

    private List<GameObject> currentOptions = new List<GameObject>();
    private int currentRound = 0;
    private GameManager5 gm;

    private bool inputLocked = true; // Bloqueia cliques enquanto sons tocam

    void Start()
    {
        gm = FindAnyObjectByType<GameManager5>();
        StartRound();
    }

    public void StartRound()
    {
        ClearOptions();

        if (currentRound >= syllables.Count)
        {
            Debug.Log("Jogo finalizado!");
            return;
        }

        // Pega a sílaba do round atual
        SyllableData data = syllables[currentRound];

        // Atualiza painel da sílaba
        syllablePanel.sprite = data.syllableImage;

        // Toca o som da sílaba
        inputLocked = true;
        gm.PlaySyllable(data.syllableAudio);
        Invoke(nameof(UnlockAfterSyllable), data.syllableAudio.length);

        // Monta opções
        GenerateOptions(data);
    }

    private void UnlockAfterSyllable()
    {
        inputLocked = false;
    }

    private void GenerateOptions(SyllableData correctData)
    {
        List<OptionData> pool = new List<OptionData>(allOptions);

        // Remove a correta do pool
        pool.Remove(correctData.correctOption);

        // Pega 3 distratores aleatórios
        List<OptionData> chosen = new List<OptionData>();
        for (int i = 0; i < 3; i++)
        {
            int r = Random.Range(0, pool.Count);
            chosen.Add(pool[r]);
            pool.RemoveAt(r);
        }

        // Adiciona a correta na lista
        chosen.Add(correctData.correctOption);

        // Embaralha
        Shuffle(chosen);

        // Instancia opções na tela
        foreach (var opt in chosen)
        {
            GameObject btn = Instantiate(optionPrefab, optionsParent);
            currentOptions.Add(btn);

            Image img = btn.GetComponent<Image>();
            img.sprite = opt.optionImage;

            Button b = btn.GetComponent<Button>();
            b.onClick.AddListener(() => OnOptionClicked(opt, correctData.correctOption));
        }
    }

    private void OnOptionClicked(OptionData clicked, OptionData correct)
    {
        if (inputLocked) return;

        inputLocked = true;

        // Sempre toca o som da figura clicada
        gm.PlayOption(clicked.optionAudio);

        // Verifica acerto/erro
        if (clicked == correct)
        {
            Debug.Log("Acertou!");
            gm.PlayCorrect();
            Invoke(nameof(NextRound), clicked.optionAudio.length + 0.5f);
        }
        else
        {
            Debug.Log("Errou!");
            gm.PlayWrong();
            Invoke(nameof(UnlockAfterOption), clicked.optionAudio.length);
        }
    }

    private void UnlockAfterOption()
    {
        inputLocked = false;
    }

    private void NextRound()
    {
        currentRound++;
        StartRound();
    }

    private void ClearOptions()
    {
        foreach (var opt in currentOptions)
            Destroy(opt);
        currentOptions.Clear();
    }

    private void Shuffle(List<OptionData> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            var tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }
}
