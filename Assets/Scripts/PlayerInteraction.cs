using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    private float interactionDistance = 10.0f;
    private const string ignoreLayerName = "Player";
    private int finalLayerMask;
    private MonoBehaviour lastFrameItem;

    private void Start()
    {
        int layerIndex = LayerMask.NameToLayer(ignoreLayerName);
        finalLayerMask = (layerIndex != -1) ? ~(1 << layerIndex) : ~0;
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    private void Update()
    {
        if (SettingPanel.Instance != null && SettingPanel.Instance.isPanelActive) { ClearHighlight(); return; }

        // 【直接读取 GameData】
        if (GameData.Instance != null) interactionDistance = GameData.Instance.InteractionDistance;

        PerformRaycast();
    }

    private void PerformRaycast()
    {
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, finalLayerMask))
        {
            var img = hit.collider.GetComponentInParent<ImageExhibition>();
            var vid = hit.collider.GetComponentInParent<VideoExhibition>();
            var pnm = hit.collider.GetComponentInParent<PanoramaExhibition>();

            if (img) HandleInteract(img); else if (vid) HandleInteract(vid); else if (pnm) HandleInteract(pnm); else ClearHighlight();
        }
        else { ClearHighlight(); }
    }

    private void HandleInteract(MonoBehaviour item)
    {
        if (lastFrameItem != item)
        {
            ClearHighlight(); lastFrameItem = item;
            if (GameData.Instance && GameData.Instance.HighlightSound)
                AudioSource.PlayClipAtPoint(GameData.Instance.HighlightSound, transform.position, GameData.Instance.ButtonVolume);
            item.SendMessage("SetHighlight", true, SendMessageOptions.DontRequireReceiver);
        }
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) item.SendMessage("StartDisplay", SendMessageOptions.DontRequireReceiver);
    }

    private void ClearHighlight()
    {
        if (lastFrameItem != null) { lastFrameItem.SendMessage("SetHighlight", false, SendMessageOptions.DontRequireReceiver); lastFrameItem = null; }
    }
}