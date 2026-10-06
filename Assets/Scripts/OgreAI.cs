using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class OgreAI : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    public GameObject player;
    public float attackRange = 3;
    private IEnumerator routine;
    public AnimationClip attackAnimation;
    private enum state{idle, attack,chase,dead};
    state currentState;
    Animator animator;
    private Quaternion q;
    private bool canAttack = true;
    private bool attacking = false;
    public float attackSpeed = 2f;
	private bool isDead = false;
	
	[Header("Death Drop")]
	[SerializeField] private float deathDropHeight = 2.22333f;
	[SerializeField] private float deathDropTime  = 0.4f;    

    public Stats stats;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        stats  = GetComponent<Stats>();
        currentState = state.idle;
    }

    // Update is called once per frame
    void Update()
    {
        switch (currentState)
        {
            case state.idle:
                if (stats.GetHealth() <= 0)
                {
                    StartCoroutine(Die());
                    break;
                }
                q = Quaternion.LookRotation(player.transform.position-transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, q,1);
                if (Vector3.Distance(transform.position, player.transform.position) > attackRange)
                {
                    animator.SetTrigger("Walk");
                    navMeshAgent.isStopped = false;
                    currentState = state.chase;
                    Debug.Log("Chasing");
                } else if (Vector3.Distance(transform.position, player.transform.position) <= attackRange && canAttack && !attacking)
                {
                    currentState = state.attack;
                }
                break;
            case state.chase:
                if (stats.GetHealth() <= 0)
                {
                    StartCoroutine(Die());
                    break;
                }
                navMeshAgent.SetDestination(player.transform.position);
                q = Quaternion.LookRotation(player.transform.position-transform.position);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, q,1);
                if (Vector3.Distance(transform.position, player.transform.position) <= attackRange)
                {
                    animator.SetTrigger("Idle");
                    navMeshAgent.isStopped = true;
                    currentState = state.idle;
                }
                break;
            case state.attack:
                if (stats.GetHealth() <= 0)
                {
                    StartCoroutine(Die());
                    break;
                }
                if (!attacking)
                {
                    Debug.Log("Attacking Routine");
                    StartCoroutine(Attack());
                }
                break;
            
        }
    }
	
	public bool getAttackState()
	{
		return attacking;
	}

    IEnumerator Attack()
    {
        //Attack animation started
        Debug.Log("Attack started");
        canAttack = false;
        attacking = true;
        navMeshAgent.isStopped = true;
        navMeshAgent.angularSpeed = 0;
        animator.SetTrigger("Attack");
        yield return new WaitForSeconds(attackAnimation.length);
        //Attack animation ended
        attacking = false;
        StartCoroutine(AttackCooldown(attackSpeed));
        Debug.Log("Attack ended");
        navMeshAgent.angularSpeed = 120;
        Debug.Log("Idle from Attack");
        animator.SetTrigger("Idle");
        currentState = state.idle;
    }

    IEnumerator Die()
    {
        isDead = true;
		animator.SetTrigger("Death");
        navMeshAgent.isStopped = true;
		//navMeshAgent.enabled    = false;      // <- important, prevents jitter
		//animator.applyRootMotion = false;
		
		yield return StartCoroutine(SinkAfterDeath());  // <── smooth move
		
        yield return new WaitForSeconds(5);

        Destroy(gameObject);
    }

    IEnumerator AttackCooldown(float cooldown)
    {
        canAttack = false;
        yield return new WaitForSeconds(cooldown);
        canAttack =  true;
    }
	
	public bool IsDead()
	{
		return isDead;
	}
	private IEnumerator SinkAfterDeath()
	{
		Vector3 startPos = transform.position;
		Vector3 endPos   = startPos - new Vector3(0f, deathDropHeight, 0f);

		float  t = 0f;                                   // 0→1 over deathDropTime
		while (t < 1f)
		{
			t += Time.deltaTime / deathDropTime;
			// smoothstep curve: feels nicer than linear
			float k = t * t * (3f - 2f * t);             
			transform.position = Vector3.LerpUnclamped(startPos, endPos, k);
			yield return null;                            // wait one frame
		}
		transform.position = endPos;                      // final clamp
	}
}
