using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FDK;

namespace TJAPlayer3
{
    class CVisualLogManager
    {
        public enum ELogCardType
        {
            LogInfo,
            LogWarning,
            LogError
        }

        class LogCard
        {
            public LogCard(ELogCardType type, string message)
            {
                lct = type;
                msg = message;
                timeSinceCreation = new CCounter(0, 10000, 1, TJAPlayer3.Timer);
            }

            public void Display(int screenPosition)
            {
                timeSinceCreation.Tick();

                // Display stuff here

                int x = 0;
                int y = 0 + (40 * screenPosition);

                TJAPlayer3.act文字コンソール.tPrint(x, y, C文字コンソール.Eフォント種別.赤, msg);
            }

            public bool IsExpired()
            {
                return timeSinceCreation.IsEnded;
            }

            private CCounter timeSinceCreation;
            private ELogCardType lct;
            private string msg;
        }

        public void PushCard(ELogCardType lct, string msg)
        {
            cards.Add(new LogCard(lct, msg));
        }

        public void Display()
        {
            for (int i = 0; i < cards.Count; i++)
                cards[i].Display(i);
            cards.RemoveAll(card => card.IsExpired());
        }

        private List<LogCard> cards = new List<LogCard>();
    }
}
