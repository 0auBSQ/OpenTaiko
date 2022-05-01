using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using SharpDX;
using FDK;

using Rectangle = System.Drawing.Rectangle;

namespace TJAPlayer3
{
    internal class CAct演奏Drumsチップエフェクト : CActivity
    {
        // コンストラクタ

        public CAct演奏Drumsチップエフェクト()
        {
            //base.b活性化してない = true;
        }


        // メソッド
        public virtual void Start(int nPlayer, int Lane)
        {
            if (TJAPlayer3.Tx.Gauge_Soul_Explosion != null)
            {
                for (int i = 0; i < 128; i++)
                {
                    if (!st[i].b使用中)
                    {
                        st[i].b使用中 = true;
                        st[i].ct進行 = new CCounter(0, TJAPlayer3.Skin.Game_Effect_NotesFlash[2], TJAPlayer3.Skin.Game_Effect_NotesFlash_Timer, TJAPlayer3.Timer);
                        st[i].ctChipEffect = new CCounter(0, 24, 17, TJAPlayer3.Timer);
                        st[i].nプレイヤー = nPlayer;
                        st[i].Lane = Lane;
                        break;
                    }
                }
            }
        }

        // CActivity 実装

        public override void On活性化()
        {
            for (int i = 0; i < 128; i++)
            {
                st[i] = new STチップエフェクト
                {
                    b使用中 = false,
                    ct進行 = new CCounter(),
                    ctChipEffect = new CCounter()
                };
            }
            base.On活性化();
        }
        public override void On非活性化()
        {
            for (int i = 0; i < 128; i++)
            {
                st[i].ct進行 = null;
                st[i].ctChipEffect = null;
                st[i].b使用中 = false;
            }
            base.On非活性化();
        }
        public override int On進行描画()
        {
            for (int i = 0; i < 128; i++)
            {
                if (st[i].b使用中)
                {
                    st[i].ct進行.t進行();
                    st[i].ctChipEffect.t進行();
                    if (st[i].ct進行.b終了値に達した)
                    {
                        st[i].ct進行.t停止();
                        st[i].b使用中 = false;
                    }
                    switch (st[i].nプレイヤー)
                    {
                        case 0:
                            TJAPlayer3.Tx.Gauge_Soul_Explosion[TJAPlayer3.P1IsBlue() ? 1 : 0]?.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[0], TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[0], new Rectangle(st[i].ct進行.n現在の値 * TJAPlayer3.Skin.Game_Effect_NotesFlash[0], 0, TJAPlayer3.Skin.Game_Effect_NotesFlash[0], TJAPlayer3.Skin.Game_Effect_NotesFlash[1]));
                            if (this.st[i].ctChipEffect.n現在の値 < 13)
                                TJAPlayer3.Tx.Notes.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[0], TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[0], new Rectangle(st[i].Lane * 130, 390, 130, 130));
                            break;

                        case 1:
                            TJAPlayer3.Tx.Gauge_Soul_Explosion[1]?.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[1], TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[1], new Rectangle(st[i].ct進行.n現在の値 * TJAPlayer3.Skin.Game_Effect_NotesFlash[0], 0, TJAPlayer3.Skin.Game_Effect_NotesFlash[0], TJAPlayer3.Skin.Game_Effect_NotesFlash[1]));
                            if (this.st[i].ctChipEffect.n現在の値 < 13)
                                TJAPlayer3.Tx.Notes.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[1], TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[1], new Rectangle(st[i].Lane * 130, 390, 130, 130));
                            break;
                    }

                    if (TJAPlayer3.Tx.ChipEffect != null)
                    {
                        if (this.st[i].ctChipEffect.n現在の値 < 12)
                        {
                            TJAPlayer3.Tx.ChipEffect.color4 = new Color4(1.0f, 1.0f, 0.0f, 1.0f);
                            TJAPlayer3.Tx.ChipEffect.Opacity = (int)(this.st[i].ctChipEffect.n現在の値 * (float)(225 / 11));
                            TJAPlayer3.Tx.ChipEffect.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[st[i].nプレイヤー], TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[st[i].nプレイヤー], new Rectangle(st[i].Lane * 130, 0, 130, 130));
                        }
                        if (this.st[i].ctChipEffect.n現在の値 > 12 && this.st[i].ctChipEffect.n現在の値 < 24)
                        {
                            TJAPlayer3.Tx.ChipEffect.color4 = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
                            TJAPlayer3.Tx.ChipEffect.Opacity = 255 - (int)((this.st[i].ctChipEffect.n現在の値 - 10) * (float)(255 / 14));
                            TJAPlayer3.Tx.ChipEffect.t2D中心基準描画(TJAPlayer3.app.Device, TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_X[st[i].nプレイヤー], TJAPlayer3.Skin.Game_Effect_FlyingNotes_EndPoint_Y[st[i].nプレイヤー], new Rectangle(st[i].Lane * 130, 0, 130, 130));
                        }
                    }
                    
                }
            }
            return 0;
        }


        // その他

        #region [ private ]
        //-----------------
        //private CTexture[] txChara;

        [StructLayout(LayoutKind.Sequential)]
        private struct STチップエフェクト
        {
            public bool b使用中;
            public CCounter ct進行;
            public CCounter ctChipEffect;
            public int nプレイヤー;
            public int Lane;
        }
        private STチップエフェクト[] st = new STチップエフェクト[128];

        //-----------------
        #endregion
    }
}
