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
                timeSinceCreation.t進行();

                // Display stuff here
            }

            public bool IsExpired()
            {
                return timeSinceCreation.b終了値に達した;
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
