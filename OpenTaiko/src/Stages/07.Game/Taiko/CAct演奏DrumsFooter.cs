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
            base.IsDeActivated = true;
        }

        public override void Activate()
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

            base.Activate();
        }

        public override void DeActivate()
        {
            TJAPlayer3.t安全にDisposeする(ref Mob_Footer);

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {
            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {
            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            if (this.Mob_Footer != null)
            {
                this.Mob_Footer.t2D描画(0, TJAPlayer3.Skin.Resolution[1] - this.Mob_Footer.szテクスチャサイズ.Height);
            }
            return base.Draw();
        }

        #region[ private ]
        //-----------------
        public CTexture Mob_Footer;
        //-----------------
        #endregion
    }
}
