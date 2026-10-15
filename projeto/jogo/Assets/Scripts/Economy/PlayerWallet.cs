using System;
using System.Collections.Generic;

namespace ZombieStreamer.Economy
{
    /// <summary>Estado persistente do jogador relevante para monetização. Serializável em JSON.</summary>
    [Serializable]
    public sealed class WalletState
    {
        public double coins;
        public int tickets;
        public bool noAds;
        public List<int> ownedOutfits = new List<int>();
        public List<string> processedTransactions = new List<string>();
        public List<string> oncePerAccountPurchased = new List<string>();
        public List<ClaimStamp> lastClaims = new List<ClaimStamp>();
    }

    /// <summary>Último resgate de um placement com cooldown (persistido para sobreviver a reinícios).</summary>
    [Serializable]
    public sealed class ClaimStamp
    {
        public string placementId;
        public double timeSeconds;
    }

    /// <summary>Fonte única de verdade para moedas, tickets e direitos (no-ads, roupas).</summary>
    public sealed class PlayerWallet
    {
        private readonly WalletState _state;

        public PlayerWallet(WalletState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public event Action Changed;

        public double Coins => _state.coins;
        public int Tickets => _state.tickets;
        public bool HasNoAds => _state.noAds;
        public WalletState Snapshot => _state;

        public void AddCoins(double amount)
        {
            if (amount < 0 || double.IsNaN(amount) || double.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));
            _state.coins += amount;
            Changed?.Invoke();
        }

        public bool TrySpendCoins(double amount)
        {
            if (amount < 0 || _state.coins < amount)
                return false;
            _state.coins -= amount;
            Changed?.Invoke();
            return true;
        }

        public void AddTickets(int amount)
        {
            if (amount < 0)
                throw new ArgumentOutOfRangeException(nameof(amount));
            _state.tickets += amount;
            Changed?.Invoke();
        }

        public bool TrySpendTicket()
        {
            if (_state.tickets <= 0)
                return false;
            _state.tickets--;
            Changed?.Invoke();
            return true;
        }

        public void GrantNoAds()
        {
            _state.noAds = true;
            Changed?.Invoke();
        }

        public bool OwnsOutfit(int index) => _state.ownedOutfits.Contains(index);

        public void GrantOutfit(int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!OwnsOutfit(index))
                _state.ownedOutfits.Add(index);
            Changed?.Invoke();
        }

        /// <summary>Marca uma transação como processada. Retorna false se já tinha sido (evita entrega dupla).</summary>
        public bool MarkTransactionProcessed(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId) || _state.processedTransactions.Contains(transactionId))
                return false;
            _state.processedTransactions.Add(transactionId);
            return true;
        }

        public bool TryGetLastClaim(string placementId, out double timeSeconds)
        {
            var stamp = _state.lastClaims.Find(s => s.placementId == placementId);
            timeSeconds = stamp?.timeSeconds ?? 0;
            return stamp != null;
        }

        public void SetLastClaim(string placementId, double timeSeconds)
        {
            var stamp = _state.lastClaims.Find(s => s.placementId == placementId);
            if (stamp == null)
                _state.lastClaims.Add(new ClaimStamp { placementId = placementId, timeSeconds = timeSeconds });
            else
                stamp.timeSeconds = timeSeconds;
        }

        public bool HasPurchasedOnce(string productId) => _state.oncePerAccountPurchased.Contains(productId);

        public void MarkPurchasedOnce(string productId)
        {
            if (!HasPurchasedOnce(productId))
                _state.oncePerAccountPurchased.Add(productId);
        }
    }
}
