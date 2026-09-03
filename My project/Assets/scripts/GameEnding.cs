using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class GameEnding : MonoBehaviour
{
    [Header("Ending Text")]
    [SerializeField] private string title = "The Roots Have Awakened";

    [TextArea(2, 5)]
    [SerializeField] private string message =
        "You reached the heart of the roots.";

    [Header("Controls")]
    [SerializeField] private bool allowRestart = true;
    [SerializeField] private bool allowQuit = true;

    private bool endingActive;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (endingActive)
            return;

        PlayerMovement player =
            other.GetComponentInParent<PlayerMovement>();

        if (player == null)
            return;

        endingActive = true;

        PlayerInput playerInput =
            player.GetComponent<PlayerInput>();

        Rigidbody playerBody =
            player.GetComponent<Rigidbody>();

        if (playerInput != null)
        {
            playerInput.DeactivateInput();
        }

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            playerBody.isKinematic = true;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!endingActive || Keyboard.current == null)
            return;

        if (
            allowRestart &&
            Keyboard.current.rKey.wasPressedThisFrame
        )
        {
            RestartGame();
        }

        if (
            allowQuit &&
            Keyboard.current.escapeKey.wasPressedThisFrame
        )
        {
            QuitGame();
        }
    }

    private void OnGUI()
    {
        if (!endingActive)
            return;

        float panelWidth = Mathf.Min(700f, Screen.width - 40f);
        Rect panel = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - 280f) * 0.5f,
            panelWidth,
            280f
        );

        GUI.Box(panel, GUIContent.none);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            wordWrap = true
        };

        GUI.Label(
            new Rect(panel.x + 20f, panel.y + 25f, panel.width - 40f, 65f),
            title,
            titleStyle
        );

        GUI.Label(
            new Rect(panel.x + 35f, panel.y + 95f, panel.width - 70f, 90f),
            message,
            messageStyle
        );

        string controls = allowRestart ? "R - Restart" : "";

        if (allowQuit)
        {
            controls += allowRestart
                ? "     Escape - Quit"
                : "Escape - Quit";
        }

        GUI.Label(
            new Rect(panel.x + 20f, panel.y + 205f, panel.width - 40f, 45f),
            controls,
            messageStyle
        );
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (endingActive)
        {
            Time.timeScale = 1f;
        }
    }
}
