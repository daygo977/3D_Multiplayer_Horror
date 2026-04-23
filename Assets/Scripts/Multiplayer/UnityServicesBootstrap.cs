using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

/// <summary>
/// Initializes Unity Services once for the whole game and signs player in anonymously
/// </summary>
public class UnityServicesBootstrap : MonoBehaviour
{
    //Tracks to see if Unity Services have been initialized
    //Static, shared globally across scenes
    public static bool IsInitialized { get; private set; }
    private static UnityServicesBootstrap instance;

    private async void Awake()
    {
        //Prevent duplicate bootstrap objects
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        //Prevent GameObject from being destroyed when loading new scene
        DontDestroyOnLoad(gameObject);

        if (IsInitialized)
            return;
        
        try
        {
            //Initialize all unity services used by project
            await UnityServices.InitializeAsync();

            //If player is not signed in, sign in anonymously
            //Lets dev use game with account sign in
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            //Mark services are ready
            IsInitialized = true;

            //Log success and show Unity Player ID in Console
            Debug.Log($"Unity Services ready. Player ID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (System.Exception ex)
        {
            //Log any initialization/sign-in errors
            Debug.LogError("Failed to initialize Unity Services: " + ex.Message);
        }
    }
}