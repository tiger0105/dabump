using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Splash : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(7f);
        SceneManager.LoadSceneAsync(1);
    }
}
