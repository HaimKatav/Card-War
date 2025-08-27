using CardWar.Core.Data;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace CardWar.Gameplay.Cards
{
    public class CardView : MonoBehaviour
    {
        public async UniTask FlipToFront(CardData data)
        {
            await transform.DORotate(Vector3.up * 90f, 0.15f);
            SetupCardVisuals(data);
            ShowFrontFace();
            await transform.DORotate(Vector3.up * 0f, 0.15f);
        }

        public async UniTask MoveToPosition(Vector3 target)
        {
            await transform.DOMove(target, 0.5f).SetEase(Ease.OutQuad);
        }

        public void ShowWinPile(int cardCount)
        {
            transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
        }

        private void SetupCardVisuals(CardData data) { }
        private void ShowFrontFace() { }

        public class Pool : MonoMemoryPool<CardView> { }
    }
}
