using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ProfilePicture : MonoBehaviour
{
     public RawImage imagemPerfil;

    public void SelecionarImagem()
{
    NativeGallery.GetImageFromGallery((path) =>
    {
        if (path != null)
        {
            Texture2D textura = NativeGallery.LoadImageAtPath(path, 512);
            if (textura != null)
            {
                // Mostra na tela
                imagemPerfil.texture = textura;

                // Salva para a próxima cena
                GlobalData.profilePicture = textura;
            }
        }
    }, "Escolha uma imagem", "image/*");
}



}
