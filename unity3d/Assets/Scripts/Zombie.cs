using UnityEngine;
using System.Collections;

[RequireComponent (typeof (NavMeshAgent))]
[RequireComponent (typeof (Animator))]
[RequireComponent (typeof (CapsuleCollider))]
[RequireComponent (typeof (AudioSource))]
public class Zombie : MonoBehaviour {

	//玩家
	private Player player;

	//寻路代理
	private NavMeshAgent agent;

	//动画
	private Animator animator;

	public float moveSpeed = 0.5f;

	public int hp;

	private float rotateSpeed = 120;

	private float actTimer = 2;

	protected ZombieGenerator generator;

	private CapsuleCollider capsuleCollider;

	private bool damagePlayer; //记录在attack范围内播放attack动画必定造成1点伤害

	public AudioClip attackSound;
	public AudioClip deathSound;

	//僵尸血量为0设置death死亡状态true后有个转换状态的时间，所以不在death为true时发出死亡声效，
	//实际上僵尸会在死亡后一段时间才倒下
	private bool isPlayDeathSound;

	public void Init(ZombieGenerator generator){
		generator.IncrCount();
		this.generator = generator;
	}

	// Use this for initialization
	void Start () {
		player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
		agent = GetComponent<NavMeshAgent>();
		animator = GetComponent<Animator>();
		capsuleCollider = GetComponent<CapsuleCollider>();
	}

	public bool IsDeath(){
		return hp <= 0;
	}
	
	// Update is called once per frame
	void Update () {
		if(player.IsDeath()){
			return;
		}
			
		if (animator.IsInTransition (0)) {
			return;
		}

		//基于状态机的判断播放相应的行为动画
		AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
		if(InState(state, "idle")){
			animator.SetBool("idle", false);
			damagePlayer = false;

			actTimer -= Time.deltaTime;
			if(actTimer > 0){
				return;
			}

			if(DistanceToPlayerLessEnough()) {
				animator.SetBool ("attack", true);
			}else{
				animator.SetBool("run", true);
				//agent.SetDestination(player.transform.position);
				actTimer = 1;
			}
		}else if(InState(state, "run")){
			animator.SetBool("run", false);

			if(DistanceToPlayerLessEnough()){
				animator.SetBool ("attack", true);
				agent.ResetPath();
				return;
			}else{
				actTimer -= Time.deltaTime;
				if(actTimer < 0){
					agent.SetDestination(player.transform.position);
					actTimer = 1;
				}

				//发现其实只需要agent.SetDestination便可以自动寻路了，都不需要调用agent.Move
				//MoveToPlayer();
			}
		}else if(InState(state, "attack")){
			animator.SetBool("attack", false);
			if (!DistanceToPlayerLessEnough()) {
				animator.SetBool("run", true);
			} else {
				//设定在此范围内造成的伤害为1点，太大范围的话因为update函数执行时处于attack状态比较长，所以会一直持续掉血
				//或者可以使用一个布尔值记录仅此一次attack动画造成的掉血，与attack动画播放无关，但是体验不太好，最好体验是attack动画出手立即掉血
				//print("state.normalizedTime="+state.normalizedTime);
				if (!damagePlayer) {
					if(IsAttackSound()){
						GetComponent<AudioSource>().PlayOneShot(attackSound);
					}
					if (state.normalizedTime > 0.61f && state.normalizedTime < 0.63f || state.normalizedTime > 0.63f) {
						DamagePlayer();
					}
				} else {
					animator.SetBool("idle", true);
					actTimer = 1;
				}

				RotateToPlayer ();
			}

		}else if(InState(state, "death")){
			if(!isPlayDeathSound){
				GetComponent<AudioSource>().PlayOneShot(deathSound);
				isPlayDeathSound = true;
			}
			CapsuleColliderDisable();
			if (AninatorPlayLargerThan(state, 2)) {
				OnDeath();
			}
		}
	}

	bool IsAttackSound(){
		int randomNum = Random.Range(1, 100);
		return randomNum >= 25 && randomNum <= 28;
	}

	bool InState(AnimatorStateInfo state, string stateStr){
		//return state.fullPathHash == Animator.StringToHash("Base Layer."+stateStr);
		//使用如下的IsName方法替代上面的哈希值判断
		return state.IsName(stateStr);
	}

	bool AninatorPlayEnough(AnimatorStateInfo state){
		return state.normalizedTime >= 1f;
	}

	bool AninatorPlayLargerThan(AnimatorStateInfo state, float time){
		return state.normalizedTime >= time;
	}

	bool DistanceToPlayerLessEnough(){
		return DistanceToPlayerLessThan(2f);
	}

	bool DistanceToPlayerLessThan(float distance){
		return Vector3.Distance (transform.position, player.transform.position) < distance;
	}

	/*
	void MoveToPlayer(){
		agent.SetDestination(player.transform.position);
		agent.Move(transform.TransformDirection(new Vector3(0, 0, moveSpeed*Time.deltaTime)));
	}
	*/

	void RotateToPlayer(){
		Vector3 old = transform.eulerAngles;
		transform.LookAt(player.transform);
		float playerY = transform.eulerAngles.y;

		float speed = rotateSpeed * Time.deltaTime;
		float angle = Mathf.MoveTowardsAngle(old.y, playerY, speed);
		transform.eulerAngles = new Vector3(0, angle, 0);
	}

	void DamagePlayer(){
		player.OnDamage(1);
		damagePlayer = true;
	}

	void OnDamage(int damage){
		if(IsDeath()){
			return;
		}

		hp -= damage;

		if(IsDeath()){
			animator.SetBool("death", true);
		}
	}

	void CapsuleColliderDisable(){
		if (capsuleCollider.enabled) {
			capsuleCollider.enabled = false;
		}
	}

	void OnDeath(){
		GUIManager.instance.AddScore(100);
		generator.DecrCount();
		Destroy(this.gameObject);
	}

}
