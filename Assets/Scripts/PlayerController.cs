using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : UserData
{
    public FixedJoystick joystick;
    public CharacterStatus characterStatus = CharacterStatus.idling;
    public Scoring scoring;
    public string answer = string.Empty;
    public bool IsCorrect = false;
    public bool IsTriggerToNextQuestion = false;
    public bool IsCheckedAnswer = false;
    public CanvasGroup answerBoxCg;
    public Image answerBoxFrame;
    [HideInInspector]
    public Transform characterTransform;
    public float limitMovingXOffsetPercentage = 0.95f;
    public float limitMovingYOffsetPercentage = 0.95f;
    [HideInInspector]
    public Canvas characterCanvas = null;
    public Vector3 startPosition = Vector3.zero;
    public int characterOrder = 11;
    private CharacterAnimation characterAnimation = null;
    private TextMeshProUGUI answerBox = null;
    public List<Cell> collectedCell = new List<Cell>();
    public float countGetAnswerAtStartPoints = 2f;
    private float countAtStartPoints = 0f;

    public RectTransform rectTransform = null;
    public float rotationSpeed = 200f; // Speed of rotation
    public float moveSpeed = 5f; // Speed of movement
    //public Ease movingEase = Ease.Linear;
    private Rigidbody2D rb = null;
   // public bool isRotating = true;
    public Vector3 playerCurrentPosition = Vector3.zero;
    private float randomDirection;
    private Vector2 moveDirection;
    public float reduceBaseFactor = 0.93f;
    private float reducedFactor = 0f;
    public CanvasGroup bornParticle;
    public GameObject playerAppearEffect;
    public GameObject[] answerParticles;
    public float resetCount = 5.0f;

    public bool IsFlipped
    {
        get {
            return this.UserId < 2 ? false : true;
        }
    }

    public void Init(CharacterSet characterSet = null, Sprite[] defaultAnswerBoxes = null, Vector3 startPos = default)
    {
        if(LoaderConfig.Instance.gameSetup.playersMovingSpeed > 0f)
        {
            this.moveSpeed = LoaderConfig.Instance.gameSetup.playersMovingSpeed;
        }

        if(LoaderConfig.Instance.gameSetup.playersRotationSpeed > 0f)
        {
            this.rotationSpeed = LoaderConfig.Instance.gameSetup.playersRotationSpeed;
        }

        for (int i=0; i < this.answerParticles.Length; i++)
        {
            if(this.answerParticles[i] != null)
            {
                if(i == this.UserId)
                {
                    this.answerParticles[i].SetActive(true);
                }
                else
                {
                    this.answerParticles[i].SetActive(false);
                }
            }
        }
        this.GetComponent<CircleCollider2D>().enabled = true;
        SetUI.Set(this.bornParticle, true, 1f);
        this.characterStatus = CharacterStatus.born;
        this.transform.DOScale(this.IsFlipped ? -1f : 1f, 1f).OnComplete(()=>
        {
            this.characterStatus = CharacterStatus.idling;
            this.playerAppearEffect.SetActive(false);
            SetUI.Set(this.bornParticle, false, 1f);
        });
        this.rb = GetComponent<Rigidbody2D>();
        this.SetRandomRotationDirection();

        this.countAtStartPoints = this.countGetAnswerAtStartPoints;
        this.updateRetryTimes(false);
        this.startPosition = startPos;
        this.characterTransform = this.transform;
        this.characterTransform.localPosition = this.startPosition;
        this.characterCanvas = this.GetComponent<Canvas>();
        this.characterCanvas.sortingOrder = this.characterOrder;
        this.characterAnimation = this.GetComponent<CharacterAnimation>();
        this.characterAnimation.characterSet = characterSet;

        if(this.answerBoxCg != null ) {
            this.answerBoxCg.transform.localScale = Vector3.zero;
            this.answerBoxCg.transform.localPosition = this.IsFlipped ? new Vector2(60f, -60f) : new Vector2(60f, -60f);
            SetUI.SetScale(this.answerBoxCg, false);
            this.answerBox = this.answerBoxCg.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (this.joystick == null)
        {
            this.joystick = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "-controller").GetComponent<FixedJoystick>();
        }

        if (this.PlayerIcons[0] == null)
        {
            this.PlayerIcons[0] = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Icon").GetComponent<PlayerIcon>();
        }

        if (this.scoring.scoreTxt == null)
        {
            this.scoring.scoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_Score").GetComponent<TextMeshProUGUI>();
        }

        if (this.scoring.answeredEffectTxt == null)
        {
            this.scoring.answeredEffectTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_AnswerScore").GetComponent<TextMeshProUGUI>();
        }

        if (this.scoring.resultScoreTxt == null)
        {
            this.scoring.resultScoreTxt = GameObject.FindGameObjectWithTag("P" + this.RealUserId + "_ResultScore").GetComponent<TextMeshProUGUI>();
        }

        this.scoring.init();
        this.reducedFactor = this.reduceBaseFactor;
    }

    void updateRetryTimes(bool deduct = false)
    {
        if (deduct)
        {
            if (this.Retry > 0)
            {
                this.Retry--;
            }

            /*if (this.bloodController != null)
            {
                this.bloodController.setBloods(false);
            }*/
        }
        else
        {
            this.NumberOfRetry = LoaderConfig.Instance.gameSetup.retry_times;
            this.Retry = this.NumberOfRetry;
        }
    }

    public void updatePlayerIcon(bool _status = false, string _playerName = "", Sprite _icon = null)
    {
        for (int i = 0; i < this.PlayerIcons.Length; i++)
        {
            if (this.PlayerIcons[i] != null)
            {
                this.PlayerColor = this.characterAnimation.characterSet.playerColor;
                this.PlayerIcons[i].playerColor = this.characterAnimation.characterSet.playerColor;
                this.PlayerIcons[i].SetStatus(_status, _playerName, _icon);
            }
        }

    }


    string CapitalizeFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return str; // Return if the string is empty or null
        return char.ToUpper(str[0]) + str.Substring(1).ToLower();
    }

    public void checkAnswer(int currentTime, Action onCompleted = null)
    {
        var currentQuestion = QuestionController.Instance?.currentQuestion;
        var lowerQIDAns = currentQuestion.correctAnswer.ToLower();

        if (!this.IsCheckedAnswer)
        {
            this.IsCheckedAnswer = true;
            var loader = LoaderConfig.Instance;
            int eachQAScore = currentQuestion.qa.score.full == 0 ? 10 : currentQuestion.qa.score.full;
            int currentScore = this.Score;

            int resultScore = this.scoring.score(this.answer, currentScore, lowerQIDAns, eachQAScore);
            this.Score = resultScore;
            this.IsCorrect = this.scoring.correct;
            StartCoroutine(this.showAnswerResult(this.scoring.correct,()=>
            {
                if (this.UserId == 0 && loader != null && loader.apiManager.IsLogined) // For first player
                {
                    float currentQAPercent = 0f;
                    int correctId = 0;
                    float score = 0f;
                    float answeredPercentage;
                    int progress = (int)((float)currentQuestion.answeredQuestion / QuestionManager.Instance.totalItems * 100);

                    if (this.answer == lowerQIDAns)
                    {
                        if (this.CorrectedAnswerNumber < QuestionManager.Instance.totalItems)
                            this.CorrectedAnswerNumber += 1;

                        correctId = 2;
                        score = eachQAScore; // load from question settings score of each question

                        LogController.Instance?.debug("Each QA Score!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + eachQAScore + "______answer" + this.answer);
                        currentQAPercent = 100f;
                    }
                    else
                    {
                        if (this.CorrectedAnswerNumber > 0)
                        {
                            this.CorrectedAnswerNumber -= 1;
                        }
                    }

                    if (this.CorrectedAnswerNumber < QuestionManager.Instance.totalItems)
                    {
                        answeredPercentage = this.AnsweredPercentage(QuestionManager.Instance.totalItems);
                    }
                    else
                    {
                        answeredPercentage = 100f;
                    }

                    loader.SubmitAnswer(
                               currentTime,
                               this.Score,
                               answeredPercentage,
                               progress,
                               correctId,
                               currentTime,
                               currentQuestion.qa.qid,
                               currentQuestion.correctAnswerId,
                               this.CapitalizeFirstLetter(this.answer),
                               currentQuestion.correctAnswer,
                               score,
                               currentQAPercent,
                               onCompleted
                               );
                }
                else
                {
                   onCompleted?.Invoke();
                }
            }));
        }
    }

    public void resetRetryTime()
    {
        this.scoring.resetText();
        this.updateRetryTimes(false);
       // this.bloodController.setBloods(true);
        this.IsTriggerToNextQuestion = false;
    }

    public IEnumerator showAnswerResult(bool correct, Action onCompleted = null)
    {
        float delay = 2f;
        if (correct)
        {
            GameController.Instance?.PrepareNextQuestion();
            LogController.Instance?.debug("Add marks" + this.Score);
            GameController.Instance?.setGetScorePopup(true);
            AudioController.Instance?.PlayAudio(1);
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setGetScorePopup(false);
            GameController.Instance?.UpdateNextQuestion();
        }
        else
        {
            GameController.Instance?.setWrongPopup(true);
            AudioController.Instance?.PlayAudio(2);
            this.updateRetryTimes(true);
            yield return new WaitForSeconds(delay);
            GameController.Instance?.setWrongPopup(false);
            if (this.Retry <= 0)
            {
                this.IsTriggerToNextQuestion = true;
            }
        }
        this.scoring.correct = false;

        onCompleted?.Invoke();
    }

    public void characterReset(Vector3 newStartPostion)
    {
        this.randomDirection = UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
        this.startPosition = newStartPostion;
        this.characterCanvas.sortingOrder = this.characterOrder;
        this.characterTransform.localPosition = this.startPosition;
        this.collectedCell.Clear();
    }

    void FixedUpdate()
    {
        /*if(this.rectTransform != null)
        {
            switch (this.characterStatus)
            {
                case CharacterStatus.born:
                case CharacterStatus.idling:
                    this.StopCharacter();
                    return;
                case CharacterStatus.moving:

                    break;
                case CharacterStatus.nextQA:
                    this.HoldCharacter();
                    break;
            }

        }*/

        if (this.joystick == null || this.playerAppearEffect.activeInHierarchy) return;
        Vector2 direction = Vector2.zero;

        if (this.rectTransform != null)
        {
            direction = new Vector2(this.joystick.Horizontal, this.joystick.Vertical);

            if (this.UserId == 0) // only player one can use
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    direction.y = 1;
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    direction.y = -1;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    direction.x = -1;
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    direction.x = 1;
                }
            }

            if (direction.magnitude > 1)
            {
                direction.Normalize();
            }
            Vector3 newPosition = this.characterTransform.position + (Vector3)direction * this.moveSpeed * Time.deltaTime;
            newPosition.x = Mathf.Clamp(newPosition.x, 
                                        -Camera.main.orthographicSize * this.limitMovingXOffsetPercentage, 
                                        Camera.main.orthographicSize * this.limitMovingXOffsetPercentage);
            newPosition.y = Mathf.Clamp(newPosition.y, 
                                        -Camera.main.orthographicSize * (this.limitMovingYOffsetPercentage - 0.4f), 
                                        Camera.main.orthographicSize * this.limitMovingYOffsetPercentage);

            this.characterTransform.position = newPosition;
            //this.characterTransform.DOMove(newPosition, 0.05f).SetEase(this.movingEase);
            this.rectTransform.localScale = new Vector3(direction.x > 0 ? -0.65f : 0.65f, 0.65f, 1);
        }
        else
        {
            this.characterTransform.localPosition = new Vector2(this.characterTransform.localPosition.x, 220f);
        }

        if (direction.magnitude > 0.1f)
        {
            if (this.characterStatus != CharacterStatus.moving)
            {
                this.characterStatus = CharacterStatus.moving;
            }
        }
        else
        {
            if (this.characterStatus != CharacterStatus.idling)
            {
                this.characterStatus = CharacterStatus.idling;
            }
        }
    }

    void SetRandomRotationDirection()
    {
        if(this.rectTransform == null) this.rectTransform = this.GetComponent<RectTransform>();
        this.rb = GetComponent<Rigidbody2D>();
        this.rb.gravityScale = 0;
        this.randomDirection =  UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
    }


    public void playerReset(Vector3 newStartPostion)
    {
        this.characterStatus = CharacterStatus.born;
        SetUI.Set(this.bornParticle, true, 1f);
        this.transform.DOScale(0f, 0f);
        if(this.playerAppearEffect != null) this.playerAppearEffect.SetActive(true);
        this.GetComponent<CircleCollider2D>().enabled = true;

        this.transform.DOScale(this.IsFlipped ? -1f: 1f, 1f).OnComplete(() =>
        {
            if(this.characterStatus != CharacterStatus.nextQA)
            {
                this.characterStatus = CharacterStatus.idling;
                this.playerAppearEffect.SetActive(false);
                SetUI.Set(this.bornParticle, false, 1f);
            }
        });
        this.deductAnswer();
        this.setAnswer(null);
        this.characterReset(newStartPostion);
        this.IsCheckedAnswer = false;
        this.IsCorrect = false;
        this.resetCount = 2.0f;
    }

    public void setAnswer(Cell cell)
    {
        if (cell == null)
        {
            this.answer = "";
            SetUI.SetScale(this.answerBoxCg, false);
        }
        else
        {
            string content = cell.content.text;
            var gridManager = GameController.Instance.gridManager;
            if (gridManager.isMCType) { 
                this.answer = content;
            }
            else
            {
                GameController.Instance.updateQAFillInBlank(cell, 
                ()=>
                {
                    //Correct
                    AudioController.Instance?.PlayAudio(9);
                    gridManager.removeCollectedCellId(cell);
                    cell.setGetWordEffect(true,
                                          GameController.Instance.FlyingPosition(this.UserId < 2 ? 0: 1),
                                          ()=>
                                          {
                                              GameController.Instance.UpdateDisplayedQuestion(content);
                                          });
                    this.collectedCell.Add(cell);
                },
                ()=>
                {
                    //Wrong
                    AudioController.Instance?.PlayAudio(10);
                    LogController.Instance.debug("wrong letter!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    gridManager.updateNewWordPosition(cell);
                }
                );
            }
        }
    }

    public void correctAction(Cell cell)
    {
        this.answer += cell.content.text;
        //SetUI.SetScale(this.answerBoxCg, true, 1f, 0.5f, Ease.OutElastic);
        if (this.answerBox != null)
            this.answerBox.text = this.answer;
    }

    public void finishedAction()
    {
        LogController.Instance.debug("finished spelling!!!!!!!!!!!!!!!!!!!!!!");
        var gameTimer = GameController.Instance.gameTimer;
        int currentTime = Mathf.FloorToInt(((gameTimer.gameDuration - gameTimer.currentTime) / gameTimer.gameDuration) * 100);
        this.checkAnswer(currentTime);
    }

    public void autoDeductAnswer()
    {
        if(this.collectedCell.Count > 0) {
            if (this.countAtStartPoints > 0f)
            {
                this.countAtStartPoints -= Time.deltaTime;
            }
            else
            {
                this.deductAnswer();
                this.countAtStartPoints = this.countGetAnswerAtStartPoints;
            }
        }
        else
        {
            this.countAtStartPoints = this.countGetAnswerAtStartPoints;
        }
    }

    public void deductAnswer()
    {
       var gridManager = GameController.Instance.gridManager;
        if (this.answer.Length > 0)
        {
            string deductedChar;
            if (gridManager.isMCType)
            {
                deductedChar = this.answer;
                this.setAnswer(null);
            }
            else
            {
                deductedChar = this.answer[this.answer.Length - 1].ToString();
                this.answer = this.answer.Substring(0, this.answer.Length - 1);
                if (this.answerBox != null)
                    this.answerBox.text = this.answer;

                if (this.answer.Length == 0)
                {
                    SetUI.SetScale(this.answerBoxCg, false);
                }
            }

            if (this.collectedCell.Count > 0)
            {
                var latestCell= this.collectedCell[this.collectedCell.Count - 1];
                latestCell.SetTextStatus(true);
                this.collectedCell.RemoveAt(this.collectedCell.Count - 1);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the other collider has a specific tag, e.g., "Player"
        if (other.CompareTag("Word"))
        {
            var cell = other.GetComponent<Cell>();
            if (cell != null)
            {
                cell.setCellEnterColor(true, GameController.Instance.showCells);
                if (cell.isSelected && this.Retry > 0)
                {
                    var gridManager = GameController.Instance.gridManager;
                    if (gridManager.isMCType){
                        if (this.collectedCell.Count > 0)
                        {
                            var latestCell = this.collectedCell[this.collectedCell.Count - 1];
                            latestCell.SetTextStatus(true);
                            this.collectedCell.RemoveAt(this.collectedCell.Count - 1);
                        }
                    }
                    this.setAnswer(cell);
                    this.characterStatus = CharacterStatus.idling;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Word"))
        {
            var cell = other.GetComponent<Cell>();
            if (cell != null)
            {
                cell.setCellEnterColor(false);
                if (cell.isSelected)
                {
                    LogController.Instance.debug("Player has exited the trigger!" + other.name);
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (this.characterStatus == CharacterStatus.moving)
        {
            if (this.gameObject.name != collision.gameObject.name)
            {
                Rigidbody2D rb = collision.rigidbody;
                Vector2 relativeVelocity = collision.relativeVelocity;

                // Calculate the distance between the two objects
                float distance = Vector2.Distance(this.playerCurrentPosition, collision.transform.localPosition);

                var distanceFactor = distance / 10000f;
                this.reducedFactor = this.reduceBaseFactor + distanceFactor;
                // Apply the reduced factor
                collision.gameObject.GetComponent<PlayerController>().reducedFactor = this.reducedFactor;
                rb.angularVelocity = 0f;

                // Debug log the collision information
                LogController.Instance.debug($"Collision with: {collision.gameObject.name},distanceFactor: {distanceFactor}, Reduced Factor: {reducedFactor}, Distance: {distance}");
            }
            AudioController.Instance?.PlayAudio(10); //blob
            this.characterStatus = CharacterStatus.idling;
        }
    }



    /*private void OnCollisionExit2D(Collision2D collision)
    {
        collision.collider.enabled = true;
    }*/
}
