namespace App
{
    using UnityEngine;
    using CoLib;

    /// <summary>
    /// Move attached object between two transforms in given time.
    /// </summary>
    public class MoveBetweenTwoLocations : MonoBehaviour
    {
        public Transform Begin, End;
        public float TransitionTime = 1;

        private readonly CommandQueue _queue = new CommandQueue();

        private void Start()
        {
            var tr = transform;
            var st = Begin;
            var position = st.position;
            tr.position = position;
            tr.rotation = st.rotation;

            _queue.Sequence(
                Cmd.RepeatForever(
                    Cmd.MoveTo(tr, End.position, TransitionTime, Ease.InOutQuad()),
                    Cmd.MoveTo(tr, position, TransitionTime, Ease.InOutQuad())
                )
            );
        }

        private void Update()
        {
            _queue.Update(Time.deltaTime);
        }
    }
}

