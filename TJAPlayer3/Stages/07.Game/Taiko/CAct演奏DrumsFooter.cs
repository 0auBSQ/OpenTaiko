using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using FDK;

namespace TJAPlayer3
{
    internal class CAct演奏DrumsFooter : CActivity
    {
        /// <summary>
        /// フッター
        /// </summary>
        public CAct演奏DrumsFooter()
        {
            base.b活性化してない = true;
        }

        public override void On活性化()
        {
            base.On活性化();
        }

        public override void On非活性化()
        {
            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            if (TJAPlayer3.Tx.Mob_Footer != null)
            {
                TJAPlayer3.Tx.Mob_Footer.t2D描画(TJAPlayer3.app.Device, 0, 720 - TJAPlayer3.Tx.Mob_Footer.szテクスチャサイズ.Height);
            }
            return base.On進行描画();
        }

        #region[ private ]
        //-----------------
        //-----------------
        #endregion
    }
}
