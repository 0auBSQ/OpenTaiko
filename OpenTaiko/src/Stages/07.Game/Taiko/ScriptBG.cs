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
            TJAPlayer3.act文字コンソール.tPrint((int)x, (int)y, C文字コンソール.Eフォント種別.白, text);
        }
        public void DrawNum(double x, double y, double text)
        {
            TJAPlayer3.act文字コンソール.tPrint((int)x, (int)y, C文字コンソール.Eフォント種別.白, text.ToString());
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
                Textures[fileName].vc拡大縮小倍率.X = (float)xscale;
                Textures[fileName].vc拡大縮小倍率.Y = (float)yscale;
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
                LuaSetConstValues.Call(TJAPlayer3.ConfigIni.nPlayerCount, TJAPlayer3.P1IsBlue(), TJAPlayer3.ConfigIni.sLang);
                LuaUpdateValues.Call(TJAPlayer3.FPS.DeltaTime,
                    TJAPlayer3.FPS.NowFPS,
                    TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared,
                    0,
                    TJAPlayer3.stage演奏ドラム画面.AIBattleState,
                    TJAPlayer3.stage演奏ドラム画面.bIsAIBattleWin,
                    TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値,
                    TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM);

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

                if (TJAPlayer3.stage選曲.r確定された曲 != null && TJAPlayer3.stage選曲.r確定された曲.arスコア[5] != null)
                {
                    int maxFloor = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;
                    int nightTime = Math.Max(140, maxFloor / 2);

                    currentFloorPositionMax140 = Math.Min(TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);
                }

                LuaUpdateValues.Call(TJAPlayer3.FPS.DeltaTime, 
                    TJAPlayer3.FPS.NowFPS, 
                    TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared, 
                    (double)currentFloorPositionMax140, 
                    TJAPlayer3.stage演奏ドラム画面.AIBattleState,
                    TJAPlayer3.stage演奏ドラム画面.bIsAIBattleWin,
                    TJAPlayer3.stage演奏ドラム画面.actGauge.db現在のゲージ値,
                    TJAPlayer3.stage演奏ドラム画面.actPlayInfo.dbBPM);
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
