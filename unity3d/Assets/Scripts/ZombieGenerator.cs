using UnityEngine;
using System.Collections;

public class ZombieGenerator : MonoBehaviour {

	public Transform zombiePrefab;

	public static int MAX_COUNT = 1;
	private int count;

	private float timer;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if(count >= MAX_COUNT){
			return;
		}

		timer -= Time.deltaTime;
		if(timer <= 0){
			timer = Random.value * 15.0f;
			if(timer < 5){
				timer = 5;
			}

			Transform enemyObj = Instantiate(zombiePrefab, transform.position, Quaternion.identity) as Transform;
			Zombie zombie = enemyObj.GetComponent<Zombie>();
			zombie.Init(this);
		}
	}

	void OnDrawGizmos(){
		Gizmos.DrawIcon(transform.position, "../Gizmos/item.png", true);
	}

	public void IncrCount(){
		count++;
	}

	public void DecrCount(){
		count--;
	}

}
