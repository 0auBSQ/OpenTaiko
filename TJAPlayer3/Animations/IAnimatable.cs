using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TJAPlayer3.Animations
{
    /// <summary>
    /// アニメーション インターフェイス。
    /// </summary>
    interface IAnimatable
    {
        /// <summary>
        /// アニメーションを開始します。
        /// </summary>
        void Start();
        /// <summary>
        /// アニメーションを停止します。
        /// </summary>
        void Stop();
        /// <summary>
        /// アニメーションをリセットします。
        /// </summary>
        void Reset();
        /// <summary>
        /// アニメーションの進行を行います。
        /// </summary>
        void Tick();
        /// <summary>
        /// アニメーションのパラメータを返します。
        /// </summary>
        /// <returns>アニメーションのパラメータを返します。</returns>
        object GetAnimation();
    }
}
