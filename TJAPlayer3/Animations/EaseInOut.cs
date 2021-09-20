using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3.Animations
{
    /// <summary>
    /// イーズイン・アウトを行うクラス。
    /// </summary>
    class EaseInOut : Animator
    {
        /// <summary>
        /// イーズイン・アウトを初期化します。
        /// </summary>
        /// <param name="startPoint">始点。</param>
        /// <param name="endPoint">終点。</param>
        /// <param name="timeMs">イージングにかける時間。</param>
        public EaseInOut(int startPoint, int endPoint, int timeMs) : base(0, timeMs, 1, false)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Sa = EndPoint - StartPoint;
            TimeMs = timeMs;
        }

        public override object GetAnimation()
        {
            var persent = Counter.n現在の値 / (double)TimeMs * 2.0;
            if (persent < 1)
            {
                return ((double)Sa / 2.0 * persent * persent * persent) + StartPoint;
            }
            else
            {
                persent -= 2;
                return ((double)Sa / 2.0 * ((persent * persent * persent) + 2)) + StartPoint;
            }
        }

        private readonly int StartPoint;
        private readonly int EndPoint;
        private readonly int Sa;
        private readonly int TimeMs;
    }
}
