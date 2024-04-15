using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    GameObject player;

    // Start is called before the first frame update
    void Awake()
    {
        player = GameObject.Find("Player");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 2, this.transform.position.z);
    }
}
