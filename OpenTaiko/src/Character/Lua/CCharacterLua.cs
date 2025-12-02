using FDK;
using Silk.NET.Maths;

namespace OpenTaiko;

class CCharacterLua : CCharacter {
	private CLuaCharacterScript[] Script = new CLuaCharacterScript[5];

	private Dictionary<string, int>[] animationLoadCounts = new Dictionary<string, int>[5] {
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>()
	};

	private Dictionary<string, int>[] voiceLoadCounts = new Dictionary<string, int>[5] {
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>(),
		new Dictionary<string, int>()
	};

	public CLuaCharacterScript GetScript(int player) => Script[player];

	public CCharacterLua(string path, int i) : base(path, i) {
		for (int player = 0; player < 5; player++) {
			Script[player] = new CLuaCharacterScript(path, null, null, true);
		}
	}

	public override void Dispose() {
		base.Dispose();
		for (int player = 0; player < 5; player++) {
			Script[player].Dispose();
			voiceLoadCounts[player].Clear();
		}
	}

	public override void LoadAnimation(int player, string animationType) {
		if (animationType == ANIM_NONE) return;
		Dictionary<string, int> animationCounts = animationLoadCounts[player];
		if (!animationCounts.ContainsKey(animationType)) animationCounts.Add(animationType, 0);

		animationCounts[animationType]++;

		if (animationCounts[animationType] == 1) {
			ImplLoadAnimation(player, animationType);
			bool avaiable = AvaiableAnimation(player, animationType, false);
			if (!avaiable) {
				animationCounts[animationType]--;

				string alternative = GetAlternativeAnimation(animationType);
				LoadAnimation(player, alternative);
			}
		}

	}

	public override void DisposeAnimation(int player, string animationType) {
		if (animationType == ANIM_NONE) return;

		bool avaiable = AvaiableAnimation(player, animationType, false);
		if (!avaiable) {
			string alternative = GetAlternativeAnimation(animationType);
			DisposeAnimation(player, alternative);
			return;
		}

		Dictionary<string, int> animationCounts = animationLoadCounts[player];
		if (!animationCounts.ContainsKey(animationType)) animationCounts.Add(animationType, 1);

		animationCounts[animationType]--;

		if (animationCounts[animationType] == 0) {
			ImplDisposeAnimation(player, animationType);
			animationCounts.Remove(animationType);
		}
	}

	public override bool AvaiableAnimation(int player, string animationType, bool useAlternative = true) {
		if (animationType == ANIM_NONE) return false;
		for (int i = 0; i < 5; i++) {
			bool avaiable = Script[player].AvaialbeAnimation(animationType);
			if (avaiable) return true;
			if (!useAlternative) return false;

			animationType = GetAlternativeAnimation(animationType);
		}
		return false;
	}

	public override void SetAnimationDuration(int player, string animationType, double duration) {
		Script[player].SetAnimationDuration(GetAnimation(player, this, animationType), duration);
	}

	public override void ResetAnimationCounter(int player, string animationType) {
		Script[player].ResetAnimationCounter(GetAnimation(player, this, animationType));
	}

	//voice-------------
	public override void LoadVoice(int player, string voice) {
		Dictionary<string, int> voiceCounts = voiceLoadCounts[player];
		if (!voiceCounts.ContainsKey(voice)) voiceCounts.Add(voice, 0);

		if (voiceCounts[voice] == 0) {
			ImplLoadVoice(player, voice);
		}

		voiceCounts[voice]++;
	}

	public override void DisposeVoice(int player, string voice) {
		Dictionary<string, int> voiceCounts = voiceLoadCounts[player];
		if (!voiceCounts.ContainsKey(voice)) voiceCounts.Add(voice, 0);

		if (voiceCounts[voice] <= 0) return;
		voiceCounts[voice]--;

		if (voiceCounts[voice] == 0) {
			ImplDisposeVoice(player, voice);
			voiceCounts.Remove(voice);
		}
	}

	public override bool Update(int player, string animationType, bool looping = true) {
		return Script[player].Update(OpenTaiko.FPS.DeltaTime, GetAnimation(player, this, animationType), looping);
	}

	public override void Draw(int player, string animationType, float x, float y, float scaleX = 1.0f, float scaleY = 1.0f, int opacity = 255, Color4? color = null, bool flipX = false) {
		Script[player].Draw(GetAnimation(player, this, animationType), x, y, scaleX, scaleY, opacity, new LuaColor(color ?? Color4.White), flipX);
	}


	private void ImplLoadAnimation(int player, string animationType) {
		Script[player].LoadAnimation(animationType);
	}

	private void ImplDisposeAnimation(int player, string animationType) {
		Script[player].DisposeAnimation(animationType);
	}

	public override void PlayVoice(int player, string voice) {
		Script[player].PlayVoice(voice);
	}

	private void ImplLoadVoice(int player, string voice) {
		Script[player].LoadVoice(voice);
	}

	private void ImplDisposeVoice(int player, string voice) {
		Script[player].DisposeVoice(voice);
	}

	//------------------
}
