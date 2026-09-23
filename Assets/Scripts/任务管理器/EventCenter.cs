using System;
using System.Collections.Generic;

public static class EventCenter
{
    private static readonly Dictionary<string, Action> eventDict = new Dictionary<string, Action>();//事件字典，键为事件名，值为委托（事件处理方法），不支持带参数的事件
    private static readonly Dictionary<string, Delegate> eventTDict = new Dictionary<string, Delegate>();//事件字典，键为事件名，值为委托（事件处理方法），支持带参数的事件
    #region 支持不带参数的事件
    /// <summary>
    /// 添加事件监听器
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="callback"></param>
    public static void AddListener(string eventName, Action callback)//添加事件监听器，eventName是事件的名称，callback是事件触发时要调用的方法,
    {                                                                //AddListener方法只是在存储方法，并不执行方法，只有当Trigger方法被调用时，才会执行存储的方法
        if (callback == null) return;
        if (!eventDict.ContainsKey(eventName))//如果事件字典中没有这个事件名，就添加一个新的键值对，值为null
        {
            eventDict.Add(eventName, null);
        }
        eventDict[eventName] = (Action)eventDict[eventName] + callback;//使用委托的加法运算符将新的回调函数添加到现有的委托链中
    }

    /// <summary>
    /// 移除事件监听器
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="callback"></param>
    public static void RemoveListener(string eventName, Action callback)
    {
        if (callback == null || !eventDict.TryGetValue(eventName, out var del)) return;
        del -= callback;
        //如果移除后委托为空，说明没有监听器了，可以从字典中移除这个事件
        if (del == null)
        {
            eventDict.Remove(eventName);
        }
        else
        {
            eventDict[eventName] = del;
        }
    }
    /// <summary>
    /// 触发事件
    /// </summary>
    /// <param name="eventName"></param>
    public static void Trigger(string eventName)//触发事件，调用所有注册的回调函数
    {
        if (eventDict.TryGetValue(eventName, out var del) && del is Action callback)
        {
            callback.Invoke();
        }
    }
    #endregion
    #region 支持带参数的事件
    public static void AddListener<T>(string eventName, Action<T> callback)
    {
        if (callback == null) return;
        if (!eventTDict.ContainsKey(eventName))
        {
            eventTDict[eventName] = null;
        }
        eventTDict[eventName] = Delegate.Combine(eventTDict[eventName], callback);//使用Delegate.Combine方法将新的回调函数与现有的委托组合起来
    }
    public static void RemoveListener<T>(string eventName, Action<T> callback)
    {
        if (callback == null || !eventTDict.TryGetValue(eventName, out var del)) return;
        Delegate newDel = Delegate.Remove(del, callback);
        if (newDel == null)
        {
            eventTDict.Remove(eventName);
        }
        else
        {
            eventTDict[eventName] = newDel;
        }
    }
    public static void Trigger<T>(string eventName, T arg)
    {
        if (eventTDict.TryGetValue(eventName, out var del))
        {
            (del as Action<T>)?.Invoke(arg);
        }
    }
    #endregion
    #region 支持多参数的事件
    public static void AddListener<T1, T2>(string eventName, Action<T1, T2> callback)
    {
        if (callback == null) return;
        if (!eventTDict.ContainsKey(eventName))
        {
            eventTDict[eventName] = null;
        }
        eventTDict[eventName] = Delegate.Combine(eventTDict[eventName], callback);
    }
    public static void RemoveListener<T1, T2>(string eventName, Action<T1, T2> callback)
    {
        if (callback == null || !eventTDict.TryGetValue(eventName, out var del)) return;
        Delegate newDel = Delegate.Remove(del, callback);
        if (newDel == null)
        {
            eventTDict.Remove(eventName);
        }
        else
        {
            eventTDict[eventName] = newDel;
        }
    }
    public static void Trigger<T1, T2>(string eventName, T1 arg1, T2 arg2)
    {
        if (eventTDict.TryGetValue(eventName, out var del))
        {
            (del as Action<T1, T2>)?.Invoke(arg1, arg2);
        }
    }

    #endregion
    #region 清理接口
    public static void ClearAll()//清理所有事件监听器，通常在场景切换时调用
    {
        eventDict.Clear();
        eventTDict.Clear();
    }
    public static void ClearEvent(string eventName)//清理指定事件的监听器
    {
        eventDict.Remove(eventName);
        eventTDict.Remove(eventName);
    }
    #endregion
}
