using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using TMPro;
using System.Collections;

public class AuthManager : MonoBehaviour
{
    [Header("Firebase")]
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("UI References")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private GameObject messagePanel;

    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginEmail;
    [SerializeField] private TMP_InputField loginPassword;

    [Header("Register UI")]
    [SerializeField] private TMP_InputField registerEmail;
    [SerializeField] private TMP_InputField registerPassword;
    [SerializeField] private TMP_InputField registerConfirmPassword;

    [Header("Message UI")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float messageDisplayTime = 2f;

    [Header("Scene")]
    [SerializeField] private string mainSceneName = "MainScene";

    private void Start()
    {
        InitializeFirebase();
        ShowLoginPanel();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase 초기화 성공");
            }
            else
            {
                Debug.LogError($"Firebase 초기화 실패: {task.Result}");
                ShowMessage("Firebase 초기화에 실패했습니다.", false);
            }
        });
    }

    // 회원가입 버튼 클릭
    public void OnRegisterButtonClick()
    {
        ShowRegisterPanel();
    }

    // 회원가입 수행
    public void RegisterUser()
    {
        string email = registerEmail.text.Trim();
        string password = registerPassword.text;
        string confirmPassword = registerConfirmPassword.text;

        // 입력 검증
        if (string.IsNullOrEmpty(email))
        {
            ShowMessage("이메일을 입력해주세요.", false);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowMessage("비밀번호를 입력해주세요.", false);
            return;
        }

        if (password != confirmPassword)
        {
            ShowMessage("비밀번호가 일치하지 않습니다.", false);
            return;
        }

        if (password.Length < 6)
        {
            ShowMessage("비밀번호는 최소 6자 이상이어야 합니다.", false);
            return;
        }

        // Firebase 회원가입
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                ShowMessage("회원가입이 취소되었습니다.", false);
                return;
            }

            if (task.IsFaulted)
            {
                string errorMessage = GetFirebaseErrorMessage(task.Exception);
                ShowMessage(errorMessage, false);
                return;
            }

            // 회원가입 성공
            user = task.Result.User;
            Debug.Log($"회원가입 성공: {user.Email}");

            MainThreadDispatcher.RunOnMainThread(() =>
            {
                ShowMessage("회원가입이 완료되었습니다!", true);
                StartCoroutine(TransitionToLogin());
            });
        });
    }

    // 로그인 화면으로 전환
    private IEnumerator TransitionToLogin()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        ClearInputFields();
        ShowLoginPanel();
    }

    // 로그인 수행
    public void LoginUser()
    {
        string email = loginEmail.text.Trim();
        string password = loginPassword.text;

        // 입력 검증
        if (string.IsNullOrEmpty(email))
        {
            ShowMessage("이메일을 입력해주세요.", false);
            return;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowMessage("비밀번호를 입력해주세요.", false);
            return;
        }

        // Firebase 로그인
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                ShowMessage("로그인이 취소되었습니다.", false);
                return;
            }

            if (task.IsFaulted)
            {
                string errorMessage = GetFirebaseErrorMessage(task.Exception);
                ShowMessage(errorMessage, false);
                return;
            }

            // 로그인 성공
            user = task.Result.User;
            Debug.Log($"로그인 성공: {user.Email}");

            MainThreadDispatcher.RunOnMainThread(() =>
            {
                ShowMessage("로그인 성공!", true);
                StartCoroutine(TransitionToMainScene());
            });
        });
    }

    // 메인 씬으로 전환
    private IEnumerator TransitionToMainScene()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        SceneManager.LoadScene(mainSceneName);
    }

    // Firebase 에러 메시지 변환
    private string GetFirebaseErrorMessage(System.AggregateException exception)
    {
        FirebaseException firebaseEx = exception.GetBaseException() as FirebaseException;

        if (firebaseEx == null)
            return "알 수 없는 오류가 발생했습니다.";

        AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

        switch (errorCode)
        {
            case AuthError.InvalidEmail:
                return "이메일 형식이 올바르지 않습니다.";
            case AuthError.EmailAlreadyInUse:
                return "이미 사용 중인 이메일입니다.";
            case AuthError.WeakPassword:
                return "비밀번호가 너무 약합니다. (최소 6자)";
            case AuthError.WrongPassword:
                return "비밀번호가 올바르지 않습니다.";
            case AuthError.UserNotFound:
                return "등록되지 않은 이메일입니다.";
            case AuthError.TooManyRequests:
                return "너무 많은 요청이 발생했습니다. 잠시 후 다시 시도해주세요.";
            case AuthError.NetworkRequestFailed:
                return "네트워크 연결을 확인해주세요.";
            default:
                return $"오류가 발생했습니다: {errorCode}";
        }
    }

    // 메시지 표시
    private void ShowMessage(string message, bool isSuccess)
    {
        MainThreadDispatcher.RunOnMainThread(() =>
        {
            messageText.text = message;
            messageText.color = isSuccess ? Color.green : Color.red;
            messagePanel.SetActive(true);
            StartCoroutine(HideMessageAfterDelay());
        });
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayTime);
        messagePanel.SetActive(false);
    }

    // UI 패널 전환
    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        messagePanel.SetActive(false);
    }

    private void ShowRegisterPanel()
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        messagePanel.SetActive(false);
        ClearInputFields();
    }

    public void BackToLogin()
    {
        ShowLoginPanel();
        ClearInputFields();
    }

    // 입력 필드 초기화
    private void ClearInputFields()
    {
        loginEmail.text = "";
        loginPassword.text = "";
        registerEmail.text = "";
        registerPassword.text = "";
        registerConfirmPassword.text = "";
    }

    // 로그아웃 (다른 씬에서 사용)
    public void SignOut()
    {
        if (auth != null)
        {
            auth.SignOut();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}