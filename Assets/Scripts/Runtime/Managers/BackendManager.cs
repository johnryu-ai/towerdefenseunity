using System;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace TDF.Runtime.Managers
{
    public class BackendManager : MonoBehaviour
    {
        public static BackendManager Instance { get; private set; }

        public bool IsInitialized { get; private set; } = false;
        public bool IsSignedIn => IsInitialized && AuthenticationService.Instance.IsSignedIn;
        public string PlayerId => IsInitialized ? AuthenticationService.Instance.PlayerId : null;

        public event Action OnSignInSuccess;
        public event Action<string> OnSignInFailed;
        public event Action OnSignOut;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // 확실하게 루트로 올려서 DontDestroyOnLoad가 작동하게 보장
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private async void Start()
        {
            await InitializeUnityServicesAsync();
        }

        private async Awaitable InitializeUnityServicesAsync()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    string profile = PlayerPrefs.GetString("TDF_AuthProfile", "default");
                    var options = new InitializationOptions();
                    options.SetProfile(profile);
                    await UnityServices.InitializeAsync(options);
                }

                SetupAuthenticationEvents();
                IsInitialized = true;
                Debug.Log("[BackendManager] Unity Services Initialized.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[BackendManager] Failed to initialize Unity Services: {e.Message}");
            }
        }

        public async Awaitable WaitUntilInitializedAsync()
        {
            while (!IsInitialized)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        public async Awaitable<bool> TrySignInCachedUserAsync()
        {
            if (!IsInitialized) return false;
            if (IsSignedIn) return true;

            string profile = PlayerPrefs.GetString("TDF_AuthProfile", "default");
            AuthenticationService.Instance.SwitchProfile(profile);

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                Debug.Log($"[BackendManager] Session token exists for profile {profile}. Attempting recovery...");
                try
                {
                    // UGS는 이전에 로그인한 인증 정보(세션 토큰)가 기기에 남아있으면 
                    // SignInAnonymouslyAsync() 호출 시 해당 세션을 복구하여 자동 로그인합니다.
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[BackendManager] Successfully recovered session. PlayerID: {PlayerId}");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[BackendManager] Failed to recover cached session: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        private void SetupAuthenticationEvents()
        {
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"[BackendManager] Signed in! Player ID: {AuthenticationService.Instance.PlayerId}");
                OnSignInSuccess?.Invoke();
            };

            AuthenticationService.Instance.SignInFailed += (err) =>
            {
                Debug.LogError($"[BackendManager] Sign in failed: {err}");
                OnSignInFailed?.Invoke(err.ToString());
            };

            AuthenticationService.Instance.SignedOut += () =>
            {
                Debug.Log("[BackendManager] Player signed out.");
                OnSignOut?.Invoke();
            };
        }

        public async Awaitable SignInAnonymouslyAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[BackendManager] Unity Services not initialized yet.");
                return;
            }

            if (IsSignedIn)
            {
                Debug.Log("[BackendManager] Already signed in.");
                OnSignInSuccess?.Invoke();
                return;
            }

            try
            {
                Debug.Log("[BackendManager] Attempting to sign in anonymously (Guest Profile)...");
                // Guest 로그인과 타 계정 로그인이 연동되지 않도록 완전히 별개의 프로필 사용
                AuthenticationService.Instance.SwitchProfile("GuestProfile");
                PlayerPrefs.SetString("TDF_AuthProfile", "GuestProfile");
                PlayerPrefs.Save();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"[BackendManager] Auth Error: {ex.Message}");
            }
            catch (RequestFailedException ex)
            {
                Debug.LogError($"[BackendManager] Request Error: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 소셜 로그인 (Google, Apple, Facebook) 연동
        // (실제 사용을 위해서는 Google Play Games, Apple Sign-In 등의 네이티브 플러그인 연동을 통해 토큰을 받아와야 합니다)
        // ══════════════════════════════════════════════════════════════════

        public async Awaitable LinkWithGoogleAsync(string idToken)
        {
            if (!IsSignedIn) return;
            try
            {
                await AuthenticationService.Instance.LinkWithGoogleAsync(idToken);
                Debug.Log("[BackendManager] Successfully linked with Google.");
            }
            catch (AuthenticationException ex) { Debug.LogError($"[BackendManager] Google Link Error: {ex.Message}"); }
        }

        public async Awaitable SignInWithGoogleAsync(string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithGoogleAsync(idToken);
                Debug.Log("[BackendManager] Successfully signed in with Google.");
            }
            catch (AuthenticationException ex) { Debug.LogError($"[BackendManager] Google SignIn Error: {ex.Message}"); }
        }

        public async Awaitable LinkWithAppleAsync(string idToken)
        {
            if (!IsSignedIn) return;
            try
            {
                await AuthenticationService.Instance.LinkWithAppleAsync(idToken);
                Debug.Log("[BackendManager] Successfully linked with Apple.");
            }
            catch (AuthenticationException ex) { Debug.LogError($"[BackendManager] Apple Link Error: {ex.Message}"); }
        }

        public async Awaitable SignInWithAppleAsync(string idToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithAppleAsync(idToken);
                Debug.Log("[BackendManager] Successfully signed in with Apple.");
            }
            catch (AuthenticationException ex) { Debug.LogError($"[BackendManager] Apple SignIn Error: {ex.Message}"); }
        }

        public async Awaitable LinkWithFacebookAsync(string accessToken)
        {
            if (!IsSignedIn) return;
            try
            {
                await AuthenticationService.Instance.LinkWithFacebookAsync(accessToken);
                Debug.Log("[BackendManager] Successfully linked with Facebook.");
            }
            catch (AuthenticationException ex) { Debug.LogError($"[BackendManager] Facebook Link Error: {ex.Message}"); }
        }

        public async Awaitable SignInWithFacebookAsync(string accessToken)
        {
            try
            {
                await AuthenticationService.Instance.SignInWithFacebookAsync(accessToken);
                Debug.Log("[BackendManager] Successfully signed in with Facebook.");
            }
            catch (AuthenticationException ex) { Debug.LogError($"[BackendManager] Facebook SignIn Error: {ex.Message}"); }
        }

        // ══════════════════════════════════════════════════════════════════
        // Unity Player Accounts (웹 브라우저 포털을 통한 구글/애플/유니티 로그인)
        // ══════════════════════════════════════════════════════════════════
        
        public async Awaitable SignInWithUnityPlayerAccountAsync()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[BackendManager] Unity Services not initialized yet. Cannot sign in with Unity Player Account.");
                return;
            }

            try
            {
                Debug.Log("[BackendManager] Starting Unity Player Account Sign In...");

                // Unity Player Account에 로그인이 안되어 있다면 브라우저 창을 띄워 로그인 진행
                // 참고: 이 부분에서 에러가 발생한다면 Project Settings -> Services -> Unity Player Accounts 설정이 누락되었기 때문입니다.
                if (!Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.IsSignedIn)
                {
                    // 이 비동기 작업은 유저가 브라우저에서 로그인을 완료할 때까지 대기합니다.
                    await Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.StartSignInAsync();
                }

                // 성공적으로 로그인했다면 액세스 토큰 획득
                string accessToken = Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.AccessToken;
                if (!string.IsNullOrEmpty(accessToken))
                {
                    Debug.Log("[BackendManager] Unity Player Account Token received. Authenticating with UGS (PlayerAccountProfile)...");
                    
                    // 만약 다른 프로필(GuestProfile)로 이미 로그인되어 있다면 로그아웃 처리
                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        AuthenticationService.Instance.SignOut();
                    }

                    // Guest 로그인과 연동되지 않도록 별도 프로필로 스위치
                    AuthenticationService.Instance.SwitchProfile("PlayerAccountProfile");
                    PlayerPrefs.SetString("TDF_AuthProfile", "PlayerAccountProfile");
                    PlayerPrefs.Save();

                    await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
                    Debug.Log("[BackendManager] Successfully signed in to UGS via Unity Player Account.");
                    
                    // OnSignInSuccess는 SetupAuthenticationEvents()에 등록된 콜백에서 자동 호출됨.
                }
                else
                {
                    Debug.LogError("[BackendManager] Player Account login succeeded but Access Token is empty.");
                }
            }
            catch (ServicesInitializationException)
            {
                Debug.LogError("[BackendManager] Unity Player Accounts 서비스를 초기화할 수 없습니다.\n" +
                               "1. 유니티 대시보드에서 Player Accounts 기능을 Enable 해주세요.\n" +
                               "2. Edit > Project Settings > Services 메뉴에서 프로젝트가 정상적으로 연결(Link)되었는지 확인하세요.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BackendManager] Unity Player Account SignIn Error: {ex.Message}");
            }
        }

        public void SignOut()
        {
            if (IsSignedIn)
            {
                AuthenticationService.Instance.SignOut();
                AuthenticationService.Instance.ClearSessionToken(); // 세션 토큰 강제 삭제로 확실한 로그아웃
            }
            
            // Unity Player Account 로그아웃 처리
            if (Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance != null && 
                Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.IsSignedIn)
            {
                Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.SignOut();
            }
        }
    }
}
