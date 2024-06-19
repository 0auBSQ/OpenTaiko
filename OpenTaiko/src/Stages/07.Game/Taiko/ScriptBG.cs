using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLua;
using FDK;

namespace TJAPlayer3
{
    class ScriptBGFunc
    {
        private Dictionary<string, CTexture> Textures;
        private string DirPath;

        public ScriptBGFunc(Dictionary<string, CTexture> texs, string dirPath)
        {
            Textures = texs;
            DirPath = dirPath;
        }
        public void DrawText(double x, double y, string text)
        {
            TJAPlayer3.actTextConsole.tPrint((int)x, (int)y, CTextConsole.EFontType.White, text);
        }
        public void DrawNum(double x, double y, double text)
        {
            TJAPlayer3.actTextConsole.tPrint((int)x, (int)y, CTextConsole.EFontType.White, text.ToString());
        }
        public void AddGraph(string fileName)
        {
            string trueFileName = fileName.Replace('/', Path.DirectorySeparatorChar);
            trueFileName = trueFileName.Replace('\\', Path.DirectorySeparatorChar);
            Textures.Add(fileName, TJAPlayer3.tテクスチャの生成($@"{DirPath}{Path.DirectorySeparatorChar}{trueFileName}"));
        }
        public void DrawGraph(double x, double y, string fileName)
        {
            Textures[fileName]?.t2D描画((int)x, (int)y);
        }
        public void DrawRectGraph(double x, double y, int rect_x, int rect_y, int rect_width, int rect_height, string fileName)
        {
            Textures[fileName]?.t2D描画((int)x, (int)y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
        }
        public void DrawGraphCenter(double x, double y, string fileName)
        {
            Textures[fileName]?.t2D拡大率考慮中央基準描画((int)x, (int)y);
        }
        public void DrawGraphRectCenter(double x, double y, int rect_x, int rect_y, int rect_width, int rect_height, string fileName)
        {
            Textures[fileName]?.t2D拡大率考慮中央基準描画((int)x, (int)y, new System.Drawing.RectangleF(rect_x, rect_y, rect_width, rect_height));
        }
        public void SetOpacity(double opacity, string fileName)
        {
            if (Textures[fileName] != null)
                Textures[fileName].Opacity = (int)opacity;
        }
        public void SetScale(double xscale, double yscale, string fileName)
        {
            if (Textures[fileName] != null)
            {
                Textures[fileName].vcScaleRatio.X = (float)xscale;
                Textures[fileName].vcScaleRatio.Y = (float)yscale;
            }
        }
        public void SetRotation(double angle, string fileName)
        {
            if (Textures[fileName] != null)
            {
                Textures[fileName].fZ軸中心回転 = (float)(angle * Math.PI / 180);
            }
        }
        public void SetColor(double r, double g, double b, string fileName)
        {
            if (Textures[fileName] != null)
            {
                Textures[fileName].color4 = new Color4((float)r, (float)g, (float)b, 1f);
            }
        }
        public void SetBlendMode(string type, string fileName)
        {
            if (Textures[fileName] != null)
            {
                switch(type)
                {
                    case "Normal":
                    default:
                    Textures[fileName].b加算合成 = false;
                    Textures[fileName].b乗算合成 = false;
                    Textures[fileName].b減算合成 = false;
                    Textures[fileName].bスクリーン合成 = false;
                    break;
                    case "Add":
                    Textures[fileName].b加算合成 = true;
                    Textures[fileName].b乗算合成 = false;
                    Textures[fileName].b減算合成 = false;
                    Textures[fileName].bスクリーン合成 = false;
                    break;
                    case "Multi":
                    Textures[fileName].b加算合成 = false;
                    Textures[fileName].b乗算合成 = true;
                    Textures[fileName].b減算合成 = false;
                    Textures[fileName].bスクリーン合成 = false;
                    break;
                    case "Sub":
                    Textures[fileName].b加算合成 = false;
                    Textures[fileName].b乗算合成 = false;
                    Textures[fileName].b減算合成 = true;
                    Textures[fileName].bスクリーン合成 = false;
                    break;
                    case "Screen":
                    Textures[fileName].b加算合成 = false;
                    Textures[fileName].b乗算合成 = false;
                    Textures[fileName].b減算合成 = false;
                    Textures[fileName].bスクリーン合成 = true;
                    break;
                }
            }
        }
        
        public double GetTextureWidth(string fileName)
        {
            if (Textures[fileName] != null)
            {
                return Textures[fileName].szTextureSize.Width;
            }
            return -1;
        }
        
        public double GetTextureHeight(string fileName)
        {
            if (Textures[fileName] != null)
            {
                return Textures[fileName].szTextureSize.Height;
            }
            return -1;
        }
    }
    class ScriptBG : IDisposable
    {
        public Dictionary<string, CTexture> Textures;

