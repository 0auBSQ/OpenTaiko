using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TJAPlayer3;
using FDK;

namespace TJAPlayer3
{
    /// <summary>
    /// レーンフラッシュのクラス。
    /// </summary>
    public class LaneFlash : CActivity
    {

        public LaneFlash(ref CTexture texture, int player)
        {
            Texture = texture;
            Player = player;
            base.b活性化してない = true;
        }

        public void Start()
        {
            Counter = new CCounter(0, 100, 1, TJAPlayer3.Timer);
        }

        public override void On活性化()
        {
            Counter = new CCounter();
            base.On活性化();
        }

        public override void On非活性化()
        {
            Counter = null;
            base.On非活性化();
        }

        public override int On進行描画()
        {
            if (Texture == null || Counter == null) return base.On進行描画();
            if (!Counter.b停止中)
            {
                Counter.t進行();
                if (Counter.b終了値に達した) Counter.t停止();
                int opacity = (((150 - Counter.n現在の値) * 255) / 100);
                Texture.Opacity = opacity;
                Texture.t2D描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.nScrollFieldBGX[Player], TJAPlayer3.Skin.nScrollFieldY[Player]);
            }
            return base.On進行描画();
        }

        private CTexture Texture;
        private CCounter Counter;
        private int Player;
    }
}
