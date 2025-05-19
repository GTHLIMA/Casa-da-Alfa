using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingTextHandler : MonoBehaviour
{
    //faz com que os clones do prefabs sejam deletados depois de 1 segundo, caso o prefab não seja deletado, o clone fica na hierarquia.
    //isso consome memória e pode causar lag no jogo, o prefab é o texto que aparece quando o jogador acerta a casa
    void Start()
    {
        Destroy(gameObject, 1f);
        transform.localPosition += new Vector3(-2.2f, 4.5f, 0);
        
    }


}
