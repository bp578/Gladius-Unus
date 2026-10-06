using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform door;
    public float speed = 2f;

    private Quaternion closedRot;
    private Quaternion openRot;

    public Stats bossStats;
    public float start;
    public float stop;

    public bool isOpening = false;
    public bool isClosing = false;

    private bool locked = false;
    public AudioSource doorSource;
    public AudioClip DoorOpenClip;
    public AudioClip DoorCloseClip;
    public AudioSource boss1;
    public AudioSource boss2;
    public AudioSource other;

    void Start()
    {
        door.localRotation = Quaternion.Euler(0, start, 0);
        closedRot = Quaternion.Euler(0, start, 0);
        openRot = Quaternion.Euler(0, stop, 0);
        StopAllMusic();
        boss1.Play();
    }

    void Update()
    {
        BossDead();

        if (isOpening)
        {
            door.localRotation = Quaternion.RotateTowards(door.localRotation, openRot, speed * Time.deltaTime * 100);
            if (Quaternion.Angle(door.localRotation, openRot) < 0.1f)
            {
                door.localRotation = openRot;
                isOpening = false;
                locked = true;
                StopAllMusic();
                other.Play();
            }
        }

        if (isClosing)
        {
            door.localRotation = Quaternion.RotateTowards(door.localRotation, closedRot, speed * Time.deltaTime * 100);
            if (Quaternion.Angle(door.localRotation, closedRot) < 0.1f)
            {
                door.localRotation = closedRot;
                isClosing = false;
            }
        }
    }

    void BossDead()
    {
        if (bossStats.GetHealth() == 0 && !locked)
        {
            OpenDoor();
            locked = true;
        }
    }

    public void OpenDoor()
    {
        isOpening = true;
        doorSource.PlayOneShot(DoorOpenClip);
    }

    public void CloseDoor()
    {
        isClosing = true;
        doorSource.PlayOneShot(DoorCloseClip);
    }

    public void StopAllMusic() {
        boss1.Stop();
        boss2.Stop();
        other.Stop();
    }
    public void SwitchMusic()
    {
        StopAllMusic();
        boss2.Play();
    }
}
