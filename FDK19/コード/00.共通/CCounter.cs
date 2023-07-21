using System;
using System.Collections.Generic;
using System.Text;

namespace FDK
{
    /// <summary>
    /// 一定間隔で単純増加する整数（カウント値）を扱う。
    /// </summary>
    /// <remarks>
    /// ○使い方
    /// 1.CCounterの変数をつくる。
    /// 2.CCounterを生成
    ///   ctCounter = new CCounter( 0, 3, 10, CDTXMania.Timer );
    /// 3.進行メソッドを使用する。
    /// 4.ウマー。
    ///
    /// double値を使う場合、t進行db、t進行LoopDbを使うこと。
    /// また、double版では間隔の値はミリ秒単位ではなく、通常の秒単位になります。
    /// </remarks>
    public class CCounter
    {
        public bool b開始した
        {
            get;
            set;
        }
        // 値プロパティ
        public double n開始値
        {
            get;
            private set;
        }
        public double n終了値
        {
            get;
            set;
        }
        public int n現在の値
        {
            get;
            set;
        }

        public double _n間隔
        {
            get
            {
                return this.n間隔;
            }
            set
            {
                this.n間隔 = value >= 0 ? value : value * -1;
            }
        }

        public double n現在の経過時間ms
        {
            get;
            set;
        }
        // 状態プロパティ

        public bool b進行中
        {
            get { return (this.n現在の経過時間ms != -1); }
        }
        public bool b停止中
        {
            get { return !this.b進行中; }
        }
        public bool b終了値に達した
        {
            get { return (this.n現在の値 >= this.n終了値); }
        }
        public bool b終了値に達してない
        {
            get { return !this.b終了値に達した; }
        }

        // コンストラクタ

        public CCounter()
        {
            this.timer = null;
            this.n開始値 = 0;
            this.n終了値 = 0;
            this.n現在の値 = 0;
            this.n現在の値 = 0;
            this.n現在の経過時間ms = CSoundTimer.n未使用;
        }

        /// <summary>生成と同時に開始する。</summary>
        public CCounter(double n開始値, double n終了値, double n間隔ms, CTimer timer)
            : this()
        {
            this.t開始(n開始値, n終了値, n間隔ms, timer);
        }

        /// <summary>生成と同時に開始する。(double版)</summary>
        public CCounter(double db開始値, double db終了値, double db間隔, CSoundTimer timer)
            : this()
        {
            this.t開始(db開始値, db終了値, db間隔 * 1000.0f, timer);
        }


        // 状態操作メソッド

        /// <summary>
        /// カウントを開始する。
        /// </summary>
        /// <param name="n開始値">最初のカウント値。</param>
        /// <param name="n終了値">最後のカウント値。</param>
        /// <param name="n間隔ms">カウント値を１増加させるのにかける時間（ミリ秒単位）。</param>
        /// <param name="timer">カウントに使用するタイマ。</param>
        public void t開始(double n開始値, double n終了値, double n間隔ms, CTimer timer)
        {
            this.n開始値 = n開始値;
            this.n終了値 = n終了値;
            this._n間隔 = n間隔ms;
            this.timer = timer;
            this.n現在の経過時間ms = this.timer.n現在時刻;
            this.n現在の値 = (int)n開始値;
            this.b開始した = true;
        }

        /// <summary>
        /// カウントを開始する。(double版)
        /// </summary>
        /// <param name="db開始値">最初のカウント値。</param>
        /// <param name="db終了値">最後のカウント値。</param>
        /// <param name="db間隔">カウント値を１増加させるのにかける時間（秒単位）。</param>
        /// <param name="timer">カウントに使用するタイマ。</param>
        public void t開始(double db開始値, double db終了値, double db間隔, CSoundTimer timer)
        {
            this.n開始値 = db開始値;
            this.n終了値 = db終了値;
            this._n間隔 = db間隔;
            this.timerdb = timer;
            this.n現在の経過時間ms = this.timerdb.dbシステム時刻;
            this.n現在の値 = (int)db開始値;
            this.b開始した = true;
        }

        /// <summary>
        /// 前回の t進行() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
        /// カウント値が終了値に達している場合は、それ以上増加しない（終了値を維持する）。
        /// </summary>
        public void t進行()
        {
            if ((this.timer != null) && (this.n現在の経過時間ms != CTimer.n未使用))
            {
                long num = this.timer.n現在時刻;
                if (num < this.n現在の経過時間ms)
                    this.n現在の経過時間ms = num;

                while ((num - this.n現在の経過時間ms) >= this.n間隔)
                {
                    if (++this.n現在の値 > this.n終了値)
                        this.n現在の値 = (int)this.n終了値;

                    this.n現在の経過時間ms += this.n間隔;
                }
            }
        }

