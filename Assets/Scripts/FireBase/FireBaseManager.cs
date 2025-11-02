using UnityEngine;
using Firebase;
using Firebase.Database;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class FireBaseManager : MonoBehaviour
{
    private DatabaseReference dbRef;
    private bool isFirebaseReady = false;

    [Header("UI Campos")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    public TMP_Text statusText;
    
    public int index;

    void Start()
    {
        InitializeFirebase();
    }

    // ===== FIREBASE INITIALIZATION =====
    private void InitializeFirebase()
    {
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

    // ===== REGISTER =====
    public void RegisterUser()
    {
        if (!isFirebaseReady)
        {
            UpdateStatus("Sistema não inicializado");
            return;
        }

        string username = usernameField.text.Trim();
        string password = passwordField.text.Trim();

        if (!ValidateCredentials(username, password))
            return;

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
        var checkTask = dbRef.Child("users").Child(username).Child("info").Child("username").GetValueAsync();
        yield return new WaitUntil(() => checkTask.IsCompleted);

        if (checkTask.Result != null && checkTask.Result.Exists)
        {
            UpdateStatus("Usuário já existe!");
            yield break;
        }

        UpdateStatus("Criando conta...");

        var userData = new UserData(username, password);
        string json = JsonUtility.ToJson(userData);

        var registerTask = dbRef.Child("users").Child(username).Child("info").SetRawJsonValueAsync(json);
        yield return new WaitUntil(() => registerTask.IsCompleted);

        if (registerTask.IsFaulted)
        {
            UpdateStatus("Erro no cadastro");
            Debug.LogError("Erro cadastro: " + registerTask.Exception);
        }
        else
        {
            UpdateStatus("Conta criada com sucesso!");
            Debug.Log("Usuário cadastrado: " + username);
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

        if (!ValidateCredentials(username, password))
            return;

        UpdateStatus("Conectando...");
        StartCoroutine(LoginUserCoroutine(username, password));
    }

    private IEnumerator LoginUserCoroutine(string username, string password)
    {
        var loginTask = dbRef.Child("users").Child(username).Child("info").GetValueAsync();
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

            LoadScenes.LoadSceneByIndex(index);
        }
        else
        {
            UpdateStatus("Senha incorreta");
        }
    }

    // ===== VALIDATION =====
    private bool ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            UpdateStatus("Preencha usuário e senha");
            return false;
        }
        return true;
    }

    // ===== UTILITIES =====
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