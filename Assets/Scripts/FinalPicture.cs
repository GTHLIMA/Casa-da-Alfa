using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FinalPicture : MonoBehaviour
{
    public RawImage imagemFinal;

    void Start()
    {
        if (GlobalData.profilePicture != null)
        {
            imagemFinal.texture = GlobalData.profilePicture;
        }

    }
}
