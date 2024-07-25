using UnityEngine;
using Mirror;

public class PlayerNutritionModule : NetworkBehaviour
{

	[SyncVar] public int value;

	private void OnValidate()
	{
		syncMode = SyncMode.Owner;
		syncInterval = 0f;
	}

	[SerializeField] private float _intervalInSeconds;
	private float _timeLeft;

	private void Start()
	{
		_timeLeft = _intervalInSeconds;
	}

	/// <summary>
	/// every _intervalInSeconds seconds, the player's nutrition value decreases
	/// </summary>
	private void Update()
	{
		if (isServer)
		{
			// don't increment down nutrition value when it is 0
			if (value <= 0)
			{
				return;
			}

			_timeLeft -= Time.deltaTime;

			// reset timer and increment down nutrition value when _timeLeft reaches 0
			if (_timeLeft <= 0)
			{
				_timeLeft = _intervalInSeconds;
				value--;
			}
		}
	}
}