        protected Lua LuaScript;

        protected LuaFunction LuaSetConstValues;
        protected LuaFunction LuaUpdateValues;
        protected LuaFunction LuaClearIn;
        protected LuaFunction LuaClearOut;
        protected LuaFunction LuaInit;
        protected LuaFunction LuaUpdate;
        protected LuaFunction LuaDraw;

        public ScriptBG(string filePath)
        {
            Textures = new Dictionary<string, CTexture>();

            if (!File.Exists(filePath)) return;

            LuaScript = new Lua();
            LuaScript.State.Encoding = Encoding.UTF8;

            LuaScript["func"] = new ScriptBGFunc(Textures, Path.GetDirectoryName(filePath));


            try
            {
                using (var streamAPI = new StreamReader("BGScriptAPI.lua", Encoding.UTF8))
                {
                    using (var stream = new StreamReader(filePath, Encoding.UTF8))
                    {
                        var text = $"{streamAPI.ReadToEnd()}\n{stream.ReadToEnd()}";
                        LuaScript.DoString(text);
                    }
                }

                LuaSetConstValues = LuaScript.GetFunction("setConstValues");
                LuaUpdateValues = LuaScript.GetFunction("updateValues");
                LuaClearIn = LuaScript.GetFunction("clearIn");
                LuaClearOut = LuaScript.GetFunction("clearOut");
                LuaInit = LuaScript.GetFunction("init");
                LuaUpdate = LuaScript.GetFunction("update");
                LuaDraw = LuaScript.GetFunction("draw");
            }
            catch (Exception ex)
            {
                LuaScript.Dispose();
                LuaScript = null;
            }
        }
        public bool Exists()
        {
            return LuaScript != null;
        }
        public void Dispose()
        {
            List<CTexture> texs = new List<CTexture>();
            foreach(var tex in Textures.Values)
            {
                texs.Add(tex);
            }
            for (int i = 0; i < texs.Count; i++)
            {
                var tex = texs[i];
                TJAPlayer3.tテクスチャの解放(ref tex);
            }

            Textures.Clear();

            LuaScript?.Dispose();

            LuaSetConstValues?.Dispose();
            LuaUpdateValues?.Dispose();
            LuaClearIn?.Dispose();
            LuaClearOut?.Dispose();
            LuaInit?.Dispose();
            LuaUpdate?.Dispose();
            LuaDraw?.Dispose();
        }

