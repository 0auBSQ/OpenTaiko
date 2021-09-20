using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3.Animations
{
    /// <summary>
    /// リニア移動を行うクラス。
    /// </summary>
    class Linear : Animator
    {
        /// <summary>
        /// リニア移動を初期化します。
        /// </summary>
        /// <param name="startPoint">始点。</param>
        /// <param name="endPoint">終点。</param>
        /// <param name="timeMs">移動にかける時間。</param>
        public Linear(int startPoint, int endPoint, int timeMs) : base(0, timeMs, 1, false)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Sa = EndPoint - StartPoint;
            TimeMs = timeMs;
        }

        public override object GetAnimation()
        {
            var persent = Counter.n現在の値 / (double)TimeMs;
            return (Sa * persent) + StartPoint;
        }

        private readonly int StartPoint;
        private readonly int EndPoint;
        private readonly int Sa;
        private readonly int TimeMs;
    }
}
