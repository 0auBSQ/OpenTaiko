using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3.Animations
{
    /// <summary>
    /// フェードインを行うクラス。
    /// </summary>
    internal class FadeIn : Animator
    {
        /// <summary>
        /// フェードインを初期化します。
        /// </summary>
        /// <param name="timems">フェードインに掛ける秒数(ミリ秒)</param>
        public FadeIn(int timems) : base(0, timems - 1, 1, false)
        {
            TimeMs = timems;
        }

        /// <summary>
        /// フェードインの不透明度を255段階で返します。
        /// </summary>
        /// <returns>不透明度。</returns>
        public override object GetAnimation()
        {
            var opacity = base.Counter.n現在の値 * 255 / TimeMs;
            return opacity;
        }

        private readonly int TimeMs;
    }
}
