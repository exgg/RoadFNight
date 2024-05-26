using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalDestroyer : MonoBehaviour {

	public float lifeTime = 5.0f;

    public bool isRocket = false;

	private IEnumerator Start()
	{
		yield return new WaitForSeconds(lifeTime);
		Destroy(gameObject);
	}

    private void Update()
    {
        if (isRocket & transform.parent != null)
            transform.parent = null;
    }
}
