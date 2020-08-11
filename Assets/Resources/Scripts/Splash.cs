using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Splash : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(5.5f);
        yield return new WaitUntil(() => 
        FirebaseManager.Instance.m_IsCourtsDownloadProgressCompleted == true 
        && FirebaseManager.Instance.m_IsProfilesDownloadProgressCompleted == true);
        GameObject.Find("Canvas").GetComponent<Animator>().Play("Logo_FadeOut");
        yield return new WaitForSeconds(1.5f);
        FirebaseManager.Instance.firestore.TerminateAsync();
        FirebaseManager.Instance.firestore.ClearPersistenceAsync();
        SceneManager.LoadSceneAsync(1);
    }
}
