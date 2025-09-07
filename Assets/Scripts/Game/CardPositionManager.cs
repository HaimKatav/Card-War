using System.Collections.Generic;
using CardWar.Common;
using CardWar.Game.UI;
using UnityEngine;

namespace CardWar.Game.UI
{
    public class CardPositionManager
    {
        private readonly Transform _playerDeckPosition;
        private readonly Transform _opponentDeckPosition;
        private readonly Transform _playerBattlePosition;
        private readonly Transform _opponentBattlePosition;
        private readonly List<Transform> _playerWarPositions;
        private readonly List<Transform> _opponentWarPositions;
        
        private readonly Dictionary<CardView, Vector3> _cardOriginalPositions = new();
        
        public Vector3 PlayerDeckPosition => _playerDeckPosition.position;
        public Vector3 OpponentDeckPosition => _opponentDeckPosition.position;
        public Vector3 PlayerBattlePosition => _playerBattlePosition.position;
        public Vector3 OpponentBattlePosition => _opponentBattlePosition.position;
        
        public Transform PlayerDeckTransform => _playerDeckPosition;
        public Transform OpponentDeckTransform => _opponentDeckPosition;
        public Transform PlayerBattleTransform => _playerBattlePosition;
        public Transform OpponentBattleTransform => _opponentBattlePosition;
        
        public CardPositionManager(
            Transform playerDeckPos,
            Transform opponentDeckPos,
            Transform playerBattlePos,
            Transform opponentBattlePos,
            List<Transform> playerWarPos,
            List<Transform> opponentWarPos)
        {
            _playerDeckPosition = playerDeckPos;
            _opponentDeckPosition = opponentDeckPos;
            _playerBattlePosition = playerBattlePos;
            _opponentBattlePosition = opponentBattlePos;
            _playerWarPositions = playerWarPos ?? new List<Transform>();
            _opponentWarPositions = opponentWarPos ?? new List<Transform>();
            
            ValidatePositions();
        }
        
        #region Position Validation
        
        private void ValidatePositions()
        {
            if (_playerDeckPosition == null)
                Debug.LogError("[CardPositionManager] Player deck position is null");
            
            if (_opponentDeckPosition == null)
                Debug.LogError("[CardPositionManager] Opponent deck position is null");
            
            if (_playerBattlePosition == null)
                Debug.LogError("[CardPositionManager] Player battle position is null");
            
            if (_opponentBattlePosition == null)
                Debug.LogError("[CardPositionManager] Opponent battle position is null");
            
            if (_playerWarPositions.Count == 0)
                Debug.LogWarning("[CardPositionManager] No player war positions set");
            
            if (_opponentWarPositions.Count == 0)
                Debug.LogWarning("[CardPositionManager] No opponent war positions set");
        }
        
        #endregion
        
        #region War Position Management
        
        public Vector3 GetWarPosition(bool isPlayer, int index, Vector3 offset = default)
        {
            var positions = isPlayer ? _playerWarPositions : _opponentWarPositions;
            
            if (positions == null || positions.Count == 0)
            {
                Debug.LogError($"[CardPositionManager] No war positions available for {(isPlayer ? "player" : "opponent")}");
                return isPlayer ? PlayerBattlePosition : OpponentBattlePosition;
            }
            
            if (index < 0 || index >= positions.Count)
            {
                Debug.LogError($"[CardPositionManager] Invalid war position index: {index} (max: {positions.Count - 1})");
                return isPlayer ? PlayerBattlePosition : OpponentBattlePosition;
            }
            
            return positions[index].position + offset;
        }
        
        public Transform GetWarTransform(bool isPlayer, int index)
        {
            var positions = isPlayer ? _playerWarPositions : _opponentWarPositions;
            
            if (positions == null || positions.Count == 0 || index < 0 || index >= positions.Count)
            {
                return isPlayer ? _playerBattlePosition : _opponentBattlePosition;
            }
            
            return positions[index];
        }
        
