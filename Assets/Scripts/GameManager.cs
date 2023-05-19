using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


enum PlayerSide { Player, CPU, None }
enum Piece { White, Black, Queen, Sriker }


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField] internal Camera mainCamera;
    [SerializeField] GameObject slider;

    [Space]
    [SerializeField] GameObject strikerColliderChecker;
    [SerializeField] GameObject striker;
    [SerializeField] GameObject pieceColliderChecker;

    [Space]
    [SerializeField] Sprite whiteSprite;
    [SerializeField] Sprite blackSprite;

    [Space]
    [SerializeField] GameObject playerPanel;
    [SerializeField] Image playerImage;
    [SerializeField] Text playerScore;

    [Space]
    [SerializeField] GameObject cpuPanel;
    [SerializeField] Image cpuImage;
    [SerializeField] Text cpuScore;

    [Space]
    [SerializeField] Text timeText;

    [Space]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject winnerPanel;
    [SerializeField] Text winnerText;



    PlayerSide playerSide, queenAddedBy = PlayerSide.None, queenCoveredBy = PlayerSide.None, winBy = PlayerSide.None;
    Piece playerPiece;

    int white = 0, black = 0, queen = 0;
    float width, height, screenRatio;
    const float screenRatioToCamera = 6.18f;

    float sliderXalpha;
    float finalSliderXalpha;

    bool isWhiteAdded = false, isBlackAdded = false, isQueenAdded = false, wasQueenAdded = false, isQueenCovered = false;

    internal bool isAiming = true;
    bool cpuAiming = false;
    bool strikerIsStill = true;

    bool foul = false;
    bool win = false;
    bool gameOver = false;
    bool wait = false;

    ContactFilter2D filter = new ContactFilter2D().NoFilter();
    List<Collider2D> resultList = new List<Collider2D>();

    GameObject[] whitePieces;
    GameObject[] blackPieces;
    GameObject queenPiece;

    private float localTime = 0;
    int time = 2 * 60;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        width = Screen.width;
        height = Screen.height;
        screenRatio = height / width;
        mainCamera.orthographicSize = screenRatio * screenRatioToCamera;

        slider.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, (width * 55 / 100));
        slider.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, (height * 13 / 100), 0);

        whitePieces = GameObject.FindGameObjectsWithTag("White");
        blackPieces = GameObject.FindGameObjectsWithTag("Black");
        queenPiece = GameObject.FindGameObjectWithTag("Queen");

        playerPiece = (Piece)((Random.Range(1, 10)) % 2);
        //Debug.Log(playerPiece);
        if (playerPiece == Piece.White)
        {
            playerImage.GetComponent<Image>().sprite = whiteSprite;
            cpuImage.GetComponent<Image>().sprite = blackSprite;
            playerSide = PlayerSide.Player;
            PlayerMoveStriker();
        }
        else
        {
            playerImage.GetComponent<Image>().sprite = blackSprite;
            cpuImage.GetComponent<Image>().sprite = whiteSprite;
            playerSide = PlayerSide.CPU;
            isAiming = false;
            slider.SetActive(false);
            cpuAiming = true;
            sliderXalpha = Random.Range(0.3f, 0.7f);
            finalSliderXalpha = Random.Range(0.00001f, 1);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!win && !gameOver)
        {
            UpateScore();
            UpdateTime();

            if (!wait)
            {
                if (!strikerIsStill && striker.GetComponent<Rigidbody2D>().velocity == Vector2.zero)
                {
                    strikerIsStill = true;

                    if (ShouldGiveAnotherChance())
                    { GiveAnotherChance(); }
                    else
                    { ChangePlayerSide(); }

                    isWhiteAdded = isBlackAdded = false;
                }

                CpuMoveStriker();
            }
        }
    }

    public void PlayerMoveStriker()
    {
        if (isAiming)
        {
            sliderXalpha = slider.GetComponent<Slider>().value;
            MoveStriker();
        }

    }

    void CpuMoveStriker()
    {
        if (cpuAiming)
        {
            sliderXalpha = Mathf.Lerp(sliderXalpha, finalSliderXalpha, Time.deltaTime * 5);

            if (IsApprox(sliderXalpha, finalSliderXalpha))
            {
                cpuAiming = false;
                CpuShootStriker();
                //Debug.Log("Shoot");
            }

            MoveStriker();
        }

    }

    void CpuShootStriker()
    {
        Vector3 vec = new Vector3();

        if ((playerPiece == Piece.White ? black : white) < 7 || queen == 1 || wasQueenAdded)
        {
            foreach (var piece in (playerPiece == Piece.White ? blackPieces : whitePieces))
            {
                if (piece.active)
                {
                    vec = piece.transform.position - striker.transform.position;
                }
            }
        }
        else
        {

            if (queen != null && queenPiece.active)
            {
                vec = queenPiece.transform.position - striker.transform.position;
            }
        }

        float rotationRadian = Mathf.Atan2(vec.y, vec.x);

        float force = 60;

        Vector2 goTo = new Vector2((force * Mathf.Cos(rotationRadian)), (force * Mathf.Sin(rotationRadian)));

        striker.GetComponent<Rigidbody2D>().AddForce(goTo, ForceMode2D.Impulse);

        StrikerFired();

    }

    void MoveStriker()
    {
        strikerColliderChecker.SetActive(true);

        strikerColliderChecker.transform.position = new Vector3((-3.2f + (6.4f * sliderXalpha)),
                (playerSide == PlayerSide.Player ? -3.993f : 3.993f), 0);

        strikerColliderChecker.GetComponent<Collider2D>().OverlapCollider(filter, resultList);

        bool canMove = true;

        foreach (Collider2D collider in resultList)
        {
            if (collider.tag == "White" || collider.tag == "Black" || collider.tag == "Queen")
            {
                canMove = false;
                break;
            }

        }


        if (canMove)
        {
            striker.transform.position = new Vector3((-3.2f + (6.4f * sliderXalpha)),
                    (playerSide == PlayerSide.Player ? -3.993f : 3.993f), 0);
        }

        resultList.Clear();
        strikerColliderChecker.SetActive(false);
    }

    void UpdateTime()
    {
        if (!gameOver)
        {
            localTime += Time.deltaTime;

            if (!(localTime > 1)) { return; }

            localTime = localTime - 1;

            if (time > 0) { time--; }
            else { GameOver(); }


            timeText.text = AddZeroWhenNeeded(Mathf.FloorToInt(time / 60)) + ":" + AddZeroWhenNeeded((time % 60));
        }
    }

    internal void StrikerFired()
    {
        isAiming = false;
        slider.SetActive(false);
        strikerIsStill = false;
        StartCoroutine(WaitForAWhile());
    }

    IEnumerator WaitForAWhile()
    {
        wait = true;
        yield return new WaitForSeconds(5);
        wait = false;
    }

    void UpateScore()
    {
        playerScore.text = "Score : " + ((playerPiece == Piece.White ? white : black) + (queenCoveredBy == PlayerSide.Player ? 2 * queen : 0));
        cpuScore.text = "Score : " + ((playerPiece == Piece.White ? black : white) + (queenCoveredBy == PlayerSide.CPU ? 2 * queen : 0));
    }



    internal void WhiteAdd()
    {
        if (white < 7)
        {
            isWhiteAdded = true;
            white++;
        }
        else if (isQueenCovered)
        {
            white++;
            winBy = playerPiece == Piece.White ? PlayerSide.Player : PlayerSide.CPU;
            GameWin();
        }
        else if (wasQueenAdded)
        {
            white++;
            queen++;
            winBy = playerPiece == Piece.White ? PlayerSide.Player : PlayerSide.CPU;
            GameWin();

        }
        else
        {
            StartCoroutine(WaitThenSpwan(Piece.White));
        }

    }

    internal void BlackAdd()
    {
        if (black < 7)
        {
            isBlackAdded = true;
            black++;
        }
        else if (isQueenCovered)
        {
            black++;
            winBy = playerPiece == Piece.White ? PlayerSide.CPU : PlayerSide.Player;
            GameWin();
        }
        else if (wasQueenAdded)
        {
            black++;
            queen++;
            winBy = playerPiece == Piece.White ? PlayerSide.CPU : PlayerSide.Player;
            GameWin();
        }
        else
        {
            StartCoroutine(WaitThenSpwan(Piece.Black));
        }
    }

    internal void QueenAdd()
    {
        isQueenAdded = true;

        //queen++;
    }

    internal void StrikerAdd()
    {
        foul = true;
        Debug.Log("Foul..");

    }


    IEnumerator WaitThenSpwan(Piece _piece)
    {
        yield return new WaitForSeconds(5);
        RespawnPiece(_piece);
    }

    bool ShouldGiveAnotherChance()
    {
        if (foul)
        {
            if (isQueenAdded)
            {
                isQueenAdded = false;
                RespawnPiece(Piece.Queen);
            }
            return false;
        }
        if (playerSide == PlayerSide.Player)
        {
            if (isQueenAdded)
            {
                isQueenAdded = false;
                wasQueenAdded = true;
                queenAddedBy = PlayerSide.Player;
                return true;
            }
            else if (playerPiece == Piece.White)
            {
                if (isWhiteAdded)
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        isQueenCovered = true;
                        queen++;
                        queenCoveredBy = PlayerSide.Player;
                    }
                    return true;
                }
                else
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        RespawnPiece(Piece.Queen);
                    }
                    return false;
                }
            }
            else
            {
                if (isBlackAdded)
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        isQueenCovered = true;
                        queen++;
                        queenCoveredBy = PlayerSide.Player;
                    }
                    return true;
                }
                else
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        RespawnPiece(Piece.Queen);
                    }
                    return false;
                }
            }
        }
        else
        {
            if (isQueenAdded)
            {
                isQueenAdded = false;
                wasQueenAdded = true;
                queenAddedBy = PlayerSide.CPU;
                return true;
            }
            else if (playerPiece == Piece.Black)
            {
                if (isWhiteAdded)
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        isQueenCovered = true;
                        queen++;
                        queenCoveredBy = PlayerSide.CPU;
                    }
                    return true;
                }
                else
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        RespawnPiece(Piece.Queen);
                    }
                    return false;
                }
            }
            else
            {
                if (isBlackAdded)
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        isQueenCovered = true;
                        queen++;
                        queenCoveredBy = PlayerSide.CPU;
                    }
                    return true;
                }
                else
                {
                    if (wasQueenAdded)
                    {
                        wasQueenAdded = false;
                        RespawnPiece(Piece.Queen);
                    }
                    return false;
                }
            }
        }




    }

    void GiveAnotherChance()
    {
        if (playerSide == PlayerSide.CPU)
        {
            cpuAiming = true;
            sliderXalpha = Random.Range(0.3f, 0.7f);
            finalSliderXalpha = Random.Range(0.00001f, 1);
        }
        else
        {
            isAiming = true;
            slider.GetComponent<Slider>().value = 0.5f;
            slider.SetActive(true);
            PlayerMoveStriker();
            cpuAiming = false;
        }
    }

    void ChangePlayerSide()
    {
        if (playerSide == PlayerSide.Player)
        {
            playerSide = PlayerSide.CPU;
            cpuAiming = true;
            sliderXalpha = Random.Range(0.3f, 0.7f);
            finalSliderXalpha = Random.Range(0.00001f, 1);
        }
        else
        {
            playerSide = PlayerSide.Player;
            isAiming = true;
            slider.GetComponent<Slider>().value = 0.5f;
            slider.SetActive(true);
            PlayerMoveStriker();
            cpuAiming = false;
        }
        if (foul)
        {
            foul = false;
            RespawnPiece(Piece.Sriker);
        }
    }

    void RespawnPiece(Piece _piece)
    {
        switch (_piece)
        {
            case Piece.White:
                {
                    GameObject piece = FindInactivePiece(_piece);
                    if (piece != null)
                    {
                        piece.transform.position = FindSpawnLocationAtCenter();
                        piece.SetActive(true);
                    }
                    break;
                }

            case Piece.Black:
                {
                    GameObject piece = FindInactivePiece(_piece);
                    if (piece != null)
                    {
                        piece.transform.position = FindSpawnLocationAtCenter();
                        piece.SetActive(true);
                    }
                    break;
                }

            case Piece.Queen:
                {
                    queenPiece.transform.position = FindSpawnLocationAtCenter();
                    queenPiece.SetActive(true);
                    break;
                }

            case Piece.Sriker:
                {
                    striker.transform.position = new Vector3(0, (playerSide == PlayerSide.Player ? -3.97f : 3.97f), 0);
                    striker.SetActive(true);
                    break;
                }
        }
    }


    GameObject FindInactivePiece(Piece _piece)
    {
        if (_piece == Piece.White)
        {
            foreach (GameObject piece in whitePieces)
            {
                if (!piece.active)
                {
                    return piece;
                }
            }
        }

        else if (_piece == Piece.Black)
        {

            foreach (GameObject piece in blackPieces)
            {
                if (!piece.active)
                {
                    return piece;
                }
            }
        }

        return null;

    }

    Vector3 FindSpawnLocationAtCenter()
    {
        Vector3 output = Vector3.zero;

        pieceColliderChecker.SetActive(true);

        pieceColliderChecker.transform.position = Vector3.zero;
        pieceColliderChecker.GetComponent<Collider2D>().OverlapCollider(filter, resultList);
        Debug.Log(resultList.Count);

        if (resultList.Count == 0) 
        {
            resultList.Clear();
            pieceColliderChecker.SetActive(false);
            return output;
        }


        pieceColliderChecker.SetActive(false);
        resultList.Clear();

        for (int i = 1; i < 10; i++)
        {
            //Check Left            
            pieceColliderChecker.transform.position = output = new Vector3(i * -0.56f, 0, 0);
            pieceColliderChecker.SetActive(true);
            pieceColliderChecker.GetComponent<Collider2D>().OverlapCollider(filter, resultList);
            if (resultList.Count == 0)
            {
                resultList.Clear();
                pieceColliderChecker.SetActive(false);
                return output;
            }
            pieceColliderChecker.SetActive(false);
            resultList.Clear();

            //Check Right
            pieceColliderChecker.transform.position = output = new Vector3(i * 0.56f, 0, 0);
            pieceColliderChecker.SetActive(true);
            pieceColliderChecker.GetComponent<Collider2D>().OverlapCollider(filter, resultList);
            if (resultList.Count == 0)
            {
                resultList.Clear();
                pieceColliderChecker.SetActive(false);
                return output;
            }
            pieceColliderChecker.SetActive(false);
            resultList.Clear();

            //Check Top
            pieceColliderChecker.transform.position = output = new Vector3(0, i * 0.56f, 0);
            pieceColliderChecker.SetActive(true);
            pieceColliderChecker.GetComponent<Collider2D>().OverlapCollider(filter, resultList);
            if (resultList.Count == 0)
            {
                resultList.Clear();
                pieceColliderChecker.SetActive(false);
                return output;
            }
            pieceColliderChecker.SetActive(false);
            resultList.Clear();

            //Check Bottom
            pieceColliderChecker.transform.position = output = new Vector3(0, i * -0.56f, 0);
            pieceColliderChecker.SetActive(true);
            pieceColliderChecker.GetComponent<Collider2D>().OverlapCollider(filter, resultList);
            if (resultList.Count == 0)
            {
                resultList.Clear();
                pieceColliderChecker.SetActive(false);
                return output;
            }
            pieceColliderChecker.SetActive(false);
            resultList.Clear();
        }


        resultList.Clear();
        pieceColliderChecker.SetActive(false);
        return output;
    }

    void GameWin()
    {
        winnerText.text = (winBy == PlayerSide.Player ? "PLAYER" : "CPU") + " WINS";
        winnerPanel.SetActive(true);
        win = true;
        Debug.Log("Game Win");
    }

    void GameOver()
    {
        Debug.Log("Game Over");
        gameOver = true;
        gameOverPanel.SetActive(true);
    }

    public void Restart()
    {
        SceneManager.LoadScene("Game");
    }

    string AddZeroWhenNeeded(int num)
    {
        if (num < 10)
        {
            return "0" + num;
        }
        else
        {
            return num.ToString();
        }
    }

    bool IsApprox(float a, float b)
    {
        if (a > b)
        {
            if ((a - b) < 0.00001f)
            { return true; }
            else
            { return false; }
        }
        else
        {
            if ((b - a) < 0.00001f)
            { return true; }
            else
            { return false; }
        }
    }





}
