using UnityEngine;

public class FirebaseUserSession : MonoBehaviour
{
    public static FirebaseUserSession Instance;

    public string LoggedUser { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetUser(string username)
    {
        LoggedUser = username;
        Debug.Log("🔥 Usuário ativo: " + username);
    }
}
