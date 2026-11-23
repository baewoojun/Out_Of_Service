using UnityEngine;
using UnityEngine.InputSystem;

namespace OutOfService
{
    public class MarketInteractable : MonoBehaviour
    {
        public enum Kind { Fuse, Exit, Note, Battery }
        public Kind kind;
        public string note;
        bool used;

        public void Use()
        {
            MarketGame game = MarketGame.Instance;
            if (game == null) return;
            if (kind == Kind.Fuse)
            {
                if (used) return;
                used = true;
                game.CollectFuse();
                gameObject.SetActive(false);
            }
            else if (kind == Kind.Exit)
            {
                if (game.fuses >= 3) game.Finish();
                else game.Show($"정문 전원 부족. 퓨즈가 {3 - game.fuses}개 더 필요하다.", 4f);
            }
            else if (kind == Kind.Battery)
            {
                if (used || MarketPlayer.Instance == null) return;
                used = true;
                MarketPlayer.Instance.Recharge(0.6f);
                gameObject.SetActive(false);
            }
            else game.Show(note, 6f);
        }
    }
}

