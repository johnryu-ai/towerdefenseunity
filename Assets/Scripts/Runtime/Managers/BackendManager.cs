using System;
using System.Collections.Generic;
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
        public bool IsMigrating { get; set; } = false;

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

            string profile = PlayerPrefs.GetString("TDF_AuthProfile", "default");
            AuthenticationService.Instance.SwitchProfile(profile);

            // 마지막으로 로그인한 방식이 구글(default)인 경우에만 Unity Player Account 복구를 시도합니다.
            if (profile != "GuestProfile")
            {
                // 1차: Unity Player Account가 이미 로그인되어 있다면 바로 복구
                if (Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance != null && 
                    Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.IsSignedIn)
                {
                    Debug.Log($"[BackendManager] Player Account is already signed in. Recovering UGS session...");
                    await SignInWithUnityPlayerAccountAsync();
                    if (IsSignedIn) return true;
                }
            }

            // 2차: 로컬 세션 토큰이 남아있는지 확인 (구글 복구 실패 시 또는 게스트 계정인 경우)
            if (AuthenticationService.Instance.SessionTokenExists || profile == "GuestProfile")
            {
                Debug.Log($"[BackendManager] Attempting recovery for profile {profile}...");
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
                    Debug.LogWarning($"[BackendManager] Failed to recover cached session via AnonymouslyAsync: {ex.Message}");
                    
                    // 만약 캐시된 토큰이 외부 제공자(Google)용이라서 에러가 났다면, 브라우저/백그라운드 갱신을 통해 복구를 시도합니다.
                    if (profile == "default")
                    {
                        Debug.Log($"[BackendManager] Attempting external provider (Unity Player Account) recovery for default profile...");
                        try 
                        {
                            await SignInWithUnityPlayerAccountAsync();
                            if (IsSignedIn) return true;
                        }
                        catch (Exception innerEx)
                        {
                            Debug.LogError($"[BackendManager] External provider recovery failed: {innerEx.Message}");
                        }
                    }
                }
            }
            
            // 만약 profile이 default이고 토큰이 아예 없는 상황이라도 (최초 구글 로그인 후 재시작 시 세션이 만료된 경우)
            // 브라우저가 갑자기 뜨는 것을 방지하기 위해 여기서는 더 이상 진행하지 않습니다.
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
                
                // 기존 기기 토큰이 남아있으면 자동으로 복구되어 기존 게스트 계정을 불러옵니다.
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"[BackendManager] Auth Error: {ex.Message}");
                // 기존 세션 토큰이 모종의 이유(서버 만료, 강제 초기화 등)로 무효화된 경우
                if (ex.ErrorCode == AuthenticationErrorCodes.InvalidSessionToken || ex.Message.Contains("not valid"))
                {
                    Debug.Log("[BackendManager] Invalid session token detected. SDK has cleared it. Retrying to create a new anonymous profile...");
                    try
                    {
                        await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    }
                    catch (Exception retryEx)
                    {
                        Debug.LogError($"[BackendManager] Retry failed: {retryEx.Message}");
                    }
                }
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

                if (Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.IsSignedIn && 
                    string.IsNullOrEmpty(Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.AccessToken))
                {
                    Debug.LogWarning("[BackendManager] Player Account is signed in but AccessToken is empty. Forcing SignOut to refresh session.");
                    Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.SignOut();
                }

                if (!Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.IsSignedIn)
                {
                    await Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.StartSignInAsync();
                    
                    // [중요] Unity Auth SDK 구조상 StartSignInAsync()가 완료되어도
                    // 백그라운드에서 토큰 교환 작업이 진행 중일 수 있으므로 AccessToken이 채워질 때까지 대기합니다.
                    float timeout = 5.0f;
                    while (string.IsNullOrEmpty(Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.AccessToken) && timeout > 0)
                    {
                        await Awaitable.WaitForSecondsAsync(0.1f);
                        timeout -= 0.1f;
                    }
                }

                string accessToken = Unity.Services.Authentication.PlayerAccounts.PlayerAccountService.Instance.AccessToken;
                if (!string.IsNullOrEmpty(accessToken))
                {
                    // 1. 마이그레이션을 위한 기존 로컬(익명) 세션 데이터 확보
                    bool hasOldData = false;
                    string oldDataJson = "";
                    AuthenticationService.Instance.SwitchProfile("default");
                    
                    if (AuthenticationService.Instance.SessionTokenExists)
                    {
                        try 
                        {
                            await AuthenticationService.Instance.SignInAnonymouslyAsync();
                            var keys = new HashSet<string> { "UserSaveData" };
                            var loadedData = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.LoadAsync(keys);
                            if (loadedData.TryGetValue("UserSaveData", out var cloudJson))
                            {
                                oldDataJson = cloudJson.Value.GetAsString();
                                hasOldData = true;
                                Debug.Log("[BackendManager] Old anonymous data backed up for migration.");
                            }
                        } 
                        catch (Exception) { /* 무시 */ }
                    }

                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        AuthenticationService.Instance.SignOut();
                    }

                    // 2. 구글 연동 계정으로 로그인
                    PlayerPrefs.SetString("TDF_AuthProfile", "default");
                    PlayerPrefs.Save();
                    
                    IsMigrating = true; // UserDataManager가 데이터를 불러오지 못하도록 막음
                    
                    await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
                    Debug.Log("[BackendManager] Successfully signed in to UGS via Unity Player Account.");
                    
                    // 3. 구글 계정이 완전 깡통(신규)인 경우에만 기존 익명 데이터를 덮어씌움 (마이그레이션)
                    if (hasOldData)
                    {
                        await Awaitable.WaitForSecondsAsync(1.5f); // 클라우드 권한 전파 대기
                        try 
                        {
                            var keys = new HashSet<string> { "UserSaveData" };
                            var googleData = await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.LoadAsync(keys);
                            
                            // 구글 클라우드에 기존 세이브가 없는 경우에만 마이그레이션 진행 (다른 기기 저장본 보호)
                            if (!googleData.ContainsKey("UserSaveData"))
                            {
                                var data = new Dictionary<string, object> { { "UserSaveData", oldDataJson } };
                                await Unity.Services.CloudSave.CloudSaveService.Instance.Data.Player.SaveAsync(data);
                                Debug.Log("[BackendManager] Migrated old anonymous data to Google account successfully!");
                            }
                        }
                        catch(Exception ex)
                        {
                            Debug.LogWarning($"[BackendManager] Failed to check/migrate data: {ex.Message}");
                        }
                    }
                    
                    IsMigrating = false; // 마이그레이션 완료
                    
                    // OnSignInSuccess는 SetupAuthenticationEvents()에 등록된 콜백에서 자동 호출됨.
                }
                else
                {
                    Debug.LogWarning("[BackendManager] Player Account sign-in was canceled or failed to retrieve an Access Token.");
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
                string currentProfile = AuthenticationService.Instance.Profile;
                AuthenticationService.Instance.SignOut();
                
                // 게스트 프로필은 세션 토큰을 삭제하면 기기 연동 데이터가 영구히 소실되므로 삭제하지 않습니다.
                if (currentProfile != "GuestProfile")
                {
                    AuthenticationService.Instance.ClearSessionToken();
                }
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
