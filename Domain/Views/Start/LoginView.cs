using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePack;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginView : BaseView
{
    private Transform loginTransform;
    private Transform registerTransform;
    private TMP_InputField loginUsernameInput;
    private TMP_InputField loginPasswordInput;
    private TMP_InputField registerUsernameInput;
    private TMP_InputField registerPasswordInput;
    private TMP_InputField registerRePasswordInput;
    private TMP_InputField registerCodeInput;
    private Button sendCodeButton;
    private Button loginLoginButton;
    private Button loginRegisterButton;
    private Button registerLoginButton;
    private Button registerRegisterButton;
    private Button quitGameButton;
    
    private LoginController loginController;

    protected override void Awake()
    {
        base.Awake();
        loginTransform = transform.Find("Login");
        loginUsernameInput = loginTransform.Find("UsernameInput").GetComponent<TMP_InputField>();
        loginPasswordInput = loginTransform.Find("PasswordInput").GetComponent<TMP_InputField>();
        loginLoginButton = loginTransform.Find("LoginButton").GetComponent<Button>();
        loginRegisterButton = loginTransform.Find("RegisterButton").GetComponent<Button>();
        
        registerTransform = transform.Find("Register");
        registerUsernameInput = registerTransform.Find("UsernameInput").GetComponent<TMP_InputField>();
        registerPasswordInput = registerTransform.Find("PasswordInput").GetComponent<TMP_InputField>();
        registerRePasswordInput = registerTransform.Find("RePasswordInput").GetComponent<TMP_InputField>();
        registerCodeInput = registerTransform.Find("CodeInput").GetComponent<TMP_InputField>();
        registerRegisterButton = registerTransform.Find("RegisterButton").GetComponent<Button>();
        registerLoginButton = registerTransform.Find("LoginButton").GetComponent<Button>();
        sendCodeButton = registerTransform.Find("SendCodeButton").GetComponent<Button>();
        
        quitGameButton = transform.Find("QuitGameButton").GetComponent<Button>();
        
        loginRegisterButton.onClick.AddListener(() =>
        {
            loginTransform.gameObject.SetActive(false);
            registerTransform.gameObject.SetActive(true);
        });
        
        registerLoginButton.onClick.AddListener(() =>
        {
            registerTransform.gameObject.SetActive(false);
            loginTransform.gameObject.SetActive(true);
        });
        
        registerRegisterButton.onClick.AddListener(OnRegisterClick);
        loginLoginButton.onClick.AddListener(OnLoginClick);
        sendCodeButton.onClick.AddListener(OnSendCodeClick);
        quitGameButton.onClick.AddListener(OnQuitGameClick);
        
        loginTransform.gameObject.SetActive(true);
        registerTransform.gameObject.SetActive(false);

        loginController = new LoginController(this);

    }

    private void OnLoginClick()
    {
        if (string.IsNullOrEmpty(loginUsernameInput.text) || string.IsNullOrEmpty(loginPasswordInput.text))
        {
            Debug.Log("用户名或密码不能为空");
            return;
        }

        var payload = new ClientPlayerLogin
        {
            Username = loginUsernameInput.text,
            Password = loginPasswordInput.text
        };
        GameClient.Instance.Send(Protocol.CS_Login, payload);
    }
    

    public void Register(string username)
    {
        loginTransform.gameObject.SetActive(true);
        registerTransform.gameObject.SetActive(false);
        loginUsernameInput.text = username;
        registerPasswordInput.text = "";
        registerRePasswordInput.text = "";
        registerCodeInput.text = "";
        registerUsernameInput.text = "";
    }

    private void OnRegisterClick()
    {
        if (string.IsNullOrEmpty(registerUsernameInput.text) || string.IsNullOrEmpty(registerPasswordInput.text))
        {
            Debug.Log("用户名或密码不能为空");
            return;
        }
        if (string.IsNullOrEmpty(registerRePasswordInput.text))
        {
            Debug.Log("请重复输入密码");
            return;
        }
        if (!registerRePasswordInput.text.Equals(registerPasswordInput.text))
        {
            Debug.Log("两次输入密码不一致");
            return;
        }

        var playerRegister = new ClientPlayerRegister
        {
            Username = registerUsernameInput.text,
            Password = registerPasswordInput.text,
            RePassword = registerRePasswordInput.text
        };
        
        GameClient.Instance.Send(Protocol.CS_Register, playerRegister);
    }
    
    
    private void OnQuitGameClick()
    {
        Application.Quit();
    }

    private void OnSendCodeClick()
    {
        
    }
}
