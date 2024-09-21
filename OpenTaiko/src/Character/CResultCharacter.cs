using FDK;

namespace OpenTaiko {
	class CResultCharacter {
		private static CCounter[] ctCharacterNormal = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
		private static CCounter[] ctCharacterClear = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
		private static CCounter[] ctCharacterFailed = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };
		private static CCounter[] ctCharacterFailedIn = new CCounter[5] { new CCounter(), new CCounter(), new CCounter(), new CCounter(), new CCounter() };


		public enum ECharacterResult {
			// Song select
			NORMAL,
			CLEAR,
			FAILED,
			FAILED_IN,
		}

		public static bool tIsCounterProcessing(int player, ECharacterResult eca) {
			CCounter[] _ctref = _getReferenceCounter(eca);

			if (_ctref[player] != null)
				return _ctref[player].IsTicked;
			return false;
		}

		public static bool tIsCounterEnded(int player, ECharacterResult eca) {
			CCounter[] _ctref = _getReferenceCounter(eca);

			if (_ctref[player] != null)
				return _ctref[player].IsEnded;
			return false;
		}

		private static bool _usesSubstituteTexture(int player, ECharacterResult eca) {
			int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;

			if (_charaId >= 0 && _charaId < OpenTaiko.Skin.Characters_Ptn) {
				switch (eca) {
					case (ECharacterResult.NORMAL): {
							if (OpenTaiko.Tx.Characters_Result_Normal[_charaId].Length > 0)
								return false;
							break;
						}
					case (ECharacterResult.CLEAR): {
							if (OpenTaiko.Tx.Characters_Result_Clear[_charaId].Length > 0)
								return false;
							break;
						}
					case (ECharacterResult.FAILED): {
							if (OpenTaiko.Tx.Characters_Result_Failed[_charaId].Length > 0)
								return false;
							break;
						}
					case (ECharacterResult.FAILED_IN): {
							if (OpenTaiko.Tx.Characters_Result_Failed_In[_charaId].Length > 0)
								return false;
							break;
						}
				}
			}

			return true;
		}

