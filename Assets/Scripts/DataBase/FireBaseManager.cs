using UnityEngine;
using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class FireBaseManager : MonoBehaviour
{
    private DatabaseReference dbRef;

    [Header("UI Campos")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_Text statusText;

    private bool isFirebaseReady = false;

    void Start()
    {
        // Inicializa o Firebase
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                dbRef = FirebaseDatabase.DefaultInstance.RootReference;
                isFirebaseReady = true;
                Debug.Log("Firebase Database inicializado!");
                UpdateStatus("Sistema pronto");
            }
            else
            {
                Debug.LogError("Erro Firebase: " + task.Result);
                UpdateStatus("Erro na conexão");
            }
        });
    }

    // ===== CADASTRAR =====
    public void RegisterUser()
    {
        if (!isFirebaseReady)
        {
            UpdateStatus("Sistema não inicializado");
            return;
        }

        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Preencha usuário e senha");
            return;
        }

        if (password.Length < 3)
        {
            UpdateStatus("Senha muito curta");
            return;
        }

        UpdateStatus("Verificando usuário...");
        StartCoroutine(RegisterUserCoroutine(username, password));
    }

    private IEnumerator RegisterUserCoroutine(string username, string password)
    {
        var checkTask = dbRef.Child("users").Child(username).Child("username").GetValueAsync();
        yield return new WaitUntil(() => checkTask.IsCompleted);

        if (checkTask.Result != null && checkTask.Result.Exists)
        {
            UpdateStatus("Usuário já existe!");
            yield break;
        }

        UpdateStatus("Criando conta...");

        // Dados do usuário
        var userData = new UserData(username, password);
        string json = JsonUtility.ToJson(userData);

        var registerTask = dbRef.Child("users").Child(username).SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsFaulted)
        {
            UpdateStatus("Erro no cadastro");
            Debug.LogError("Erro cadastro: " + registerTask.Exception);
        }
        else
        {

            var dateTask = dbRef.Child("users").Child(username).Child("createdAt")
                .SetValueAsync(System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            yield return new WaitUntil(() => dateTask.IsCompleted);

            UpdateStatus("Conta criada com sucesso!");
            Debug.Log("Usuário cadastrado: " + username);

            // yield return new WaitForSeconds(1f);
            LoginAfterRegister(username);
        }
    }

    // ===== LOGIN =====
    public void LoginUser()
    {
        if (!isFirebaseReady)
        {
            UpdateStatus("Sistema não inicializado");
            return;
        }

        Debug.Log("Botão de Login clicado");

        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Preencha usuário e senha");
            return;
        }

        UpdateStatus("Conectando...");
        StartCoroutine(LoginUserCoroutine(username, password));
    }

    private IEnumerator LoginUserCoroutine(string username, string password)
    {
        var loginTask = dbRef.Child("users").Child(username).GetValueAsync();
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.IsFaulted || loginTask.Result == null || !loginTask.Result.Exists)
        {
            UpdateStatus("Usuário não existe");
            yield break;
        }

        string savedPass = loginTask.Result.Child("password")?.Value?.ToString();

        if (savedPass == password)
        {
            UpdateStatus("Login deu certo!");
            Debug.Log("Login OK: " + username);


            if (FirebaseUserSession.Instance != null)
            {
                FirebaseUserSession.Instance.SetUser(username);
            }

            // yield return new WaitForSeconds(1f);
            LoadScenes.LoadSceneByIndex(1);
        }
        else
        {
            UpdateStatus("Senha incorreta");
        }
    }

    private void LoginAfterRegister(string username)
    {
        if (FirebaseUserSession.Instance != null)
        {
            FirebaseUserSession.Instance.SetUser(username);
        }

        LoadScenes.LoadSceneByIndex(1);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"Status: {message}");
    }
}

[System.Serializable]
public class UserData
{
    public string username;
    public string password;
    public string createdAt;

    public UserData(string u, string p)
    {
        username = u;
        password = p;
        createdAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}