using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EditorCoroutine: CustomYieldInstruction
{
    private IEnumerator routine;
    private readonly Stack<IEnumerator> routinesStack = new Stack<IEnumerator>();
    private readonly Action<Exception> exceptionHandler;
    bool stopped = false;

    public override bool keepWaiting
    {
        get
        {
            return !stopped;
        }
    }

    EditorCoroutine(IEnumerator routine, Action<Exception> exceptionHandler)
    {
        this.routine = routine;
        this.exceptionHandler = exceptionHandler;
    }

    public static EditorCoroutine Start(IEnumerator routine, Action<Exception> exceptionHandler = null)
    {
        EditorCoroutine coroutine = new EditorCoroutine(routine,exceptionHandler);
        coroutine.Start();
        return coroutine;
    }

    public static IEnumerator WaitAll(params EditorCoroutine[] coroutines)
    {
        while (coroutines.Any(x=>x.keepWaiting))
            yield return null;
    }

    void Start()
    {
        EditorApplication.update += Update;
    }

    public void Stop()
    {
        if (routinesStack.Count > 0)
        {
            routine = routinesStack.Pop();
            EnumerateNext();
            return;
        }
        
        EditorApplication.update -= Update;
        stopped = true;
    }

    void Update()
    {
        if (routine.Current == null)
        {
            EnumerateNext();
            return;
        }

        Type rt = routine.Current.GetType();
        
        if (routine.Current is IEnumerator)
            UpdateIEnumerator();
        else if (routine.Current is AsyncOperation)
            UpdateAsyncOperation();
        else if (rt == typeof(int))
            UpdateInt();
        else
            EnumerateNext();
    }

    void EnumerateNext()
    {
        try
        {
            if(!routine.MoveNext())
                Stop();
        }
        catch (Exception e)
        {
            routinesStack.Clear();
            Stop();

            if (exceptionHandler != null)
            {
                exceptionHandler(e);
                return;
            }
            
            Console.WriteLine(e);
            throw;
        }
    }

    void UpdateInt()
    {
        var value = (int) routine.Current;
        if (value < 1)
        {
            EnumerateNext();
            return;
        }
        
        routinesStack.Push(routine);
        routine = Enumerable.Range(0, value).Select(_=>0).GetEnumerator();
    }

    void UpdateIEnumerator()
    {
        routinesStack.Push(routine);
        routine = (IEnumerator) routine.Current;
    }

    void UpdateAsyncOperation()
    {
        if (((AsyncOperation)routine.Current).isDone)
            EnumerateNext();
    }
}

#region Extensions

public static class EditorCoroutineEx
{
    public static IEnumerator WaitAll(this IEnumerable<EditorCoroutine> coroutines)
    {
        return EditorCoroutine.WaitAll(coroutines.ToArray());
    }
}

#endregion