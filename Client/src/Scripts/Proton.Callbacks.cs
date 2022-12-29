using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Proton;

namespace Proton.Callbacks
{
    public static class ProtonCallbacks
    {
        public static void UpdateCallbacks()
        {
            if (ProtonEngine.CallbacksStack.Count == 0)
            {
                return;
            }

            object[] сallbackBody = ProtonEngine.CallbacksStack[0];
            
            string сallbackName = (string) сallbackBody[0];
            if (сallbackBody.Length > 1)
            {
                List<object> сallbackArgsList = new List<object>(сallbackBody);
                object[] arguments = сallbackArgsList.Skip(1).ToArray();
                InvokeCallback(сallbackName, arguments);
            }
            else
            {
                InvokeCallback(сallbackName, new object[] {});
            }

            ProtonEngine.CallbacksStack.RemoveAt(0);
        }
        public static void InvokeCallback(string callbackName, object[] arguments)
        {
            foreach (object targetScriptClass in ProtonEngine.CallbacksTargets)
            {
                if (targetScriptClass.GetType().GetMethod(callbackName) != null)
                {
                    targetScriptClass.GetType().GetMethod(callbackName).Invoke(targetScriptClass, arguments);
                }
            }
        }
    }
}