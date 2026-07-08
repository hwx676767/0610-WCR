using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MultiplayerUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField ipInput;

    public GameObject mainPanel;
    public GameObject joinPanel;

    private Button hostButton;
    private Button joinButton;
    private Button connectButton;

    private void Start()
    {
        mainPanel.SetActive(true);
        joinPanel.SetActive(false);

        // 自动查找按钮
        hostButton = mainPanel.transform.Find("HostButton").GetComponent<Button>();
        joinButton = mainPanel.transform.Find("JoinButton").GetComponent<Button>();
        connectButton = joinPanel.transform.Find("ConnectButton").GetComponent<Button>();

        // 自动绑定事件
        hostButton.onClick.AddListener(HostGame);
        joinButton.onClick.AddListener(OpenJoinPanel);
        connectButton.onClick.AddListener(ConnectGame);
    }

    public void HostGame()
    {
        Debug.Log("创建房间");

        NetworkManager.singleton.StartHost();
        
        mainPanel.SetActive(false);
        joinPanel.SetActive(false);
        //gameObject.SetActive(false);
    }

    public void OpenJoinPanel()
    {
        Debug.Log("打开加入界面");

        mainPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    public void ConnectGame()
    {
        string ip = ipInput.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("请输入IP地址");
            return;
        }

        Debug.Log("连接服务器: " + ip);

        NetworkManager.singleton.networkAddress = ip;
        NetworkManager.singleton.StartClient();
        
        mainPanel.SetActive(false);
        joinPanel.SetActive(false);
    }

    private void Update()
    {
        if (NetworkClient.isConnected)
        {
            gameObject.SetActive(false);
        }
    }
}