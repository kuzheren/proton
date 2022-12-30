using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Proton.Callbacks;
using Proton.Network;
using Proton.GlobalStates;
using Proton;

public class ProtonMonoBehaviour : MonoBehaviour
{
    private void Start()
    {
        Application.runInBackground = true;
        ProtonGlobalStates.LastPingTime = System.DateTimeOffset.Now.ToUnixTimeSeconds();
        SceneManager.activeSceneChanged += OnSceneChanged;
        DontDestroyOnLoad(gameObject);
        StartCoroutine(UpdateCallbacks());
        StartCoroutine(UpdateStreamZone());
        StartCoroutine(CheckConnection());
    }
    public void OnSceneChanged(Scene current, Scene next)
    {
        object[] cachedCallbacks = ProtonEngine.CallbacksTargets.ToArray();
        
        foreach (object targetScript in cachedCallbacks)
        {
            string scriptScene = (string) targetScript.GetType().GetField("SceneName").GetValue(targetScript);
            if (scriptScene != next.name)
            {
                ProtonEngine.CallbacksTargets.Remove(targetScript);
            }
        }
    }
    private IEnumerator UpdateCallbacks()
    {
        while (true)
        {
            ProtonNetwork.Receive();
            ProtonCallbacks.UpdateCallbacks();
            if (ProtonEngine.CurrentRoom != null)
            {
                ProtonEngine.CurrentRoom.UpdateSendRate();
            }
            yield return new WaitForSeconds(0.005f);
        }
    }
    private IEnumerator UpdateStreamZone()
    {
        while (true)
        {
            yield return new WaitForSeconds(2);
            if (ProtonEngine.AutoStreamZoneUpdate == true && ProtonEngine.CurrentRoom != null && ProtonEngine.IsConnected() == true)
            {
                try
                {
                    ProtonEngine.UpdateStreamZome(Camera.current.transform.position);
                }
                catch {}
            }
        }
    }
    private IEnumerator CheckConnection()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            if (System.DateTimeOffset.Now.ToUnixTimeSeconds() - ProtonGlobalStates.LastPingTime > 10 && ProtonEngine.IsConnected())
            {
                ProtonEngine.Disconnect();
                ProtonEngine.InvokeCallback("OnKicked", new object[] {true});
            }
        }
    }
    private void OnApplicationQuit()
    {
        try
        {
            ProtonNetwork.Disconnect();
        }
        catch
        {

        }
    }
}
