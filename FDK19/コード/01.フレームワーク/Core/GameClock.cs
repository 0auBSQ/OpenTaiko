/*
* Copyright (c) 2007-2009 SlimDX Group
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/
using System;
using System.Diagnostics;

namespace SampleFramework
{
    class GameClock
    {
        long baseRealTime;
        long lastRealTime;
        bool lastRealTimeValid;
        int suspendCount;
        long suspendStartTime;
        long timeLostToSuspension;
        TimeSpan currentTimeBase;
        TimeSpan currentTimeOffset;

        public TimeSpan CurrentTime
        {
            get { return currentTimeBase + currentTimeOffset; }
        }

        public TimeSpan ElapsedTime
        {
            get;
            private set;
        }

        public TimeSpan ElapsedAdjustedTime
        {
            get;
            private set;
        }

        public static long Frequency
        {
            get { return Stopwatch.Frequency; }
        }

        public GameClock()
        {
            Reset();
        }

        public void Reset()
        {
            currentTimeBase = TimeSpan.Zero;
            currentTimeOffset = TimeSpan.Zero;
            baseRealTime = Stopwatch.GetTimestamp();
            lastRealTimeValid = false;
        }

        public void Suspend()
        {
            suspendCount++;
            if (suspendCount == 1)
                suspendStartTime = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// Resumes a previously suspended clock.
        /// </summary>
        public void Resume()
        {
            suspendCount--;
            if (suspendCount <= 0)
            {
                timeLostToSuspension += Stopwatch.GetTimestamp() - suspendStartTime;
                suspendStartTime = 0;
            }
        }

        public void Step()
        {
            long counter = Stopwatch.GetTimestamp();

            if (!lastRealTimeValid)
            {
                lastRealTime = counter;
                lastRealTimeValid = true;
            }

            try
            {
                currentTimeOffset = CounterToTimeSpan(counter - baseRealTime);
            }
            catch (OverflowException)
            {
                // update the base value and try again to adjust for overflow
                currentTimeBase += currentTimeOffset;
                baseRealTime = lastRealTime;

                try
                {
                    // get the current offset
                    currentTimeOffset = CounterToTimeSpan(counter - baseRealTime);
                }
                catch (OverflowException)
                {
                    // account for overflow
                    baseRealTime = counter;
                    currentTimeOffset = TimeSpan.Zero;
                }
            }

            try
            {
                ElapsedTime = CounterToTimeSpan(counter - lastRealTime);
            }
            catch (OverflowException)
            {
                ElapsedTime = TimeSpan.Zero;
            }

            try
            {
                ElapsedAdjustedTime = CounterToTimeSpan(counter - (lastRealTime + timeLostToSuspension));
                timeLostToSuspension = 0;
            }
            catch (OverflowException)
            {
                ElapsedAdjustedTime = TimeSpan.Zero;
            }

            lastRealTime = counter;
        }

        static TimeSpan CounterToTimeSpan(long delta)
        {
            return TimeSpan.FromTicks((delta * 10000000) / Frequency);
        }
    }
}
