using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class EventService : Singleton<EventService>, IDisposable
{
    private readonly Dictionary<Type, List<Delegate>> eventHandlers = new Dictionary<Type, List<Delegate>>();
    private readonly Dictionary<Type, List<EventRecord>> eventHistory = new Dictionary<Type, List<EventRecord>>();
    private readonly Dictionary<object, List<Subscription>> subscriberSubscriptions = new Dictionary<object, List<Subscription>>();

    /// <summary>
    /// 订阅信息
    /// </summary>
    private struct Subscription
    {
        public Type EventType;
        public Delegate Handler;
    }

    public class EventRecord
    {
        public DateTime Timestamp { get; } = DateTime.Now;
        public object Sender { get; }
        public EventArgs Args { get; }
        public string CallerInfo { get; }

        public EventRecord(object sender, EventArgs args, string callerInfo)
        {
            Sender = sender;
            Args = args;
            CallerInfo = callerInfo;
        }

        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss.fff}] {CallerInfo} - Sender: {Sender?.GetType().Name}, Args: {Args}";
        }
    }

    /// <summary>
    /// 订阅事件
    /// </summary>
    /// <typeparam name="T">EventArgs类型</typeparam>
    /// <param name="subscriber">订阅者对象</param>
    /// <param name="handler">事件处理器</param>
    public void Subscribe<T>(object subscriber, Action<object, T> handler) where T : EventArgs
    {
        if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(T);
        
        // 添加到事件处理器列表
        if (!eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers = new List<Delegate>();
            eventHandlers[eventType] = handlers;
        }
        handlers.Add(handler);
        
        // 记录订阅信息
        if (!subscriberSubscriptions.TryGetValue(subscriber, out var subscriptions))
        {
            subscriptions = new List<Subscription>();
            subscriberSubscriptions[subscriber] = subscriptions;
        }
        
        subscriptions.Add(new Subscription { EventType = eventType, Handler = handler });
    }

    /// <summary>
    /// 通过订阅者对象取消所有订阅
    /// </summary>
    public void Unsubscribe(object subscriber)
    {
        if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

        if (subscriberSubscriptions.TryGetValue(subscriber, out var subscriptions))
        {
            foreach (var subscription in subscriptions)
            {
                RemoveHandler(subscription.EventType, subscription.Handler);
            }
            
            subscriberSubscriptions.Remove(subscriber);
        }
    }

    /// <summary>
    /// 取消订阅特定类型的事件
    /// </summary>
    public void Unsubscribe<T>(object subscriber) where T : EventArgs
    {
        if (subscriber == null) throw new ArgumentNullException(nameof(subscriber));

        var eventType = typeof(T);
        
        if (subscriberSubscriptions.TryGetValue(subscriber, out var subscriptions))
        {
            var toRemove = new List<Subscription>();
            
            foreach (var subscription in subscriptions)
            {
                if (subscription.EventType == eventType)
                {
                    RemoveHandler(eventType, subscription.Handler);
                    toRemove.Add(subscription);
                }
            }
            
            // 移除已取消的订阅
            foreach (var subscription in toRemove)
            {
                subscriptions.Remove(subscription);
            }
            
            if (subscriptions.Count == 0)
            {
                subscriberSubscriptions.Remove(subscriber);
            }
        }
    }

    /// <summary>
    /// 从事件处理器列表中移除指定的处理器
    /// </summary>
    private void RemoveHandler(Type eventType, Delegate handler)
    {
        if (eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            
            if (handlers.Count == 0)
            {
                eventHandlers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// 触发事件
    /// </summary>
    public void Publish<T>(object sender, T args) where T : EventArgs
    {
        if (args == null) throw new ArgumentNullException(nameof(args));

        var eventType = typeof(T);
        if (!eventHandlers.TryGetValue(eventType, out var handlers))
            return;

        // 记录事件历史
        var stackTrace = new StackTrace(1, true);
        var callerFrame = stackTrace.GetFrame(0);
        var callerInfo = $"{callerFrame?.GetMethod()?.DeclaringType?.Name}.{callerFrame?.GetMethod()?.Name}";

        if (!eventHistory.TryGetValue(eventType, out var historyList))
        {
            historyList = new List<EventRecord>();
            eventHistory[eventType] = historyList;
        }

        historyList.Add(new EventRecord(sender, args, callerInfo));
        
        // 调用所有处理器
        foreach (var handler in handlers.ToArray()) // 使用ToArray避免在迭代时修改集合
        {
            var action = handler as Action<object, T>;
            action?.Invoke(sender, args);
        }
    }

    /// <summary>
    /// 获取事件触发历史
    /// </summary>
    public IReadOnlyList<EventRecord> GetEventHistory<T>() where T : EventArgs
    {
        return eventHistory.TryGetValue(typeof(T), out var history) 
            ? history.AsReadOnly() 
            : new List<EventRecord>().AsReadOnly();
    }

    /// <summary>
    /// 清空指定类型的事件历史
    /// </summary>
    public void ClearHistory<T>() where T : EventArgs
    {
        eventHistory.Remove(typeof(T));
    }

    /// <summary>
    /// 清空所有事件历史
    /// </summary>
    public void ClearAllHistory()
    {
        eventHistory.Clear();
    }

    public void Dispose()
    {
        
    }
}