		public static CTexture[] _getReferenceArray(int player, ECharacterResult eca) {
			int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;

			if (_charaId >= 0 && _charaId < OpenTaiko.Skin.Characters_Ptn) {
				switch (eca) {
					case (ECharacterResult.NORMAL): {
							if (OpenTaiko.Tx.Characters_Result_Normal[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Result_Normal[_charaId];
							if (OpenTaiko.Tx.Characters_Normal[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Normal[_charaId];
							break;
						}
					case (ECharacterResult.CLEAR): {
							if (OpenTaiko.Tx.Characters_Result_Clear[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Result_Clear[_charaId];
							if (OpenTaiko.Tx.Characters_10Combo[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_10Combo[_charaId];
							break;
						}
					case (ECharacterResult.FAILED): {
							if (OpenTaiko.Tx.Characters_Result_Failed[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Result_Failed[_charaId];
							if (OpenTaiko.Tx.Characters_Normal[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Normal[_charaId];
							break;
						}
					case (ECharacterResult.FAILED_IN): {
							if (OpenTaiko.Tx.Characters_Result_Failed_In[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Result_Failed_In[_charaId];
							if (OpenTaiko.Tx.Characters_Normal[_charaId].Length > 0)
								return OpenTaiko.Tx.Characters_Normal[_charaId];
							break;
						}
				}
			}


			return null;
		}

		public static CCounter[] _getReferenceCounter(ECharacterResult eca) {
			switch (eca) {
				case (ECharacterResult.NORMAL): {
						return ctCharacterNormal;
					}
				case (ECharacterResult.CLEAR): {
						return ctCharacterClear;
					}
				case (ECharacterResult.FAILED): {
						return ctCharacterFailed;
					}
				case (ECharacterResult.FAILED_IN): {
						return ctCharacterFailedIn;
					}
			}
			return null;
		}

		public static int _getReferenceAnimationDuration(int player, ECharacterResult eca) {
			int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;

			switch (eca) {
				case (ECharacterResult.NORMAL): {
						return OpenTaiko.Skin.Characters_Result_Normal_AnimationDuration[_charaId];
					}
				case (ECharacterResult.CLEAR): {
						return OpenTaiko.Skin.Characters_Result_Clear_AnimationDuration[_charaId];
					}
				case (ECharacterResult.FAILED): {
						return OpenTaiko.Skin.Characters_Result_Failed_AnimationDuration[_charaId];
					}
				case (ECharacterResult.FAILED_IN): {
						return OpenTaiko.Skin.Characters_Result_Failed_In_AnimationDuration[_charaId];
					}
			}
			return 1000;
		}

		public static void tDisableCounter(ECharacterResult eca) {
			switch (eca) {
				case (ECharacterResult.NORMAL): {
						for (int i = 0; i < 5; i++)
							ctCharacterNormal[i] = new CCounter();
						break;
					}
				case (ECharacterResult.CLEAR): {
						for (int i = 0; i < 5; i++)
							ctCharacterClear[i] = new CCounter();
						break;
					}
				case (ECharacterResult.FAILED): {
						for (int i = 0; i < 5; i++)
							ctCharacterFailed[i] = new CCounter();
						break;
					}
				case (ECharacterResult.FAILED_IN): {
						for (int i = 0; i < 5; i++)
							ctCharacterFailedIn[i] = new CCounter();
						break;
					}
			}

		}


		public static void tMenuResetTimer(int player, ECharacterResult eca) {
			CTexture[] _ref = _getReferenceArray(player, eca);
			CCounter[] _ctref = _getReferenceCounter(eca);
			int _animeref = _getReferenceAnimationDuration(player, eca);

			if (_ref != null && _ref.Length > 0 && _ctref != null) {
				_ctref[player] = new CCounter(0, _ref.Length - 1, _animeref / (float)_ref.Length, OpenTaiko.Timer);
			}
		}

		public static void tMenuResetTimer(ECharacterResult eca) {
			for (int i = 0; i < 5; i++) {
				tMenuResetTimer(i, eca);
			}
		}

		public static void tMenuDisplayCharacter(int player, int x, int y, ECharacterResult eca, int pos = 0, int opacity = 255) {
			int _charaId = OpenTaiko.SaveFileInstances[OpenTaiko.GetActualPlayer(player)].data.Character;
			CTexture[] _ref = _getReferenceArray(player, eca);
			CCounter[] _ctref = _getReferenceCounter(eca);
			bool _substitute = _usesSubstituteTexture(player, eca);

			if (_ctref[player] != null && _ref != null && _ctref[player].CurrentValue < _ref.Length) {
				if (eca == ECharacterResult.NORMAL
					|| eca == ECharacterResult.CLEAR
					|| eca == ECharacterResult.FAILED)
					_ctref[player].TickLoop();
				else
					_ctref[player].Tick();

				// Quick fix
				if (_ctref[player].CurrentValue >= _ref.Length) return;

				var _tex = _ref[_ctref[player].CurrentValue];

				_tex.Opacity = opacity;

				float resolutionRatioX = OpenTaiko.Skin.Resolution[0] / (float)OpenTaiko.Skin.Characters_Resolution[_charaId][0];
				float resolutionRatioY = OpenTaiko.Skin.Resolution[1] / (float)OpenTaiko.Skin.Characters_Resolution[_charaId][1];

				//202
				//float _x = (x - (((_substitute == true) ? 20 : 40) * (TJAPlayer3.Skin.Characters_Resolution[_charaId][0] / 1280.0f))) * resolutionRatioX;

				//532
				//float _y = (y - (((_substitute == true) ? 20 : 40) * (TJAPlayer3.Skin.Characters_Resolution[_charaId][1] / 720.0f))) * resolutionRatioY;

				float _x = x;
				float _y = y;

				_tex.vcScaleRatio.X *= resolutionRatioX;
				_tex.vcScaleRatio.Y *= resolutionRatioY;

				if (pos % 2 == 0 || OpenTaiko.ConfigIni.nPlayerCount > 2) {
					_tex.t2D拡大率考慮下中心基準描画(
						_x,
						_y
						);
				} else {
					_tex.t2D拡大率考慮下中心基準描画Mirrored(
						_x,
						_y
						);
				}

				_tex.vcScaleRatio.X = 1f;
				_tex.vcScaleRatio.Y = 1f;


				_tex.Opacity = 255;

			}

		}
	}
}
