using UnityEngine;

// Mewarnai badan Enemy sesuai state aktif sebagai visual debugging FSM.
// Panel HUD dan label melayang ditangani oleh GameHUDManager.
public class EnemyStateUI : MonoBehaviour
{
    [SerializeField] private EnemyFSM fsm;
    [SerializeField] private Renderer bodyRenderer;

    private EnemyFSM.EnemyState lastState;
    private bool colorApplied;

    private void Awake()
    {
        if (fsm == null)
            fsm = GetComponent<EnemyFSM>();

        if (bodyRenderer == null)
            bodyRenderer = GetComponent<Renderer>();
    }

    private void Update()
    {
        if (bodyRenderer == null || fsm == null)
            return;

        if (colorApplied && fsm.CurrentState == lastState)
            return;

        lastState = fsm.CurrentState;
        colorApplied = true;
        bodyRenderer.material.color = GetStateColor(lastState);
    }

    public static Color GetStateColor(EnemyFSM.EnemyState state)
    {
        switch (state)
        {
            case EnemyFSM.EnemyState.Patrol: return new Color(0.2f, 0.8f, 0.3f);
            case EnemyFSM.EnemyState.Chase:  return new Color(1f, 0.6f, 0.1f);
            case EnemyFSM.EnemyState.Attack: return new Color(0.9f, 0.1f, 0.1f);
            case EnemyFSM.EnemyState.Flee:   return new Color(0.2f, 0.5f, 1f);
            case EnemyFSM.EnemyState.Heal:   return new Color(0.2f, 0.85f, 0.8f);
            default:                         return new Color(0.3f, 0.3f, 0.3f);
        }
    }
}
