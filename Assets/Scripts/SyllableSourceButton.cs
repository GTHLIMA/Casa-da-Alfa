using UnityEngine;
using UnityEngine.UI;

public class SyllableSourceButton : MonoBehaviour
{
    private SyllablePuzzleManager manager;
    private SyllablePuzzleManager.OnScreenWord myData;
    private Button button;
    private Image image;
    private Image revealedSyllableImage;

    private void Awake()
    {
        // Procura por um filho com o nome exato "SilabaRevelada"
        Transform revealedSyllableTransform = transform.Find("SilabaRevelada");
        if (revealedSyllableTransform != null)
        {
            revealedSyllableImage = revealedSyllableTransform.GetComponent<Image>();
        }
        else
        {
            Debug.LogError("ERRO DE PREFAB: Não foi possível encontrar um filho chamado 'SilabaRevelada' no prefab " + gameObject.name);
        }
    }

    public void Setup(SyllablePuzzleManager.OnScreenWord data, SyllablePuzzleManager managerRef)
    {
        myData = data;
        manager = managerRef;
        image = GetComponent<Image>();
        image.sprite = data.drawingImage;

        button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        if (revealedSyllableImage != null)
        {
            revealedSyllableImage.sprite = data.syllableImage;
            revealedSyllableImage.gameObject.SetActive(false);
        }
    }

    private void OnClicked()
    {
      
        AudioManager audioManager = FindObjectOfType<AudioManager>();
        if(audioManager != null)
        {
          
            audioManager.PauseAudio(audioManager.background);
        }
        // ------------------------------------

        if (manager != null)
        {
            manager.OnSourceButtonClicked(myData, this);
        }
    }

    // CORREÇÃO: Adicionado o método público que o manager precisa para comandar a revelação.
    public void RevealLocalSyllable()
    {
        if (revealedSyllableImage != null)
        {
            revealedSyllableImage.gameObject.SetActive(true);
        }
    }

    public void SetUsed(bool used)
    {
        button.interactable = !used;
    }
}