        public void ClearIn(int player)
        {
            if (LuaScript == null) return;
            try
            {
                LuaClearIn.Call(player);
            }
            catch (Exception ex)
            {
                LuaScript.Dispose();
                LuaScript = null;
            }
        }
        public void ClearOut(int player)
        {
            if (LuaScript == null) return;
            try
            {
                LuaClearOut.Call(player);
            }
            catch (Exception ex)
            {
                LuaScript.Dispose();
                LuaScript = null;
            }
        }
        public void Init()
        {
            if (LuaScript == null) return;
            try
            {
                // Preprocessing
                string[] raritiesP = { "Common", "Common", "Common", "Common", "Common" };
                string[] raritiesC = { "Common", "Common", "Common", "Common", "Common" };

                if (TJAPlayer3.Tx.Puchichara != null && TJAPlayer3.Tx.Characters != null)
                {
                    for (int i = 0; i < TJAPlayer3.ConfigIni.nPlayerCount; i++)
                    {
                        raritiesP[i] = TJAPlayer3.Tx.Puchichara[PuchiChara.tGetPuchiCharaIndexByName(TJAPlayer3.GetActualPlayer(i))].metadata.Rarity;
                        raritiesC[i] = TJAPlayer3.Tx.Characters[TJAPlayer3.SaveFileInstances[TJAPlayer3.GetActualPlayer(i)].data.Character].metadata.Rarity;
                    }
                }

                // Initialisation
                LuaSetConstValues.Call(TJAPlayer3.ConfigIni.nPlayerCount, 
                    TJAPlayer3.P1IsBlue(), 
                    TJAPlayer3.ConfigIni.sLang, 
                    TJAPlayer3.ConfigIni.SimpleMode,
                    raritiesP,
                    raritiesC
                    );

                LuaUpdateValues.Call(TJAPlayer3.FPS.DeltaTime,
                    TJAPlayer3.FPS.NowFPS,
                    TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared,
                    0,
                    TJAPlayer3.stage演奏ドラム画面.AIBattleState,
                    TJAPlayer3.stage演奏ドラム画面.bIsAIBattleWin,
                    TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値,
                    TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM,
                    new bool[] { false, false, false, false, false },
                    -1
                    );

                LuaInit.Call();
            }
            catch (Exception ex)
            {
                LuaScript.Dispose();
                LuaScript = null;
            }
        }
        public void Update()
        {
            if (LuaScript == null) return;
            try
            {
                float currentFloorPositionMax140 = 0;

                if (TJAPlayer3.stageSongSelect.rChoosenSong != null && TJAPlayer3.stageSongSelect.rChoosenSong.arスコア[5] != null)
                {
                    int maxFloor = TJAPlayer3.stageSongSelect.rChoosenSong.arスコア[5].譜面情報.nTotalFloor;
                    int nightTime = Math.Max(140, maxFloor / 2);

                    currentFloorPositionMax140 = Math.Min(TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);
                }
                double timestamp = -1.0;

                if (TJAPlayer3.DTX != null)
                {
                    double timeoffset = TJAPlayer3.stageSongSelect.nChoosenSongDifficulty[0] != (int)Difficulty.Dan ? -2.0 : -8.2;
                    // Due to the fact that all Dans use DELAY to offset instead of OFFSET, Dan offset can't be properly synced. ¯\_(ツ)_/¯

                    timestamp = (((double)(SoundManager.PlayTimer.NowTime * TJAPlayer3.ConfigIni.SongPlaybackSpeed)) / 1000.0) +
                            (-(TJAPlayer3.ConfigIni.MusicPreTimeMs + TJAPlayer3.DTX.nOFFSET) / 1000.0) +
                            timeoffset;
                }

                LuaUpdateValues.Call(TJAPlayer3.FPS.DeltaTime, 
                    TJAPlayer3.FPS.NowFPS, 
                    TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared, 
                    (double)currentFloorPositionMax140, 
                    TJAPlayer3.stage演奏ドラム画面.AIBattleState,
                    TJAPlayer3.stage演奏ドラム画面.bIsAIBattleWin,
                    TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値,
                    TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM,
                    TJAPlayer3.stage演奏ドラム画面.bIsGOGOTIME,
                    timestamp);
                /*LuaScript.SetObjectToPath("fps", TJAPlayer3.FPS.n現在のFPS);
                LuaScript.SetObjectToPath("deltaTime", TJAPlayer3.FPS.DeltaTime);
                LuaScript.SetObjectToPath("isClear", TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared);
                LuaScript.SetObjectToPath("towerNightOpacity", (double)(255 * currentFloorPositionMax140));*/
                LuaUpdate.Call();
            }
            catch (Exception ex)
            {
                LuaScript.Dispose();
                LuaScript = null;
            }
        }
        public void Draw()
        {
            if (LuaScript == null) return;
            try
            {
                LuaDraw.Call();
            }
            catch (Exception ex)
            {
                LuaScript.Dispose();
                LuaScript = null;
            }
        }
    }
}
