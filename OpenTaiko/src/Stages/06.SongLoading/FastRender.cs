using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FDK;

namespace TJAPlayer3
{
    class FastRender
    {
        public FastRender()
        {
            
        }

        public void Render()
        {
            /*for (int i = 0; i < 5; i++)
            {
                for (int k = 0; k < TJAPlayer3.Skin.Game_Dancer_Ptn; k++)
                {
                    NullCheckAndRender(ref TJAPlayer3.Tx.Dancer[i][k]);
                }
            }*/

            NullCheckAndRender(ref TJAPlayer3.Tx.Effects_GoGoSplash);
            for (int i = 0; i < TJAPlayer3.Skin.Game_Mob_Ptn; i++)
            {
                NullCheckAndRender(ref TJAPlayer3.Tx.Mob[i]);
            }

            //NullCheckAndRender(ref TJAPlayer3.Tx.PuchiChara);
            
        }

        private void NullCheckAndRender(ref CTexture tx)
        {
            if (tx == null) return;
            tx.Opacity = 0;
            tx.t2D描画(0, 0);
            tx.Opacity = 255;
        }
    }
}
