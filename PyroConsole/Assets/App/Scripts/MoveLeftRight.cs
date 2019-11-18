using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CoLib;

public class MoveLeftRight : MonoBehaviour
{
    public Transform Begin, End;
    public float TransitionTime = 1;
    
    private CoLib.CommandQueue _queue = new CommandQueue();
    
    void Start()
    {
        var tr = transform;
        var st = Begin;
        tr.position = st.position;
        tr.rotation = st.rotation;
        
        _queue.Sequence(
            Cmd.RepeatForever(
                Cmd.MoveTo(tr, End.position, TransitionTime, Ease.InOutQuad()),
                Cmd.MoveTo(tr, st.position, TransitionTime, Ease.InOutQuad())
            )
        );
    }
    
    void Update()
    {
        _queue.Update(Time.deltaTime);
    }
}
