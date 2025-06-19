using UnityEngine;

public class PermissionManager : MonoBehaviour
{
    void Start()
    {
        if (!SpeechToText.CheckPermission())
        {
            Debug.Log("PermissionManager: Permissão do microfone ainda não foi concedida. Solicitando...");
            RequestMicrophonePermission();
        }
        else
        {
            Debug.Log("PermissionManager: Permissão do microfone já havia sido concedida em uma sessão anterior.");
        }
    }

    private void RequestMicrophonePermission()
    {
        //caixa de diálogo nativa do Android/iOS
        SpeechToText.RequestPermissionAsync((permission) =>
        {
            //callback, ele executa depois que o usuário responde
            if (permission == SpeechToText.Permission.Granted)
            {
                Debug.Log("PermissionManager: Permissão CONCEDIDA pelo usuário.");
            }
            else
            {
                Debug.LogError("PermissionManager: Permissão NEGADA pelo usuário. Alguns recursos podem não funcionar.");
            }
        });
    }
}