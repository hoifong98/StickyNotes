using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public DatabaseReference DbReference { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFirebase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeFirebase()
    {
        Debug.Log("Checking Firebase dependencies...");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("CheckAndFixDependenciesAsync encountered an error: " + task.Exception);
                return;
            }

            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
            {
                Debug.Log("Firebase dependencies are available.");

                // Initialize the Firebase App and Database
                FirebaseApp app = FirebaseApp.DefaultInstance;

                // Optional: Set database URL manually if needed
                DbReference = FirebaseDatabase.GetInstance(
                    "https://sticky-notes-6942e-default-rtdb.asia-southeast1.firebasedatabase.app/"
                ).RootReference;

                IsInitialized = true;
                Debug.Log("Firebase initialized and database reference set.");
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + dependencyStatus);
            }
        });
    }
}
