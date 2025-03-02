using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    static public string currentCookie = "";
    static public string host = "192.168.8.200"; //Game Host IP
    static public string host_ = "http://192.168.8.200"; // DB Host IP

    [SerializeField] 
    private Button loginButton = null;
    [SerializeField] 
    private Button registerButton = null;

    [SerializeField]
    private Button goBackToMainMenuButton = null;

    [SerializeField] 
    private TMPro.TMP_InputField usernameField = null;
    [SerializeField] 
    private TMPro.TMP_InputField passwordField = null;
    [SerializeField]
    private TMPro.TMP_InputField IPField = null;
    [SerializeField]
    private TMPro.TMP_InputField SQL_IPField = null;

    [SerializeField]
    private Image AmsLogo = null;

    [SerializeField]
    private Image bettlerImage = null;

    [SerializeField]
    private Button playButton = null;

    [SerializeField]
    private Button creditsButton = null;

    [SerializeField]
    private Button quitButton = null;

    //false  = done, true = working
    private bool loginState = false; 
    private bool registerState = false; 

    private bool isInMainMenu = true;

    void Start()
    {
        loginButton.onClick.AddListener(OnClickLogin);
        registerButton.onClick.AddListener(OnClickRegister);
        IPField.onEndEdit.AddListener(OnEndEdit);
        SQL_IPField.onEndEdit.AddListener(OnEndEdit_SQL);

        goBackToMainMenuButton.onClick.AddListener(OnClickGoBack);

        playButton.onClick.AddListener(OnClickPlay);
        creditsButton.onClick.AddListener(OnClickCredits);
        quitButton.onClick.AddListener(OnClickQuit);
    }

    private void OnDestroy()
    {
        loginButton.onClick.RemoveAllListeners();
        registerButton.onClick.RemoveAllListeners();
        IPField.onEndEdit.RemoveAllListeners();
        SQL_IPField.onEndEdit.RemoveAllListeners();

        goBackToMainMenuButton.onClick.RemoveAllListeners();

        playButton.onClick.RemoveAllListeners();
        creditsButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
    }

    void OnSuccessFullLogin(Dictionary<string, string> jsonBody)
    {
        if(jsonBody.TryGetValue("cookie", out string cookie))
        {
            Login.currentCookie = cookie;

            Debug.Log("Got Cookie bruh: " + cookie);

            SceneManager.LoadScene("MainGame");
        }
        else
        {
            Debug.LogError("Failed to retrieve cookie");
        }
    }

    void ShowCredits(bool value)
    {
        if (value)
        {
            loginButton.gameObject.SetActive(false);
            registerButton.gameObject.SetActive(false);
            usernameField.gameObject.SetActive(false);
            passwordField.gameObject.SetActive(false);
            IPField.gameObject.SetActive(false);
            SQL_IPField.gameObject.SetActive(false);

            playButton.gameObject.SetActive(false);
            creditsButton.gameObject.SetActive(false);
            quitButton.gameObject.SetActive(false);
            AmsLogo.gameObject.SetActive(false);

            goBackToMainMenuButton.gameObject.SetActive(true);

            bettlerImage.gameObject.SetActive(true);
        }
        else
            ShowMainMenu(isInMainMenu);
    }

    void ShowMainMenu(bool value)
    {
        isInMainMenu = value;

        bettlerImage.gameObject.SetActive(false);

        loginButton.gameObject.SetActive(!value);
        registerButton.gameObject.SetActive(!value);
        goBackToMainMenuButton.gameObject.SetActive(!value);
        usernameField.gameObject.SetActive(!value);
        passwordField.gameObject.SetActive(!value);
        IPField.gameObject.SetActive(!value);
        SQL_IPField.gameObject.SetActive(!value);

        AmsLogo.gameObject.SetActive(value);
        playButton.gameObject.SetActive(value);
        creditsButton.gameObject.SetActive(value);
        quitButton.gameObject.SetActive(value);
    }


    void OnClickLogin()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        if (isValidData(username, password))
        {
            if(!loginState && !registerState)
            {
                loginState = true;

                StartCoroutine(SendLoginRequest(username, password));
            }
            else
            {
                Debug.Log("An Operation is still processing!");
            }
        }
        else
        {
            Debug.LogError("Entered data was not completed!");
        }
    }

    void OnClickRegister()
    {
        string username = usernameField.text;
        string password = passwordField.text;

        if (isValidData(username, password))
        {
            if (!loginState && !registerState)
            {
                registerState = true;

                StartCoroutine(SendRegisterRequest(username, password));
            }
            else
            {
                Debug.Log("An Operation is still processing!");
            }
        }
        else
        {
            Debug.LogError("Entered data was not completed!");
        }
    }

    private void OnEndEdit(string editedText)
    {
        if (string.IsNullOrEmpty(editedText))
            Login.host = "192.168.8.200";
        else
            Login.host = $"{editedText}";
    }

    private void OnEndEdit_SQL(string editedText)
    {
        if (string.IsNullOrEmpty(editedText))
            Login.host_ = "http://192.168.8.200";
        else
        {
            if(!editedText.Equals("localhost"))
                Login.host_ = $"http://{editedText}";
            else
                Login.host_ = $"{editedText}";
        }
    }

    private IEnumerator<UnityWebRequestAsyncOperation> SendLoginRequest(string username, string password)
    {
        WWWForm form = new WWWForm();

        form.AddField("name", username);
        form.AddField("pw", password);

        using (UnityWebRequest request = UnityWebRequest.Post($"{host_}/Login.php", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);

                    OnSuccessFullLogin(response);
                }
                catch (JsonException e)
                {
                    Debug.LogError("Failed to parse JSON: " + e.Message);
                }
            }
            else
            {

                try
                {
                    var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);

                    Debug.LogError("Login failed: " + request.error);
                }
                catch (JsonException e)
                {
                    Debug.LogError("Failed to parse JSON: " + e.Message);
                }
            }

            loginState = false;
        }
    }

    private IEnumerator<UnityWebRequestAsyncOperation> SendRegisterRequest(string username, string password)
    {
        WWWForm form = new WWWForm();

        form.AddField("name", username);
        form.AddField("pw", password);

        using (UnityWebRequest request = UnityWebRequest.Post($"{host_}/Register.php", form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);

                Debug.Log("Registered Successfully!");

                OnSuccessFullLogin(response);
            }
            else
            {
                var response = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);

                Debug.LogError("Registration failed: " + response["message"]);
            }

            registerState = false;
        }
    }

    void OnClickGoBack()
    {
        ShowMainMenu(true);
    }

    void OnClickPlay()
    {
        ShowMainMenu(false);
    }

    void OnClickCredits()
    {
        ShowCredits(true);
    }

    void OnClickQuit()
    {
        CleanUpAndQuit(this);
    }

    static void CleanUpAndQuit(Login loginInstance)
    {
        Application.Quit();
    }

    private bool isValidData(string username, string password)
    {
        return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
    }
}
