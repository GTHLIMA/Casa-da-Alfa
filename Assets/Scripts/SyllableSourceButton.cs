using UnityEngine;
using UnityEngine.UI;

public class SyllableSourceButton : MonoBehaviour
{
    private SyllablePuzzleManager manager;
    // Trocado para public para que o manager possa ler
    public SyllablePuzzleManager.SourceWordData myData { get; private set; }
    private Button button;
    private Image image;
    private Image silabaReveladaImage;

    private void Awake()
    {
        Transform silabaReveladaTransform = transform.Find("SilabaRevelada");
        if (silabaReveladaTransform != null)
        {
            silabaReveladaImage = silabaReveladaTransform.GetComponent<Image>();
        }
    }

    public void Setup(SyllablePuzzleManager.SourceWordData data, SyllablePuzzleManager managerRef)
    {
        myData = data;
        manager = managerRef;
        image = GetComponent<Image>();
        image.sprite = data.sourceDrawing;

        button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);

        if (silabaReveladaImage != null)
        {
            silabaReveladaImage.sprite = data.firstSyllableImage;
            silabaReveladaImage.gameObject.SetActive(false);
        }
    }

    private void OnClicked()
    {
        if (silabaReveladaImage != null)
        {
            silabaReveladaImage.gameObject.SetActive(true);
        }
        
        if (manager != null)
        {
            manager.PlaySyllableAudio(myData.firstSyllableAudio);
            SetUsed(true);
            manager.OnSourceButtonClicked(myData, this);
        }
    }

    public void SetUsed(bool used)
    {
        button.interactable = !used;
        image.color = used ? new Color(0.7f, 0.7f, 0.7f, 0.8f) : Color.white;
    }
}