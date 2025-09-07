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
        
        public Vector3 PlayerDeckPosition => _playerDeckPosition.position;
        public Vector3 OpponentDeckPosition => _opponentDeckPosition.position;
        public Vector3 PlayerBattlePosition => _playerBattlePosition.position;
        public Vector3 OpponentBattlePosition => _opponentBattlePosition.position;
        
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
            _playerWarPositions = playerWarPos;
            _opponentWarPositions = opponentWarPos;
        }
        
        public Vector3 GetWarPosition(bool isPlayer, int index, Vector3 offset = default)
        {
            var positions = isPlayer ? _playerWarPositions : _opponentWarPositions;
            
            if (index < 0 || index >= positions.Count)
            {
                Debug.LogError($"[CardPositionManager] Invalid war position index: {index}");
                return isPlayer ? PlayerBattlePosition : OpponentBattlePosition;
            }
            
            return positions[index].position + offset;
        }
        
        public Vector3 GetTargetDeckPosition(RoundResult result)
        {
            return result == RoundResult.PlayerWins ? PlayerDeckPosition : OpponentDeckPosition;
        }
        
        public bool IsPlayerCard(CardView card)
        {
            if (card == null) return false;
            
            var cardPos = card.transform.position;
            
            // Check if near player battle position
            if (Vector3.Distance(cardPos, PlayerBattlePosition) < 0.5f)
                return true;
            
            // Check if near any player war position
            foreach (var pos in _playerWarPositions)
            {
                if (pos != null && Vector3.Distance(cardPos, pos.position) < 0.5f)
                    return true;
            }
            
            // Check if near player deck
            if (Vector3.Distance(cardPos, PlayerDeckPosition) < 0.5f)
                return true;
            
            return false;
        }
        
        public List<Vector3> GetAllPlayerPositions()
        {
            var positions = new List<Vector3> { PlayerDeckPosition, PlayerBattlePosition };
            foreach (var pos in _playerWarPositions)
            {
                if (pos != null)
                    positions.Add(pos.position);
            }
            return positions;
        }
        
        public List<Vector3> GetAllOpponentPositions()
        {
            var positions = new List<Vector3> { OpponentDeckPosition, OpponentBattlePosition };
            foreach (var pos in _opponentWarPositions)
            {
                if (pos != null)
                    positions.Add(pos.position);
            }
            return positions;
        }
        
        public Vector3 CalculateWarStackOffset(int currentWarDepth, float cardSpacing)
        {
            return Vector3.up * cardSpacing * 0.5f * (currentWarDepth - 1);
        }
        
        public void ValidatePositions()
        {
            var errors = new List<string>();
            
            if (_playerDeckPosition == null) errors.Add("Player deck position");
            if (_opponentDeckPosition == null) errors.Add("Opponent deck position");
            if (_playerBattlePosition == null) errors.Add("Player battle position");
            if (_opponentBattlePosition == null) errors.Add("Opponent battle position");
            
            if (_playerWarPositions == null || _playerWarPositions.Count == 0)
                errors.Add("Player war positions");
            if (_opponentWarPositions == null || _opponentWarPositions.Count == 0)
                errors.Add("Opponent war positions");
            
            if (errors.Count > 0)
            {
                Debug.LogError($"[CardPositionManager] Missing positions: {string.Join(", ", errors)}");
            }
        }
    }
}