using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TJAPlayer3;
using FDK;

namespace TJAPlayer3.Animations
{
    class Animator : IAnimatable
    {
        public Animator(int startValue, int endValue, int tickInterval, bool isLoop)
        {
            Type = CounterType.Normal;
            StartValue = startValue;
            EndValue = endValue;
            TickInterval = tickInterval;
            IsLoop = isLoop;
            Counter = new CCounter();
        }
        public Animator(double startValue, double endValue, double tickInterval, bool isLoop)
        {
            Type = CounterType.Double;
            StartValue = startValue;
            EndValue = endValue;
            TickInterval = tickInterval;
            IsLoop = isLoop;
            Counter = new CCounter();
        }
        public void Start()
        {
            if (Counter == null) throw new NullReferenceException();
            switch (Type)
            {
                case CounterType.Normal:
                    Counter.t開始((int)StartValue, (int)EndValue, (int)TickInterval, TJAPlayer3.Timer);
                    break;
                case CounterType.Double:
                    Counter.t開始((double)StartValue, (double)EndValue, (double)TickInterval, CSound管理.rc演奏用タイマ);
                    break;
                default:
                    break;
            }
        }
        public void Stop()
        {
            if (Counter == null) throw new NullReferenceException();
            Counter.t停止();
        }
        public void Reset()
        {
            if (Counter == null) throw new NullReferenceException();
            Start();
        }

        public void Tick()
        {
            if (Counter == null) throw new NullReferenceException();
            switch (Type)
            {
                case CounterType.Normal:
                    if (IsLoop) Counter.t進行Loop(); else Counter.t進行();
                    if (!IsLoop && Counter.b終了値に達した) Stop();
                    break;
                case CounterType.Double:
                    if (IsLoop) Counter.t進行LoopDb(); else Counter.t進行db();
                    if (!IsLoop && Counter.b終了値に達した) Stop();
                    break;
                default:
                    break;
            }
        }

        public virtual object GetAnimation()
        {
            throw new NotImplementedException();
        }



        // プロパティ
        public CCounter Counter
        {
            get;
            private set;
        }
        public CounterType Type
        {
            get;
            private set;
        }
        public object StartValue
        {
            get;
            private set;
        }
        public object EndValue
        {
            get;
            private set;
        }
        public object TickInterval
        {
            get;
            private set;
        }
        public bool IsLoop
        {
            get;
            private set;
        }
    }

    enum CounterType
    {
        Normal,
        Double
    }
}
