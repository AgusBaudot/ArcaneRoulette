using System;

namespace Foundation
{
	/// <summary>
	/// Describes the global screen feedback for one attack. Passed to DamageBatch.Commit()
	///
	/// CameraShake: trauma added to CameraShake (0-1, stacks additively).
	/// HitStop: duration in second Time.timeScale is frozen.
	/// </summary>
	[Serializable]
	public struct DamageJuice
	{
		/// <summary>
		/// Duration in seconds of the HitStop freeze. 0 = no freeze.
		/// </summary>
		public float HitStop;
		
		/// <summary>
		/// Camera trauma applied. 0 = no shake.
		/// </summary>
		public float CameraShake;

		/// <summary>
		/// Optional sound to play on hit.
		/// AudioManager picks a random clip, applies polyphony/cooldown rules, and routes to the currect bus.
		/// Null = silent impact.
		/// </summary>
		public AudioEventSO ImpactSound;

		/// <summary>
		/// World position for 3D spatialized impact sounds.
		/// Ignored when ImpactSound.Is3D is false or ImpactSound is null.
		/// </summary>
		public UnityEngine.Vector3 ImpactPosition;
		
		/// <summary>
		/// Zero juice. Commit() with this is a legal no-op.
		/// </summary>
		public static DamageJuice None => default;
	}
}