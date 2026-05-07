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
		public float HitStop;
		public float CameraShake;

		public DamageJuice(float hitStop, float cameraShake)
		{
			HitStop = hitStop;
			CameraShake = cameraShake;
		}
		
		/// <summary>
		/// Zero juice. Commit() with this is a legal no-op.
		/// </summary>
		public static DamageJuice None => default;
	}
}