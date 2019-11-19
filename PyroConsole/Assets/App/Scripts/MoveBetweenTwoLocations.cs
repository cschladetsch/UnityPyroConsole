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

        public Pyro.Network.IClient Remote;

        private void Start()
        {
            if (!Main.Instance.IsServer)
            {
                return;
            }

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

        private void Connect()
        {
        }

        float _nextConnectAttempt = 1;

        private void Update()
        {
            _queue.Update(Time.deltaTime);

            if (!Main.Instance.IsServer && Time.time > _nextConnectAttempt && Remote == null)
            {
                _nextConnectAttempt = Time.time + 1;
                Main.Instance.Peer.Execute("Connected(\"/SharedCube/Cube\", 1)");
                Main.Instance.Peer.OnReceivedResponse += Peer_OnReceivedResponse;
            }
        }

        private void Peer_OnReceivedResponse(Pyro.Network.IClient client, string text)
        {
            Main.Instance.Peer.OnReceivedResponse -= Peer_OnReceivedResponse;
        }
    }
}

