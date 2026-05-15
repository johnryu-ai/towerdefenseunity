using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Economy;
using Unity.Services.Economy.Model;

namespace TDF.Runtime.Managers
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }

        public const string CURRENCY_GEMS_ID = "GEMS";

        public long CurrentGems { get; private set; } = 0;

        public event Action OnBalancesUpdated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (BackendManager.Instance != null)
            {
                BackendManager.Instance.OnSignInSuccess += HandleSignInSuccess;
            }
        }

        private async void HandleSignInSuccess()
        {
            await RefreshBalancesAsync();
        }

        public async Awaitable RefreshBalancesAsync()
        {
            if (BackendManager.Instance == null || !BackendManager.Instance.IsSignedIn)
            {
                Debug.Log("[EconomyManager] Not signed in. Skipping balance refresh (offline mode).");
                return;
            }

            try
            {
                await EconomyService.Instance.Configuration.SyncConfigurationAsync();
                GetBalancesResult balancesResult = await EconomyService.Instance.PlayerBalances.GetBalancesAsync();

                CurrentGems = 0;

                foreach (PlayerBalance balance in balancesResult.Balances)
                {
                    if (balance.CurrencyId == CURRENCY_GEMS_ID)
                    {
                        CurrentGems = balance.Balance;
                    }
                }

                Debug.Log($"[EconomyManager] Balances Refreshed: Gems={CurrentGems}");
                OnBalancesUpdated?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[EconomyManager] Failed to get balances: {e.Message}");
            }
        }

        public async Awaitable<bool> SpendCurrencyAsync(string currencyId, int amount)
        {
            if (amount <= 0) return true;

            if (BackendManager.Instance == null || !BackendManager.Instance.IsSignedIn)
            {
                Debug.Log($"[EconomyManager] Skipped spending {amount} {currencyId} (offline mode).");
                return true; // 에디터 오프라인 테스트 시에는 성공으로 간주
            }

            try
            {
                PlayerBalance balance = await EconomyService.Instance.PlayerBalances.DecrementBalanceAsync(currencyId, amount);
                
                if (currencyId == CURRENCY_GEMS_ID) CurrentGems = balance.Balance;

                OnBalancesUpdated?.Invoke();
                return true;
            }
            catch (EconomyException e)
            {
                Debug.LogWarning($"[EconomyManager] Failed to spend {amount} {currencyId}. Exception: {e.Message}");
                return false;
            }
        }

        public async Awaitable<bool> AddCurrencyAsync(string currencyId, int amount)
        {
            if (amount <= 0) return true;

            if (BackendManager.Instance == null || !BackendManager.Instance.IsSignedIn)
            {
                Debug.Log($"[EconomyManager] Skipped adding {amount} {currencyId} (offline mode).");
                return true; // 에디터 오프라인 테스트 시에는 성공으로 간주
            }

            try
            {
                 PlayerBalance balance = await EconomyService.Instance.PlayerBalances.IncrementBalanceAsync(currencyId, amount);
                 
                 if (currencyId == CURRENCY_GEMS_ID) CurrentGems = balance.Balance;

                 OnBalancesUpdated?.Invoke();
                 return true;
            }
            catch (EconomyException e)
            {
                 Debug.LogError($"[EconomyManager] Failed to add {amount} {currencyId}. Exception: {e.Message}");
                 return false;
            }
        }
    }
}
