using UnityEngine;
using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;


public class FireBaseManager : MonoBehaviour
{
    private DatabaseReference dbRef;

    [Header("UI Campos")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;

    // private string loggedUser = null;

    void Start()
    {
        // Inicializa o Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase Database inicializado!");
            }
            else
            {
                Debug.LogError("Erro Firebase: " + task.Result);
            }
        });
    }

    // ===== CADASTRAR =====
    public void RegisterUser()
    {
        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Preencha usuário e senha");
            return;
        }

        // Dados básicos do usuário
        var userData = new UserData(username, password);
        string json = JsonUtility.ToJson(userData);

        dbRef.Child("users").Child(username).SetRawJsonValueAsync(json).ContinueWith(task =>
        {
            if (task.IsFaulted) Debug.LogError("Erro cadastro: " + task.Exception);
            else
            {
                Debug.Log("Usuário cadastrado: " + username);
                dbRef.Child("users").Child(username).Child("createdAt")
                .SetValueAsync(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
        });
    }

    // ===== LOGIN =====
    public void LoginUser()
    {
        Debug.Log("Botão de Login clicado");

        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Debug.LogWarning("Preencha usuário e senha");
            return;
        }

        dbRef.Child("users").Child(username).Child("password").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.Result == null || !task.Result.Exists)
            {
                Debug.LogError("Usuário não existe");
                return;
            }

            string savedPass = task.Result.Value.ToString();

            if (savedPass == password)
            {
                Debug.Log("Login OK: " + username);

                FirebaseUserSession.Instance.SetUser(username);

                Debug.Log("Tentando carregar cena: Game1");

               UnityMainThreadDispatcher.Enqueue(() =>
                {
                    LoadScenes.LoadSceneByIndex(3); // Call the static method directly
                });
            }
            else
            {
                Debug.LogError("Senha incorreta");
            }
        });
    }
}

// Classe auxiliar para converter em JSON
[System.Serializable]
public class UserData
{
    public string username;
    public string password;

    public UserData(string u, string p)
    {
        username = u;
        password = p;
    }
}
