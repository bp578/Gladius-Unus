using UnityEngine;
public class ProximityTrigger : MonoBehaviour
{
    public DoorController targetDoor1;
    public DoorController targetDoor2;
    public GameObject boss;
    public GameObject rickHealthBar;
	private bool alreadyTriggered = false;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
			if (alreadyTriggered) return;      
			if (!other.CompareTag("Player")) return;
			alreadyTriggered = true;            
			Debug.Log("Hit Collider");
            targetDoor1.CloseDoor();
            targetDoor2.CloseDoor();
            boss.SetActive(true);
            rickHealthBar.SetActive(true);
            targetDoor1.SwitchMusic();

        }
    }
}