using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Minimalist menu class to use for custom menus
namespace TJAPlayer3
{
    class CStageTemplate : CStage
    {
        public CStageTemplate()
        {
            base.eステージID = Eステージ.Template;
            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;

            // Load CActivity objects here
            // base.list子Activities.Add(this.act = new CAct());

            base.list子Activities.Add(this.actFOtoTitle = new CActFIFOBlack());

        }

        public override void On活性化()
        {
            // On activation

            if (base.b活性化してる)
                return;

            base.eフェーズID = CStage.Eフェーズ.共通_通常状態;
            this.eフェードアウト完了時の戻り値 = EReturnValue.Continuation;

            

            base.On活性化();
        }

        public override void On非活性化()
        {
            // On de-activation

            base.On非活性化();
        }

        public override void OnManagedリソースの作成()
        {
            // Ressource allocation

            base.OnManagedリソースの作成();
        }

        public override void OnManagedリソースの解放()
        {
            // Ressource freeing

            base.OnManagedリソースの解放();
        }

        public override int On進行描画()
        {





            // Menu exit fade out transition
            switch (base.eフェーズID)
            {
                case CStage.Eフェーズ.共通_フェードアウト:
                    if (this.actFOtoTitle.On進行描画() == 0)
                    {
                        break;
                    }
                    return (int)this.eフェードアウト完了時の戻り値;

            }

            return 0;
        }

        #region [Private]


        public EReturnValue eフェードアウト完了時の戻り値;
        public CActFIFOBlack actFOtoTitle;

        #endregion
    }
}
