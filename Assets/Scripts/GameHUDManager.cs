using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// HUD praktikum (dibangun lewat kode saat Play):
// - Panel Enemy  : State, HP bar, status Vision, jarak ke Player
// - Panel Player : HP bar + status regenerasi
// - Panel Info   : kontrol dan fitur
// - Label melayang di atas Enemy (state + HP%)
// - Banner Game Over / Enemy Defeated, tekan R untuk restart
public class GameHUDManager : MonoBehaviour
{
    [Header("References (otomatis dicari jika kosong)")]
    [SerializeField] private EnemyFSM enemyFsm;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyPerception enemyPerception;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerAttackTest playerAttack;

    [Header("Floating Label")]
    [SerializeField] private float floatingHeight = 2.6f;

    private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.1f, 0.85f);
    private static readonly Color BarBackColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    private Font font;
    private RectTransform canvasRect;

    // Enemy panel
    private Text enemyStateText;
    private Image enemyStateBadge;
    private RectTransform enemyBarFill;
    private Image enemyBarFillImage;
    private Text enemyHpText;
    private Text visionText;

    // Player panel
    private RectTransform playerBarFill;
    private Image playerBarFillImage;
    private Text playerHpText;
    private Text playerStatusText;

    // Floating label
    private RectTransform floatRoot;
    private Text floatStateText;
    private RectTransform floatBarFill;
    private Image floatBarFillImage;
    private Text floatHpText;

    // Game over
    private GameObject overlay;
    private Text overlayTitle;
    private Text overlaySub;
    private float endTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindFirstObjectByType<GameHUDManager>() != null)
            return;

        if (FindFirstObjectByType<EnemyFSM>() == null)
            return;

        new GameObject("GameHUD").AddComponent<GameHUDManager>();
    }

    private void Awake()
    {
        if (enemyFsm == null) enemyFsm = FindFirstObjectByType<EnemyFSM>();
        if (enemyFsm != null)
        {
            if (enemyHealth == null) enemyHealth = enemyFsm.GetComponent<EnemyHealth>();
            if (enemyPerception == null) enemyPerception = enemyFsm.GetComponent<EnemyPerception>();
        }
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerAttack == null) playerAttack = FindFirstObjectByType<PlayerAttackTest>();

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildCanvas();
    }

    // =====================================
    // BUILD UI
    // =====================================

    private void BuildCanvas()
    {
        GameObject canvasGo = new GameObject("HUD_Canvas");
        canvasGo.transform.SetParent(transform, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasGo.GetComponent<RectTransform>();

        BuildEnemyPanel();
        BuildPlayerPanel();
        BuildInfoPanel();
        BuildFloatingLabel();
        BuildOverlay();
    }

    private void BuildEnemyPanel()
    {
        RectTransform panel = CreatePanel(canvasRect, "EnemyPanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -20f), new Vector2(560f, 190f));

        CreateText(panel, "Title", 0f, 0.78f, 1f, 1f,
            "[ENEMY] ENEMY HEALTH BAR & FSM", 24, new Color(1f, 0.35f, 0.35f),
            TextAnchor.MiddleCenter, FontStyle.Bold);

        RectTransform badge = CreatePanel(panel, "StateBadge",
            new Vector2(0.1f, 0.52f), new Vector2(0.9f, 0.76f), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        enemyStateBadge = badge.GetComponent<Image>();
        enemyStateText = CreateText(badge, "StateText", 0f, 0f, 1f, 1f,
            "STATE: -", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        CreateBar(panel, "HpBar", 0.06f, 0.27f, 0.94f, 0.48f,
            out enemyBarFill, out enemyBarFillImage, out enemyHpText);

        visionText = CreateText(panel, "Vision", 0f, 0.03f, 1f, 0.24f,
            "Vision: -", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
    }

    private void BuildPlayerPanel()
    {
        RectTransform panel = CreatePanel(canvasRect, "PlayerPanel",
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(20f, 20f), new Vector2(480f, 170f));

        CreateText(panel, "Title", 0f, 0.72f, 1f, 1f,
            "[PLAYER] PLAYER HEALTH BAR", 22, new Color(0.35f, 0.75f, 1f),
            TextAnchor.MiddleCenter, FontStyle.Bold);

        CreateBar(panel, "HpBar", 0.06f, 0.38f, 0.94f, 0.64f,
            out playerBarFill, out playerBarFillImage, out playerHpText);

        playerStatusText = CreateText(panel, "Status", 0.06f, 0.06f, 0.94f, 0.32f,
            "STATUS: -", 18, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
    }

    private void BuildInfoPanel()
    {
        RectTransform panel = CreatePanel(canvasRect, "InfoPanel",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-20f, 20f), new Vector2(620f, 250f));

        CreateText(panel, "Title", 0f, 0.82f, 1f, 1f,
            "[INFO] KONTROL & FITUR PRAKTIKUM", 20, new Color(1f, 0.85f, 0.3f),
            TextAnchor.MiddleCenter, FontStyle.Bold);

        string damage = playerAttack != null ? playerAttack.Damage.ToString("0") : "-";
        string reach = playerAttack != null ? playerAttack.AttackDistance.ToString("0.#") : "-";
        string regen = playerHealth != null ? playerHealth.RegenPerSecond.ToString("0.#") : "-";
        string regenDelay = playerHealth != null ? playerHealth.RegenDelay.ToString("0.#") : "-";

        string flee = enemyFsm != null ? enemyFsm.LowHealthThreshold.ToString("0") : "-";
        string healRate = enemyFsm != null ? enemyFsm.HealRate.ToString("0.#") : "-";
        string healTo = enemyFsm != null ? enemyFsm.HealUntilHealth.ToString("0") : "-";
        string wpHeal = enemyFsm != null ? enemyFsm.WaypointHealAmount.ToString("0.#") : "-";

        string info =
            "[W A S D / Panah] : Gerakkan Player\n" +
            "[SPASI] : Serang Enemy (Damage: " + damage + ", jarak <= " + reach + ")\n" +
            "[R] : Restart scene\n" +
            "• Player Regen: +" + regen + " HP/detik jika tidak diserang " + regenDelay + " detik\n" +
            "• Enemy Flee: kabur ke SafePoint jika HP <= " + flee + "\n" +
            "• Enemy Heal: pulih +" + healRate + " HP/detik di SafePoint sampai HP >= " + healTo + "\n" +
            "• Enemy Heal: pulih +" + wpHeal + " HP di setiap waypoint patrol";

        CreateText(panel, "Body", 0.04f, 0.04f, 0.96f, 0.82f,
            info, 16, Color.white, TextAnchor.UpperLeft, FontStyle.Normal);
    }

    private void BuildFloatingLabel()
    {
        floatRoot = CreatePanel(canvasRect, "FloatingEnemyBar",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(190f, 58f));

        floatRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        floatStateText = CreateText(floatRoot, "State", 0f, 0.52f, 1f, 1f,
            "PATROL", 20, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);

        CreateBar(floatRoot, "HpBar", 0.06f, 0.1f, 0.94f, 0.48f,
            out floatBarFill, out floatBarFillImage, out floatHpText);
        floatHpText.fontSize = 14;
    }

    private void BuildOverlay()
    {
        RectTransform banner = CreatePanel(canvasRect, "GameOverOverlay",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -40f), new Vector2(760f, 200f));

        overlayTitle = CreateText(banner, "Title", 0f, 0.4f, 1f, 1f,
            "GAME OVER", 64, Color.red, TextAnchor.MiddleCenter, FontStyle.Bold);

        overlaySub = CreateText(banner, "Sub", 0f, 0.05f, 1f, 0.4f,
            "Tekan [R] untuk restart", 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);

        overlay = banner.gameObject;
        overlay.SetActive(false);
    }

    // =====================================
    // UI HELPERS
    // =====================================

    private RectTransform CreatePanel(RectTransform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;

        Image image = go.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;

        return rt;
    }

    private Text CreateText(RectTransform parent, string name,
        float xMin, float yMin, float xMax, float yMax,
        string content, int size, Color color, TextAnchor anchor, FontStyle style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.fontStyle = style;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return text;
    }

    private void CreateBar(RectTransform parent, string name,
        float xMin, float yMin, float xMax, float yMax,
        out RectTransform fill, out Image fillImage, out Text label)
    {
        RectTransform back = CreatePanel(parent, name,
            new Vector2(xMin, yMin), new Vector2(xMax, yMax), new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);
        back.offsetMin = Vector2.zero;
        back.offsetMax = Vector2.zero;
        back.GetComponent<Image>().color = BarBackColor;

        RectTransform fillRt = CreatePanel(back, "Fill",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
            Vector2.zero, Vector2.zero);
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        fill = fillRt;
        fillImage = fillRt.GetComponent<Image>();
        fillImage.color = Color.green;

        label = CreateText(back, "Label", 0f, 0f, 1f, 1f,
            "", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
    }

    private static void SetBar(RectTransform fill, Image fillImage, Text label,
        float current, float max)
    {
        float pct = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        fill.anchorMax = new Vector2(pct, 1f);
        fillImage.color = HealthColor(pct);
        label.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max) +
                     " HP (" + Mathf.RoundToInt(pct * 100f) + "%)";
    }

    private static Color HealthColor(float pct)
    {
        if (pct > 0.6f) return new Color(0.15f, 0.7f, 0.2f);
        if (pct > 0.3f) return new Color(0.9f, 0.7f, 0.1f);
        return new Color(0.85f, 0.15f, 0.15f);
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        UpdateEnemyPanel();
        UpdatePlayerPanel();
        UpdateOverlay();
    }

    private void LateUpdate()
    {
        UpdateFloatingLabel();
    }

    private void UpdateEnemyPanel()
    {
        if (enemyFsm == null || enemyHealth == null)
            return;

        EnemyFSM.EnemyState state = enemyFsm.CurrentState;

        enemyStateText.text = "STATE: " + state.ToString().ToUpper();
        enemyStateBadge.color = EnemyStateUI.GetStateColor(state);

        SetBar(enemyBarFill, enemyBarFillImage, enemyHpText,
            enemyHealth.CurrentHealth, enemyHealth.MaxHealth);

        if (enemyPerception == null)
            return;

        string vision;

        if (state == EnemyFSM.EnemyState.Dead)
            vision = "<color=#888888>-</color>";
        else if (enemyPerception.CanSeePlayer)
            vision = "<color=#FF5555>DETECTED</color>";
        else if (state == EnemyFSM.EnemyState.Chase)
            vision = "<color=#FFD24D>SEARCHING</color>";
        else
            vision = "<color=#66DD77>HIDDEN</color>";

        float distance = enemyPerception.DistanceToPlayer;

        visionText.text =
            "Vision: " + vision + "  |  Dist: " + distance.ToString("0.0") + "m";
    }

    private void UpdatePlayerPanel()
    {
        if (playerHealth == null)
            return;

        SetBar(playerBarFill, playerBarFillImage, playerHpText,
            playerHealth.CurrentHealth, playerHealth.MaxHealth);

        string status;

        if (playerHealth.IsDead)
            status = "<color=#FF5555>DEAD</color>";
        else if (playerHealth.RecentlyAttacked)
            status = "<color=#FF9955>Under Attack!</color>";
        else if (playerHealth.IsRegenerating)
            status = "<color=#66DD77>Regenerating... +" +
                     playerHealth.RegenPerSecond.ToString("0.#") + " HP/s</color>";
        else if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            status = "Full Health";
        else
            status = "Recovering...";

        playerStatusText.text = "STATUS: " + status;
    }

    private void UpdateFloatingLabel()
    {
        if (enemyFsm == null || enemyHealth == null)
            return;

        Camera cam = Camera.main;

        if (cam == null)
        {
            floatRoot.gameObject.SetActive(false);
            return;
        }

        Vector3 screen = cam.WorldToScreenPoint(
            enemyFsm.transform.position + Vector3.up * floatingHeight
        );

        if (screen.z < 0f)
        {
            floatRoot.gameObject.SetActive(false);
            return;
        }

        floatRoot.gameObject.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screen, null, out Vector2 local
        );

        floatRoot.anchoredPosition = local;

        EnemyFSM.EnemyState state = enemyFsm.CurrentState;

        floatStateText.text = state.ToString().ToUpper();
        floatStateText.color = EnemyStateUI.GetStateColor(state);

        float pct = enemyHealth.MaxHealth > 0f
            ? Mathf.Clamp01(enemyHealth.CurrentHealth / enemyHealth.MaxHealth)
            : 0f;

        floatBarFill.anchorMax = new Vector2(pct, 1f);
        floatBarFillImage.color = HealthColor(pct);
        floatHpText.text = Mathf.RoundToInt(pct * 100f) + "%";
    }

    private void UpdateOverlay()
    {
        bool playerDead = playerHealth != null && playerHealth.IsDead;
        bool enemyDead = enemyHealth != null && enemyHealth.IsDead;

        if (!playerDead && !enemyDead)
        {
            endTime = -1f;
            overlay.SetActive(false);
            return;
        }

        if (endTime < 0f)
            endTime = Time.time;

        // Beri jeda agar transisi ke Dead sempat terlihat sebelum banner muncul.
        if (Time.time - endTime < 1.2f)
            return;

        overlay.SetActive(true);

        if (playerDead)
        {
            overlayTitle.text = "GAME OVER";
            overlayTitle.color = new Color(1f, 0.3f, 0.3f);
            overlaySub.text = "Player mati. Tekan [R] untuk restart";
        }
        else
        {
            overlayTitle.text = "ENEMY DEFEATED";
            overlayTitle.color = new Color(0.4f, 1f, 0.5f);
            overlaySub.text = "Enemy masuk state DEAD. Tekan [R] untuk restart";
        }
    }
}
