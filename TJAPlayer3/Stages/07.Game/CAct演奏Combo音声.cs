using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using FDK;

namespace TJAPlayer3
{
	internal class CAct演奏Combo音声 : CActivity
	{
		// コンストラクタ

		public CAct演奏Combo音声()
		{
			base.b活性化してない = true;
		}
		
		// メソッド
        public void t再生( int nCombo, int player )
        {
            if (VoiceIndex[player] < ListCombo[player].Count)
            {
                
                var index = ListCombo[player][VoiceIndex[player]];
                if (nCombo == index.nCombo)
                {
                    index.soundComboVoice.t再生を開始する();
                    VoiceIndex[player]++;
                }
                
            }
        }

        public void tPlayFloorSound()
        {
            if (FloorIndex[0] < ListFloor[0].Count)
            {

                var index = ListFloor[0][FloorIndex[0]];
                if (CFloorManagement.LastRegisteredFloor == index.nCombo)
                {
                    index.soundComboVoice.t再生を開始する();
                    FloorIndex[0]++;
                }
            }
        }

        /// <summary>
        /// カーソルを戻す。
        /// コンボが切れた時に使う。
        /// </summary>
        public void tReset(int nPlayer)
        {
            VoiceIndex[nPlayer] = 0;
        }

		// CActivity 実装

		public override void On活性化()
		{
            for (int i = 0; i < 5; i++)
            {
                ListCombo[i] = new List<CComboVoice>();
                ListFloor[i] = new List<CComboVoice>();
            }
            VoiceIndex = new int[] { 0, 0, 0, 0, 0 };
            FloorIndex = new int[] { 0, 0, 0, 0, 0 };
            base.On活性化();
		}
		public override void OnManagedリソースの作成()
		{
			if( !base.b活性化してない )
			{
                

                // フォルダ内を走査してコンボボイスをListに入れていく
                // 1P、2P コンボボイス
                for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                {
                    #region [Combo voices]

                    int _charaId = TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(i)].data.Character;

                    var currentDir = ($@"{TJAPlayer3.strEXEのあるフォルダ}Global\Characters\{TJAPlayer3.Skin.Characters_DirName[_charaId]}\Sounds\Combo\");
                    if (Directory.Exists(currentDir))
                    {
                        foreach (var item in Directory.GetFiles(currentDir))
                        {
                            var comboVoice = new CComboVoice();
                            comboVoice.bFileFound = true;
                            comboVoice.nPlayer = i;
                            comboVoice.strFilePath = item;
                            comboVoice.soundComboVoice = TJAPlayer3.Sound管理.tサウンドを生成する(item, ESoundGroup.Voice);
                            /*
                            if (TJAPlayer3.ConfigIni.nPlayerCount >= 2) //2020.05.06 Mr-Ojii 左右に出したかったから追加。
                            {
                                if (i == 0)
                                    comboVoice.soundComboVoice.n位置 = -100;
                                else
                                    comboVoice.soundComboVoice.n位置 = 100;
                            }
                            */
                            comboVoice.soundComboVoice.n位置 = TJAPlayer3.ConfigIni.nPanning[TJAPlayer3.ConfigIni.nPlayerCount - 1][i];
                            comboVoice.nCombo = int.Parse(Path.GetFileNameWithoutExtension(item));
                            ListCombo[i].Add(comboVoice);
                        }
                        if(ListCombo[i].Count > 0)
                        {
                            ListCombo[i].Sort();
                        }
                    }

                    #endregion

                    #region [Floor voices]

                    currentDir = ($@"{TJAPlayer3.strEXEのあるフォルダ}Global\Characters\{TJAPlayer3.Skin.Characters_DirName[_charaId]}\Sounds\Tower_Combo\");
                    if (Directory.Exists(currentDir))
                    {
                        foreach (var item in Directory.GetFiles(currentDir))
                        {
                            var comboVoice = new CComboVoice();
                            comboVoice.bFileFound = true;
                            comboVoice.nPlayer = i;
                            comboVoice.strFilePath = item;
                            comboVoice.soundComboVoice = TJAPlayer3.Sound管理.tサウンドを生成する(item, ESoundGroup.Voice);
                            comboVoice.nCombo = int.Parse(Path.GetFileNameWithoutExtension(item));
                            ListFloor[i].Add(comboVoice);
                        }
                        if (ListFloor[i].Count > 0)
                        {
                            ListFloor[i].Sort();
                        }
                    }

                    #endregion

                }
                base.OnManagedリソースの作成();
			}
		}
		public override void OnManagedリソースの解放()
		{
			if( !base.b活性化してない )
			{
                for (int i = 0; i < 5; i++)
                {
                    foreach (var item in ListCombo[i])
                    {
                        TJAPlayer3.Sound管理.tサウンドを破棄する(item.soundComboVoice);
                    }
                    ListCombo[i].Clear();

                    foreach (var item in ListFloor[i])
                    {
                        TJAPlayer3.Sound管理.tサウンドを破棄する(item.soundComboVoice);
                    }
                    ListFloor[i].Clear();
                }

				base.OnManagedリソースの解放();
			}
		}

		#region [ private ]
		//-----------------
        int[] VoiceIndex;
        int[] FloorIndex;

        readonly List<CComboVoice>[] ListCombo = new List<CComboVoice>[5];
        readonly List<CComboVoice>[] ListFloor = new List<CComboVoice>[5];
        //-----------------
        #endregion
    }

    public class CComboVoice : IComparable<CComboVoice>
    {
        public bool bFileFound;
        public int nCombo;
        public int nPlayer;
        public string strFilePath;
        public CSound soundComboVoice;

        public CComboVoice()
        {
            bFileFound = false;
            nCombo = 0;
            nPlayer = 0;
            strFilePath = "";
            soundComboVoice = null;
        }

        public int CompareTo( CComboVoice other )
        {
            if( this.nCombo > other.nCombo ) return 1;
            else if( this.nCombo < other.nCombo ) return -1;

            return 0;
        }
    }
}
