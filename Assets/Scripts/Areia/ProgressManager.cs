using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    [Header("Prefabs – podem ser arrastados OU deixar vazio para criar via código")]


    public UnityEvent<char> OnLetterCompleted = new UnityEvent<char>();

    void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        // opcional: não persiste
    }
    else
    {
        Destroy(gameObject);
        return;
    }


}

    public void MarkCompleted(char letter)
    {
        PlayerPrefs.SetInt("Letter_" + letter, 1);
        OnLetterCompleted.Invoke(letter);
        Debug.Log("Concluiu a letra " + letter);
    }

    public bool IsCompleted(char letter)
    {
        return PlayerPrefs.GetInt("Letter_" + letter, 0) == 1;
    }

}