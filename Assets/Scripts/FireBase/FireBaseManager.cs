using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Auth;
using TMPro; // Para interagir com elementos de texto/input da UI

public class FireBaseManager : MonoBehaviour
{
    private DatabaseReference databaseReference;
    private FirebaseAuth auth;
    public TMP_InputField usernameInputField;

    void Start()
    {
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        // Verifica se todas as dependências do Firebase estão resolvidas
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                // Tá checando se a conexão tá correta, e um debug pra ficar fácil de saber o que tá acontecendo
                auth = FirebaseAuth.DefaultInstance;
                databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase inicializado com sucesso");
            }
            else
            {
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    public void SaveUsername()
    {
        // É o label pra colocar o nome, vai dar erro se não tive
        string username = usernameInputField.text;

        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("O campo de nome de usuário está vazio.");
            return;
        }

        
        StartCoroutine(SaveUsernameToDatabase(username));
    }

    private IEnumerator SaveUsernameToDatabase(string username)
    {
        // Isso tá gerando um id único pra um usuário
        string userId = System.Guid.NewGuid().ToString();

        DatabaseReference userRef = databaseReference.Child("users").Child(userId);

        var saveTask = userRef.Child("username").SetValueAsync(username);
        yield return new WaitUntil(() => saveTask.IsCompleted);

        if (saveTask.Exception != null)
        {
            Debug.LogError($"Falha ao salvar o nome: {saveTask.Exception}");
            
            // Um log mais específico pro erro
            FirebaseException firebaseEx = saveTask.Exception.GetBaseException() as FirebaseException;
            if (firebaseEx != null)
            {
                Debug.LogError($"Código de erro do Firebase: {firebaseEx.ErrorCode}");
            }
        }
        else
        {
            Debug.Log("Nome de usuário salvo com sucesso no Firebase!");
        }
    }

}