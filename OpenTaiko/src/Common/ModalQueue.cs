namespace OpenTaiko {
	internal class ModalQueue {
		public ModalQueue(Modal.EModalFormat mf) {
			_modalQueues = new Queue<Modal>[] { new Queue<Modal>(), new Queue<Modal>(), new Queue<Modal>(), new Queue<Modal>(), new Queue<Modal>() };
			_modalFormat = mf;
		}

		// Add a single modal
		public void tAddModal(Modal mp, int player) {
			mp.modalFormat = _modalFormat;
			mp.player = player;

			if (mp != null && player >= 0 && player < OpenTaiko.ConfigIni.nPlayerCount)
				_modalQueues[player].Enqueue(mp);
		}

		public Modal tPopModal(int player) {
			if (!tIsQueueEmpty(player))
				return _modalQueues[player].Dequeue();
			return null;
		}

		// 1P => 2P => 3P => 4P => 5P
		public Modal? tPopModalInOrder() {
			for (int i = 0; i < OpenTaiko.ConfigIni.nPlayerCount; i++) {
				if (!tIsQueueEmpty(i)) {
					Modal? _m = _modalQueues[i].Dequeue();
					_m?.tRegisterModal(i + 1);
					return _m;
				}

			}
			return null;
		}

		public bool tIsQueueEmpty(int player) {
			if (player < 0 || player >= OpenTaiko.ConfigIni.nPlayerCount)
				return true;

			return _modalQueues[player].Count < 1;
		}

		public bool tAreBothQueuesEmpty() {
			return tIsQueueEmpty(0) && tIsQueueEmpty(1) && tIsQueueEmpty(2) && tIsQueueEmpty(3) && tIsQueueEmpty(4);
		}

		private Modal.EModalFormat _modalFormat;
		private Queue<Modal>[] _modalQueues;
	}
}
