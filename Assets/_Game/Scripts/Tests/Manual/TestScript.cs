using UnityEngine;

public class TestScript : MonoBehaviour
{  
    public void Test()
    {
        GameRoot.Instance.StageFlow.ChangeSceneAsync("TITLE", null);
    }
}
