using UnityEngine;
using TMPro;

public class ShowName : MonoBehaviour
{
    public TMP_Text textName;

    void Start()
    {
        textName.text = GlobalData.playerName;
    }
}
