using FDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class CActSelectSongInfo : CStage
    {
        public CActSelectSongInfo()
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
            if (TJAPlayer3.stage選曲.r現在選択中の曲 != null && TJAPlayer3.stage選曲.r現在選択中の曲.eノード種別 == C曲リストノード.Eノード種別.SCORE)
            {
                int[] bpms = new int[3] {
                        (int)TJAPlayer3.stage選曲.r現在選択中の曲.arスコア[TJAPlayer3.stage選曲.act曲リスト.tFetchDifficulty(TJAPlayer3.stage選曲.r現在選択中の曲)].譜面情報.BaseBpm,
                        (int)TJAPlayer3.stage選曲.r現在選択中の曲.arスコア[TJAPlayer3.stage選曲.act曲リスト.tFetchDifficulty(TJAPlayer3.stage選曲.r現在選択中の曲)].譜面情報.MinBpm,
                        (int)TJAPlayer3.stage選曲.r現在選択中の曲.arスコア[TJAPlayer3.stage選曲.act曲リスト.tFetchDifficulty(TJAPlayer3.stage選曲.r現在選択中の曲)].譜面情報.MaxBpm
                    };
                for (int i = 0; i < 3; i++)
                {
                    tBPMNumberDraw(TJAPlayer3.Skin.SongSelect_Bpm_X[i], TJAPlayer3.Skin.SongSelect_Bpm_Y[i], bpms[i]);
                }

                if (TJAPlayer3.stage選曲.act曲リスト.ttkSelectedSongMaker != null && TJAPlayer3.Skin.SongSelect_Maker_Show)
                {
                    TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(TJAPlayer3.stage選曲.act曲リスト.ttkSelectedSongMaker).t2D拡大率考慮描画(CTexture.RefPnt.Left, TJAPlayer3.Skin.SongSelect_Maker[0], TJAPlayer3.Skin.SongSelect_Maker[1]);
                }
                if (TJAPlayer3.stage選曲.act曲リスト.ttkSelectedSongBPM != null && TJAPlayer3.Skin.SongSelect_BPM_Text_Show)
                {
                    TJAPlayer3.stage選曲.act曲リスト.ResolveTitleTexture(TJAPlayer3.stage選曲.act曲リスト.ttkSelectedSongBPM).t2D拡大率考慮描画(CTexture.RefPnt.Left, TJAPlayer3.Skin.SongSelect_BPM_Text[0], TJAPlayer3.Skin.SongSelect_BPM_Text[1]);
                }
                if (TJAPlayer3.stage選曲.r現在選択中の曲.bExplicit == true)
                    TJAPlayer3.Tx.SongSelect_Explicit?.t2D描画(TJAPlayer3.Skin.SongSelect_Explicit[0], TJAPlayer3.Skin.SongSelect_Explicit[1]);
            }


            return 0;
        }

        #region [Private]

        private void tBPMNumberDraw(float originx, float originy, int num)
        {
            int[] nums = CConversion.SeparateDigits(num);

            for (int j = 0; j < nums.Length; j++)
            {
                if (TJAPlayer3.Skin.SongSelect_Bpm_Show && TJAPlayer3.Tx.SongSelect_Bpm_Number != null)
                {
                    float offset = j;
                    float x = originx - (TJAPlayer3.Skin.SongSelect_Bpm_Interval[0] * offset);
                    float y = originy - (TJAPlayer3.Skin.SongSelect_Bpm_Interval[1] * offset);

                    float width = TJAPlayer3.Tx.SongSelect_Bpm_Number.sz画像サイズ.Width / 10.0f;
                    float height = TJAPlayer3.Tx.SongSelect_Bpm_Number.sz画像サイズ.Height;

                    TJAPlayer3.Tx.SongSelect_Bpm_Number.t2D描画(x, y, new RectangleF(width * nums[j], 0, width, height));
                }
            }
        }

        #endregion
    }
}
