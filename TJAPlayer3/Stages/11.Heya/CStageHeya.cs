using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using FDK;
using SlimDX.DirectInput;
using static TJAPlayer3.CActSelect�ȃ��X�g;

namespace TJAPlayer3
{
    class CStageHeya : CStage
    {
        public CStageHeya()
        {
            base.e�X�e�[�WID = E�X�e�[�W.Heya;
            base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�ʏ���;

            base.list�qActivities.Add(this.actFOtoTitle = new CActFIFOBlack());

            base.list�qActivities.Add(this.PuchiChara = new PuchiChara());
        }

        public override void On������()
        {
            if (base.b���������Ă�)
                return;

            base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�ʏ���;
            this.e�t�F�[�h�A�E�g�������̖߂�l = E�߂�l.�p��;

            ctDonchan_In = new CCounter();
            ctDonchan_Normal = new CCounter(0, TJAPlayer3.Tx.SongSelect_Donchan_Normal.Length - 1, 1000 / 45, TJAPlayer3.Timer);


            bInSongPlayed = false;

            this.PuchiChara.IdleAnimation();

            base.On������();
        }

        public override void On�񊈐���()
        {
            base.On�񊈐���();
        }

        public override void OnManaged���\�[�X�̍쐬()
        {
            base.OnManaged���\�[�X�̍쐬();
        }

        public override void OnManaged���\�[�X�̉��()
        {
            base.OnManaged���\�[�X�̉��();
        }

        public override int On�i�s�`��()
        {
            ctDonchan_Normal.t�i�sLoop();
            ctDonchan_In.t�i�s();

            TJAPlayer3.Tx.Heya_Background.t2D�`��(TJAPlayer3.app.Device, 0, 0);

            if (!ctDonchan_In.b�J�n����)
            {
                TJAPlayer3.Skin.soundHeyaBGM.t�Đ�����();
                ctDonchan_In.t�J�n(0, 180, 1.25f, TJAPlayer3.Timer);    
            }

            TJAPlayer3.NamePlate.tNamePlateDraw(TJAPlayer3.Skin.SongSelect_NamePlate_X[0], TJAPlayer3.Skin.SongSelect_NamePlate_Y[0] + 5, 0);

            #region [ �ǂ񂿂��֘A ]

                if (ctDonchan_In.n���݂̒l != 90)
                {
                    float DonchanX = 0f, DonchanY = 0f;

                    DonchanX = (float)Math.Sin(ctDonchan_In.n���݂̒l / 2 * (Math.PI / 180)) * 200f;
                    DonchanY = ((float)Math.Sin((90 + (ctDonchan_In.n���݂̒l / 2)) * (Math.PI / 180)) * 150f);

                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n���݂̒l].Opacity = ctDonchan_In.n���݂̒l * 2;
                    TJAPlayer3.Tx.SongSelect_Donchan_Normal[ctDonchan_Normal.n���݂̒l].t2D�`��(TJAPlayer3.app.Device, -200 + DonchanX, 336 - DonchanY);

                    #region [PuchiChara]

                    this.PuchiChara.On�i�s�`��(0 + 100, 336 + 230, false);

                    #endregion
                }

            #endregion

            #region [ �L�[�֘A ]

            if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.RightArrow) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.RBlue))
            {
                //this.�i�ʃ��X�g.t�E�Ɉړ�();
            }

            if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.LeftArrow) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.LBlue))
            {
                //this.�i�ʃ��X�g.t���Ɉړ�();
            }

            if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.Return) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.LRed) ||
                TJAPlayer3.Pad.b�����ꂽ(E�y��p�[�g.DRUMS, E�p�b�h.RRed))
            {
                //this.t�i�ʂ�I������();
                TJAPlayer3.Skin.sound���艹.t�Đ�����();
                //this.�i�ʒ���I�����.ctBarIn.t�J�n(0, 255, 1, TJAPlayer3.Timer);
            }

            if (TJAPlayer3.Input�Ǘ�.Keyboard.b�L�[�������ꂽ((int)Key.Escape))
            {
                TJAPlayer3.Skin.soundHeyaBGM.t��~����();
                TJAPlayer3.Skin.sound�����.t�Đ�����();
                this.e�t�F�[�h�A�E�g�������̖߂�l = E�߂�l.�^�C�g���ɖ߂�;
                this.actFOtoTitle.t�t�F�[�h�A�E�g�J�n();
                base.e�t�F�[�YID = CStage.E�t�F�[�Y.����_�t�F�[�h�A�E�g;
                return 0;
            }

            #endregion

            switch (base.e�t�F�[�YID)
            {
                case CStage.E�t�F�[�Y.����_�t�F�[�h�A�E�g:
                    if (this.actFOtoTitle.On�i�s�`��() == 0)
                    {
                        break;
                    }
                    return (int)this.e�t�F�[�h�A�E�g�������̖߂�l;

            }

            return 0;
        }

        public enum E�߂�l : int
        {
            �p��,
            �^�C�g���ɖ߂�,
            �I�Ȃ���
        }

        public bool bInSongPlayed;

        private CCounter ctDonchan_In;
        private CCounter ctDonchan_Normal;

        private PuchiChara PuchiChara;

        public E�߂�l e�t�F�[�h�A�E�g�������̖߂�l;

        public CActFIFOBlack actFOtoTitle;
    }
}