        /// <summary>
        /// 前回の t進行() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
        /// カウント値が終了値に達している場合は、それ以上増加しない（終了値を維持する）。
        /// </summary>
        public void t進行db()
        {
            if ((this.timerdb != null) && (this.n現在の経過時間ms != CSoundTimer.n未使用))
            {
                double num = this.timerdb.n現在時刻;
                if (num < this.n現在の経過時間ms)
                    this.n現在の経過時間ms = num;

                while ((num - this.n現在の経過時間ms) >= this.n間隔)
                {
                    if (++this.n現在の値 > this.n終了値)
                        this.n現在の値 = (int)this.n終了値;

                    this.n現在の経過時間ms += this.n間隔;
                }
            }
        }

        /// <summary>
        /// 前回の t進行Loop() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
        /// カウント値が終了値に達している場合は、次の増加タイミングで開始値に戻る（値がループする）。
        /// </summary>
        public void t進行Loop()
        {
            if ((this.timer != null) && (this.n現在の経過時間ms != CTimer.n未使用))
            {
                long num = this.timer.n現在時刻;
                if (num < this.n現在の経過時間ms)
                    this.n現在の経過時間ms = num;

                while ((num - this.n現在の経過時間ms) >= this.n間隔)
                {
                    if (++this.n現在の値 > this.n終了値)
                        this.n現在の値 = (int)this.n開始値;

                    this.n現在の経過時間ms += this.n間隔;
                }
            }
        }

        /// <summary>
        /// 前回の t進行Loop() の呼び出しからの経過時間をもとに、必要なだけカウント値を増加させる。
        /// カウント値が終了値に達している場合は、次の増加タイミングで開始値に戻る（値がループする）。
        /// </summary>
        public void t進行LoopDb()
        {
            if ((this.timerdb != null) && (this.n現在の経過時間ms != CSoundTimer.n未使用))
            {
                double num = this.timerdb.n現在時刻;
                if (num < this.n現在の経過時間ms)
                    this.n現在の経過時間ms = num;

                while ((num - this.n現在の経過時間ms) >= this.n間隔)
                {
                    if (++this.n現在の値 > this.n終了値)
                        this.n現在の値 = (int)this.n開始値;

                    this.n現在の経過時間ms += this.n間隔;
                }
            }
        }

        /// <summary>
        /// カウントを停止する。
        /// これ以降に t進行() や t進行Loop() を呼び出しても何も処理されない。
        /// </summary>
        public void t停止()
        {
            this.n現在の経過時間ms = CTimer.n未使用;
        }

        public void t間隔値変更(double Value)
        {
            this._n間隔 = Value;
        }

        // その他

        #region [ 応用：キーの反復入力をエミュレーションする ]
        //-----------------

        /// <summary>
        /// <para>「bキー押下」引数が true の間中、「tキー処理」デリゲート引数を呼び出す。</para>
        /// <para>ただし、2回目の呼び出しは1回目から 200ms の間を開けてから行い、3回目以降の呼び出しはそれぞれ 30ms の間隔で呼び出す。</para>
        /// <para>「bキー押下」が false の場合は何もせず、呼び出し回数を 0 にリセットする。</para>
        /// </summary>
        /// <param name="bキー押下">キーが押下されている場合は true。</param>
        /// <param name="tキー処理">キーが押下されている場合に実行する処理。</param>
        public void tキー反復(bool bキー押下, DGキー処理 tキー処理)
        {
            const int n1回目 = 0;
            const int n2回目 = 1;
            const int n3回目以降 = 2;

            if (bキー押下)
            {
                switch (this.n現在の値)
                {
                    case n1回目:

                        tキー処理();
                        this.n現在の値 = n2回目;
                        this.n現在の経過時間ms = this.timer.n現在時刻;
                        return;

                    case n2回目:

                        if ((this.timer.n現在時刻 - this.n現在の経過時間ms) > 200)
                        {
                            tキー処理();
                            this.n現在の経過時間ms = this.timer.n現在時刻;
                            this.n現在の値 = n3回目以降;
                        }
                        return;

                    case n3回目以降:

                        if ((this.timer.n現在時刻 - this.n現在の経過時間ms) > 30)
                        {
                            tキー処理();
                            this.n現在の経過時間ms = this.timer.n現在時刻;
                        }
                        return;
                }
            }
            else
            {
                this.n現在の値 = n1回目;
            }
        }
        public delegate void DGキー処理();

        //-----------------
        #endregion

        #region [ private ]
        //-----------------
        private CTimer timer;
        private CSoundTimer timerdb;
        private double n間隔;
        //-----------------
        #endregion
    }
}
