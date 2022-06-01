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
            var footerDir = CSkin.Path($"{TextureLoader.BASE}{TextureLoader.GAME}{TextureLoader.FOOTER}");

            if (System.IO.Directory.Exists(footerDir))
            {
                Random random = new Random();

                var upDirs = System.IO.Directory.GetFiles(footerDir);
                if (upDirs.Length > 0)
                {
                    var upPath = upDirs[random.Next(0, upDirs.Length)];

                    Mob_Footer = TJAPlayer3.tテクスチャの生成(upPath);
                }
            }

            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            TJAPlayer3.t安全にDisposeする(ref Mob_Footer);

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {
            if (this.Mob_Footer != null)
            {
                this.Mob_Footer.t2D描画(TJAPlayer3.app.Device, 0, 720 - this.Mob_Footer.szテクスチャサイズ.Height);
            }
            return base.On進行描画();
        }

        #region[ private ]
        //-----------------
        public CTexture Mob_Footer;
        //-----------------
        #endregion
    }
}
