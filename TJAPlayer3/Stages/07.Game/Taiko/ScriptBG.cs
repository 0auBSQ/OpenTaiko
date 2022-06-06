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
            Textures.Add(fileName, TJAPlayer3.tテクスチャの生成($@"{DirPath}\{fileName}"));
        }
        public void DrawGraph(double x, double y, string fileName)
        {
            Textures[fileName]?.t2D描画(TJAPlayer3.app.Device, (int)x, (int)y);
        }
        public void DrawGraphCenter(double x, double y, string fileName)
        {
            Textures[fileName]?.t2D拡大率考慮中央基準描画(TJAPlayer3.app.Device, (int)x, (int)y);
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
        public void SetColor(double r, double g, double b, string fileName)
        {
            if (Textures[fileName] != null)
            {
                Textures[fileName].color4 = new SharpDX.Color4((float)r, (float)g, (float)b, 1f);
            }
        }
    }
    class ScriptBG : IDisposable
    {
        public Dictionary<string, CTexture> Textures;

        private Lua LuaScript;

        private LuaFunction LuaUpdateValues;
        private LuaFunction LuaClearIn;
        private LuaFunction LuaClearOut;
        private LuaFunction LuaInit;
        private LuaFunction LuaUpdate;
        private LuaFunction LuaDraw;

        public ScriptBG(string filePath)
        {
            Textures = new Dictionary<string, CTexture>();

            if (!File.Exists(filePath)) return;

            LuaScript = new Lua();
            LuaScript.State.Encoding = Encoding.UTF8;

            LuaScript["func"] = new ScriptBGFunc(Textures, Path.GetDirectoryName(filePath));




            LuaScript.DoFile(filePath);

            LuaUpdateValues = LuaScript.GetFunction("updateValues");
            LuaClearIn = LuaScript.GetFunction("clearIn");
            LuaClearOut = LuaScript.GetFunction("clearOut");
            LuaInit = LuaScript.GetFunction("init");
            LuaUpdate = LuaScript.GetFunction("update");
            LuaDraw = LuaScript.GetFunction("draw");
        }
        public void Dispose()
        {
            var texs = Textures.ToArray();
            for (int i = 0; i < texs.Length; i++)
            {
                TJAPlayer3.t安全にDisposeする(ref texs[i]);
            }

            Textures.Clear();

            LuaScript?.Dispose();
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
                LuaScript["playerCount"] = TJAPlayer3.ConfigIni.nPlayerCount;
                LuaScript["p1IsBlue"] = TJAPlayer3.P1IsBlue();
                //LuaScript["isClear"] = new bool[4] { false, false, false, false };
                //LuaScript["towerNightOpacity"] = 0;
                //LuaScript["towerNightOpacity"] = (double)(255 * currentFloorPositionMax140);

                LuaUpdateValues.Call(TJAPlayer3.FPS.DeltaTime, TJAPlayer3.FPS.n現在のFPS, TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared, 0);

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

                if (TJAPlayer3.stage選曲.r確定された曲.arスコア[5] != null)
                {
                    int maxFloor = TJAPlayer3.stage選曲.r確定された曲.arスコア[5].譜面情報.nTotalFloor;
                    int nightTime = Math.Max(140, maxFloor / 2);

                    currentFloorPositionMax140 = Math.Min(TJAPlayer3.stage演奏ドラム画面.actPlayInfo.NowMeasure[0] / (float)nightTime, 1f);
                }

                LuaUpdateValues.Call(TJAPlayer3.FPS.DeltaTime, TJAPlayer3.FPS.n現在のFPS, TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared, (double)currentFloorPositionMax140);
                /*LuaScript.SetObjectToPath("fps", TJAPlayer3.FPS.n現在のFPS);
                LuaScript.SetObjectToPath("deltaTime", TJAPlayer3.FPS.DeltaTime);
                LuaScript.SetObjectToPath("isClear", TJAPlayer3.stage演奏ドラム画面.bIsAlreadyCleared);
                LuaScript.SetObjectToPath("towerNightOpacity", (double)(255 * currentFloorPositionMax140));*/
                if (!TJAPlayer3.stage演奏ドラム画面.bPAUSE) LuaUpdate.Call();
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
