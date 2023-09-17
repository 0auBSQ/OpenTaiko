using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJAPlayer3
{
    internal class ModalQueue
    {
        public ModalQueue(Modal.EModalFormat mf)
        {
            _modalQueues = new Queue<Modal>[] { new Queue<Modal>(), new Queue<Modal>(), new Queue<Modal>(), new Queue<Modal>(), new Queue<Modal>() };
            _modalFormat = mf;
        }

        // Add two modals (one per player) at the same time
        public void tAddModal(Modal mp1, Modal mp2, Modal mp3, Modal mp4, Modal mp5)
        {
            mp1.modalFormat = _modalFormat;
            mp2.modalFormat = _modalFormat;
            mp3.modalFormat = _modalFormat;
            mp4.modalFormat = _modalFormat;
            mp5.modalFormat = _modalFormat;
            mp1.player = 0;
            mp2.player = 1;
            mp3.player = 2;
            mp4.player = 3;
            mp5.player = 4;
            mp1.tSetupModal();
            mp2.tSetupModal();
            mp3.tSetupModal();
            mp4.tSetupModal();
            mp5.tSetupModal();

            if (mp1 != null)
                _modalQueues[0].Enqueue(mp1);
            if (mp2 != null)
                _modalQueues[1].Enqueue(mp2);
            if (mp3 != null)
                _modalQueues[2].Enqueue(mp3);
            if (mp4 != null)
                _modalQueues[3].Enqueue(mp4);
            if (mp5 != null)
                _modalQueues[4].Enqueue(mp5);
        }

        // Add a single modal
        public void tAddModal(Modal mp, int player)
        {
            mp.modalFormat = _modalFormat;
            mp.player = player;
            mp.tSetupModal();

            if (mp != null && player >= 0 && player < TJAPlayer3.ConfigIni.nPlayerCount)
                _modalQueues[player].Enqueue(mp);
        }

        public Modal tPopModal(int player)
        {
            if (!tIsQueueEmpty(player))
                return _modalQueues[player].Dequeue();
            return null;
        }

        public bool tIsQueueEmpty(int player)
        {
            if (player < 0 || player >= TJAPlayer3.ConfigIni.nPlayerCount)
                return true;

            return _modalQueues[player].Count < 1;
        }

        public bool tAreBothQueuesEmpty()
        {
            return tIsQueueEmpty(0) && tIsQueueEmpty(1) && tIsQueueEmpty(2) && tIsQueueEmpty(3) && tIsQueueEmpty(4);
        }

        private Modal.EModalFormat _modalFormat;
        private Queue<Modal>[] _modalQueues;
    }
}
