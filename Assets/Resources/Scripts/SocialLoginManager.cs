
using UnityEngine;

public class SocialLoginManager : MonoBehaviour
{
    private readonly string webClientId = "543189817157-l9aeb9kc7qgamf84tl23bus6rk25q7e8.apps.googleusercontent.com";
    private GoogleSignInConfiguration configuration;
    private Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
    private FirebaseAuth auth;
    private FirebaseUser user;

    private bool m_IsRecovery = false;
    private bool IsAskedForLogin = false;
}
