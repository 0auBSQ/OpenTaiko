﻿using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class CActSelectTowerInfo : CStage
    {
        public CActSelectTowerInfo()
        {
            base.IsDeActivated = true;
        }

        public override void Activate()
        {
            // On activation

            if (base.IsActivated)
                return;



            base.Activate();
        }

        public override void DeActivate()
        {
            // On de-activation

            base.DeActivate();
        }

        public override void CreateManagedResource()
        {

            // Ressource allocation

            base.CreateManagedResource();
        }

        public override void ReleaseManagedResource()
        {

            // Ressource freeing

            base.ReleaseManagedResource();
        }

        public override int Draw()
        {
            tFloorNumberDraw(TJAPlayer3.Skin.SongSelect_FloorNum_X, TJAPlayer3.Skin.SongSelect_FloorNum_Y, TJAPlayer3.stage選曲.r現在選択中の曲.nTotalFloor);

            return 0;
        }

        #region [Private]

        private void tFloorNumberDraw(float originx, float originy, int num)
        {
            int[] nums = CConversion.SeparateDigits(num);

            for (int j = 0; j < nums.Length; j++)
            {
                if (TJAPlayer3.Skin.SongSelect_FloorNum_Show && TJAPlayer3.Tx.SongSelect_Floor_Number != null)
                {
                    float offset = j;
                    float x = originx - (TJAPlayer3.Skin.SongSelect_FloorNum_Interval[0] * offset);
                    float y = originy - (TJAPlayer3.Skin.SongSelect_FloorNum_Interval[1] * offset);

                    float width = TJAPlayer3.Tx.SongSelect_Floor_Number.sz画像サイズ.Width / 10.0f;
                    float height = TJAPlayer3.Tx.SongSelect_Floor_Number.sz画像サイズ.Height;

                    TJAPlayer3.Tx.SongSelect_Floor_Number.t2D描画(x, y, new RectangleF(width * nums[j], 0, width, height));
                }
            }
        }

        #endregion
    }
}
