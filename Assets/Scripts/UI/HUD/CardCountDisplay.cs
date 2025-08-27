using TMPro;
using UnityEngine;

namespace CardWar.UI.HUD
{
    public class CardCountDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countText;

        public void UpdateCount(int count)
        {
            if (_countText != null)
                _countText.text = count.ToString();
        }
    }
}
