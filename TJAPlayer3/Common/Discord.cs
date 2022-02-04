using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace TJAPlayer3
{
    public static class DiscordRpc
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public delegate void ReadyCallback();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public delegate void DisconnectedCallback(int errorCode, string message);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        public delegate void ErrorCallback(int errorCode, string message);

        public struct EventHandlers
        {
            public ReadyCallback readyCallback;
            public DisconnectedCallback disconnectedCallback;
            public ErrorCallback errorCallback;
        }

        [Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct RichPresence
        {
            public IntPtr state;
            public IntPtr details;
            public long startTimestamp;
            public long endTimestamp;
            public IntPtr largeImageKey;
            public IntPtr largeImageText;
            public IntPtr smallImageKey;
            public IntPtr smallImageText;
            public IntPtr partyId;
            public int partySize;
            public int partyMax;
            public IntPtr matchSecret;
            public IntPtr joinSecret;
            public IntPtr spectateSecret;
            public bool instance;
        }

        [DllImport("discord-rpc", EntryPoint = "Discord_Initialize", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Initialize(string applicationId, ref EventHandlers handlers, bool autoRegister, string optionalSteamId);

        [DllImport("discord-rpc", EntryPoint = "Discord_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Shutdown();

        /*
        [DllImport("discord-rpc", EntryPoint = "Discord_RunCallbacks", CallingConvention = CallingConvention.Cdecl)]
        public static extern void RunCallbacks();
        */

        [DllImport("discord-rpc", EntryPoint = "Discord_UpdatePresence", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdatePresence(ref RichPresence presence);

        /*
        [DllImport("discord-rpc", EntryPoint = "Discord_ClearPresence", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearPresence();

        [DllImport("discord-rpc", EntryPoint = "Discord_UpdateHandlers", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UpdateHandlers(ref EventHandlers handlers);
        */
    }

    public static class Discord
    {

        private static readonly List<IntPtr> _buffers = new List<IntPtr>(10);

        /// <summary>
        /// Discord Rich Presenceの初期化をします。
        /// </summary>
        /// <param name="clientId">Discord APIのクライアントID。</param>
        public static void Initialize(string clientId)
        {
            var handlers = new DiscordRpc.EventHandlers();
            handlers.readyCallback = ReadyCallback;
            handlers.disconnectedCallback += DisconnectedCallback;
            handlers.errorCallback += ErrorCallback;

            DiscordRpc.Initialize(clientId, ref handlers, true, null);

        }

        /// <summary>
        /// Discord Rich Presenceの更新をします。
        /// </summary>
        /// <param name="details">現在の説明。</param>
        /// <param name="state">現在の状態。</param>
        /// <param name="startTimeStamp">開始時間(Unix時間)</param>
        /// <param name="endTimeStamp">終了時間(Unix時間)</param>
        /// <param name="smallImageKey">小さなアイコン用キー。</param>
        /// <param name="smallImageText">小さなアイコンのツールチップに表示するテキスト。</param>
        public static void UpdatePresence(string details, string state, long startTimeStamp = 0, long endTimeStamp = 0, string smallImageKey = null, string smallImageText = null)
        {
            var presence = new DiscordRpc.RichPresence();
            presence.details = StrToPtr(details);
            presence.state = StrToPtr(state);

            if (startTimeStamp != 0) presence.startTimestamp = startTimeStamp;
            if (endTimeStamp != 0) presence.endTimestamp = endTimeStamp;
            presence.largeImageKey = StrToPtr("OpenTaiko".ToLowerInvariant());
            presence.largeImageText = StrToPtr(TJAPlayer3.AppDisplayThreePartVersion);
            if (!string.IsNullOrEmpty(smallImageKey)) presence.smallImageKey = StrToPtr(smallImageKey);
            if (!string.IsNullOrEmpty(smallImageText)) presence.smallImageText = StrToPtr(smallImageText);

            DiscordRpc.UpdatePresence(ref presence);
            FreeMem();
        }

        /// <summary>
        /// Discord Rich Presenceのシャットダウンを行います。
        /// 終了時に必ず呼び出す必要があります。
        /// </summary>
        public static void Shutdown()
        {
            DiscordRpc.Shutdown();
            Trace.TraceInformation("[Discord] Shutdowned.");
        }

        private static void ReadyCallback()
        {
            Trace.TraceInformation("[Discord] Ready.");
        }

        /// <summary>
        /// Discordとの接続が切断された場合呼び出されます。
        /// </summary>
        /// <param name="errorCode">エラーコード。</param>
        /// <param name="message">エラーメッセージ。</param>
        private static void DisconnectedCallback(int errorCode, string message)
        {
            Trace.TraceInformation("[Discord] Disconnected.");
        }

        /// <summary>
        /// Discordとの接続でエラーが発生した場合呼び出されます。
        /// </summary>
        /// <param name="errorCode">エラーコード。</param>
        /// <param name="message">エラーメッセージ。</param>
        private static void ErrorCallback(int errorCode, string message)
        {
            Trace.TraceInformation("[Discord] Error occured: {0} {1}", errorCode, message);
        }

        // string型の文字列をポインタで参照させるようにするためのメソッド。
        private static IntPtr StrToPtr(string input)
        {
            if (string.IsNullOrEmpty(input)) return IntPtr.Zero;
            var convbytecnt = Encoding.UTF8.GetByteCount(input);
            var buffer = Marshal.AllocHGlobal(convbytecnt + 1);
            for (int i = 0; i < convbytecnt + 1; i++)
            {
                Marshal.WriteByte(buffer, i, 0);
            }
            _buffers.Add(buffer);
            Marshal.Copy(Encoding.UTF8.GetBytes(input), 0, buffer, convbytecnt);
            return buffer;
        }

        internal static void FreeMem()
        {
            for (var i = _buffers.Count - 1; i >= 0; i--)
            {
                Marshal.FreeHGlobal(_buffers[i]);
                _buffers.RemoveAt(i);
            }
        }

        /// <summary>
        /// 現在のUnix時間をlong型で返します。
        /// </summary>
        /// <returns>Unix時間。</returns>
        public static long GetUnixTime()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).Ticks / 10000000;
        }
    }
}
