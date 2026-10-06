using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class KnightAI : MonoBehaviour
{
    private NavMeshAgent navMeshAgent;
    public GameObject player;
    public float attackRange;
    private IEnumerator routine;
    public AnimationClip normalAttackAnimation, attack2, attack3;
    private enum state{idle, attack,chase,dead};
    state currentState;
    Animator animator;
    private Stats stats;
    private Quaternion q;
    private bool canAttack = true;
    private bool attacking = false;
    public float attackSpeed = 5f;
	private bool isDead = false;

	[Header("Death Drop")]
	[SerializeField] private float deathDropHeight = 1.06033397f; 
	[SerializeField] private float deathDropTime  = 0.4f;         // units per second
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        currentState = state.idle;
        stats = GetComponent<Stats>();
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
					animator.ResetTrigger("Idle");
					animator.ResetTrigger("Attack");
					animator.ResetTrigger("Combo");
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
					animator.ResetTrigger("Attack");
					animator.ResetTrigger("Combo");
					animator.ResetTrigger("Walk");
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
                    SelectAttack();
                }
                break;
        }
    }

    private void SelectAttack()
    {
        //Select from 2 attacks
        int attackId = Random.Range(0, 2);
        Debug.Log($"Attack ID: {attackId}");
        switch (attackId)
        {
            case 0:
                //One hit slash
                Debug.Log($"Single attack");
                StartCoroutine(Attack(normalAttackAnimation.length, "Attack"));
                break;
            case 1:
                //3 hit combo
                Debug.Log($"Combo attack");
                StartCoroutine(Attack(normalAttackAnimation.length + attack2.length + attack3.length, "Combo"));
                break;
        }
    }
    
    IEnumerator Attack(float animationLength, string triggerName)
    {
        //Attack animation started
        Debug.Log("Attack started");
        canAttack = false;
        attacking = true;
        navMeshAgent.isStopped = true;
        animator.SetTrigger(triggerName);
        yield return new WaitForSeconds(animationLength);
        Debug.Log("Attack ended");
        //Attack animation ended
        attacking = false;
        StartCoroutine(AttackCooldown(attackSpeed));
        Debug.Log("Idle from Attack");
		animator.ResetTrigger("Attack");
		animator.ResetTrigger("Combo");
		animator.ResetTrigger("Walk");
        animator.SetTrigger("Idle");
        currentState = state.idle;
    }
    
    IEnumerator Die()
    {
        isDead = true;
		animator.ResetTrigger("Idle");
		animator.ResetTrigger("Attack");
		animator.ResetTrigger("Combo");
		animator.ResetTrigger("Walk");
		//navMeshAgent.enabled    = false;      // <- important, prevents jitter
		//animator.applyRootMotion = false;
	    navMeshAgent.isStopped = true;
		animator.SetTrigger("Death");
		yield return StartCoroutine(SinkAfterDeath());  // <── smooth move

		
 		
		
        yield return new WaitForSeconds(5f);
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