        public Vector3 CalculateWarStackOffset(int warDepth, int cardIndex, float cardSpacing)
        {
            if (warDepth <= 1 || !ShouldStackWarCards())
            {
                return Vector3.zero;
            }
            
            var baseOffset = Vector3.up * cardSpacing * 0.5f;
            var stackMultiplier = (warDepth - 1) * 2;
            
            return baseOffset * stackMultiplier;
        }
        
        public bool ShouldStackWarCards()
        {
            return true;
        }
        
        public int GetMaxWarPositions(bool isPlayer)
        {
            var positions = isPlayer ? _playerWarPositions : _opponentWarPositions;
            return positions?.Count ?? 0;
        }
        
        #endregion
        
        #region Collection Position Management
        
        public Vector3 GetCollectionTargetPosition(RoundResult result)
        {
            return result == RoundResult.PlayerWins 
                ? PlayerDeckPosition 
                : OpponentDeckPosition;
        }
        
        public Vector3 GetCollectionTargetPosition(bool playerWon)
        {
            return playerWon ? PlayerDeckPosition : OpponentDeckPosition;
        }
        
        public Transform GetCollectionTargetTransform(RoundResult result)
        {
            return result == RoundResult.PlayerWins 
                ? _playerDeckPosition 
                : _opponentDeckPosition;
        }
        
        #endregion
        
        #region Card Origin Tracking
        
        public void TrackCardOrigin(CardView card, Vector3 origin)
        {
            if (card != null)
            {
                _cardOriginalPositions[card] = origin;
            }
        }
        
        public Vector3 GetCardOrigin(CardView card)
        {
            if (card != null && _cardOriginalPositions.TryGetValue(card, out var origin))
            {
                return origin;
            }
            
            return Vector3.zero;
        }
        
        public void ClearCardOrigin(CardView card)
        {
            if (card != null)
            {
                _cardOriginalPositions.Remove(card);
            }
        }
        
        public void ClearAllCardOrigins()
        {
            _cardOriginalPositions.Clear();
        }
        
        #endregion
        
        #region Card Ownership Detection
        
        public bool IsPlayerCard(CardView card)
        {
            if (card == null) return false;
            
            var cardPos = card.transform.position;
            
            foreach (var warPos in _playerWarPositions)
            {
                if (warPos != null && Vector3.Distance(cardPos, warPos.position) < 0.1f)
                {
                    return true;
                }
            }
            
            if (_playerBattlePosition != null && 
                Vector3.Distance(cardPos, _playerBattlePosition.position) < 0.1f)
            {
                return true;
            }
            
            if (_cardOriginalPositions.TryGetValue(card, out var origin))
            {
                return Vector3.Distance(origin, PlayerDeckPosition) < 0.1f;
            }
            
            return false;
        }
        
        public bool IsOpponentCard(CardView card)
        {
            return !IsPlayerCard(card);
        }
        
        public Vector3 GetCardReturnPosition(CardView card)
        {
            return IsPlayerCard(card) ? PlayerDeckPosition : OpponentDeckPosition;
        }
        
        #endregion
        
        #region Position Queries
        
        public float GetDistanceBetweenDecks()
        {
            return Vector3.Distance(PlayerDeckPosition, OpponentDeckPosition);
        }
        
        public float GetDistanceBetweenBattlePositions()
        {
            return Vector3.Distance(PlayerBattlePosition, OpponentBattlePosition);
        }
        
        public Vector3 GetCenterPosition()
        {
            return (PlayerBattlePosition + OpponentBattlePosition) * 0.5f;
        }
        
        public bool ArePositionsValid()
        {
            return _playerDeckPosition != null &&
                   _opponentDeckPosition != null &&
                   _playerBattlePosition != null &&
                   _opponentBattlePosition != null;
        }
        
        #endregion
        
        #region Cleanup
        
        public void Cleanup()
        {
            ClearAllCardOrigins();
            Debug.Log("[CardPositionManager] Cleanup complete");
        }
        
        #endregion
    }
}