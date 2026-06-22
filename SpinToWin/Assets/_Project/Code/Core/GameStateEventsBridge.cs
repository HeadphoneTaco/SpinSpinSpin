using CoreUtils;
using CoreUtils.GameEvents;
using UnityEngine;

namespace _Project.Code.Core {
    /// <summary>
    ///     Bridges the CoreUtils <see cref="StateMachine" /> to ScriptableObject GameEvents:
    ///     raises <see cref="stateEntered" /> / <see cref="stateExited" /> (carrying the state
    ///     name) whenever a state is entered or exited. Consumers subscribe to those event
    ///     assets instead of referencing the StateMachine directly.
    ///     Put this next to the StateMachine and assign all three references.
    /// </summary>
    public class GameStateEventsBridge : MonoBehaviour {
        [SerializeField, AutoFill] private StateMachine stateMachine;
        [SerializeField] private GameEventString stateEntered;
        [SerializeField] private GameEventString stateExited;

        private void OnEnable() {
            if (stateMachine == null) {
                stateMachine = GetComponent<StateMachine>();
            }

            if (stateMachine != null) {
                stateMachine.OnStateEntered += HandleStateEntered;
                stateMachine.OnStateExited += HandleStateExited;

                // The StateMachine sets its default state in Awake, before we subscribed,
                // so re-raise the current state once so nothing misses the opening state.
                if (stateMachine.CurrentState != null) {
                    HandleStateEntered(stateMachine.CurrentState);
                }
            }
        }

        private void OnDisable() {
            if (stateMachine != null) {
                stateMachine.OnStateEntered -= HandleStateEntered;
                stateMachine.OnStateExited -= HandleStateExited;
            }
        }

        private void HandleStateEntered(GameObject state) {
            if (stateEntered != null) {
                stateEntered.Raise(state.name);
            }
        }

        private void HandleStateExited(GameObject state) {
            if (stateExited != null) {
                stateExited.Raise(state.name);
            }
        }
    }
}
