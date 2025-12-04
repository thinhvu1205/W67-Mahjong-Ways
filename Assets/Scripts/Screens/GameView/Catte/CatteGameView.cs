using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using UnityEngine.EventSystems;
using DG.Tweening;
using Globals;
using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;
using TMPro;
using System.Linq;
using System.Threading.Tasks;

[System.Serializable]
public class PlayerResultData
{
    public int uid;
    public int M;
    public int ag;
    public bool isPenaltyUser;
    public int idCardPenalty;
    public int agwinPot;

    public int typeWin;
    public bool isBigWin;
    public List<int> arrWin;
}
[System.Serializable]
public class STableData
{
    public long pot;
    public int idUserWinPot;
}
public class CatteGameView : GameView
{
    public List<List<Card>> ListCardPlayer = new List<List<Card>>();
    [SerializeField] private Transform m_ContainerCards;
    [SerializeField] private SkeletonGraphic animWinHitPot;
    [SerializeField] private TextMeshProUGUI lbWinHitPot;
    [SerializeField] private GameObject cardPrefab;
    private const float SCALE_CARD = 0.62f;
    private const float SCALE_CARD_OTHER = 0.2f;
    private const float SCALE_CARD_DANH = 0.45f;
    private const float DIS_CARD = 0.55f;
    private bool isReconnect = false;
    public Transform node;
    // private UIManager uIManager;
    // private LogicCatteManager logicCatteManager;
    // private Player player;
    // private GameView gameView;

    [Header("Card Positions")]
    public Vector2[] listPosCard;
    public Vector2[] listPosCardDanh;

    [Header("Touch Input")]
    public Vector2 posTouchBegan = Vector2.zero;
    public Vector2 posDefaultCard = Vector2.zero;
    public int zOrderCard = 0;

    [Header("Card Selection")]
    public Card cardSelect = null;
    public int numCardSelect = 0;

    [Header("UI Elements")]
    public SkeletonGraphic animStartGame;
    public TextMeshProUGUI lbRound;
    public TextMeshProUGUI lbValuePot;
    public GameObject lightTurn;
    public List<GameObject> nodeTableBoxs;
    // public List<GameObject> nodePlayerIcons;
    public GameObject[] nodePotIcons;
    public GameObject nodeWinHitPot;
    public Button btnDown;
    public Button btnBeat;
    public SkeletonDataAsset aniLost4Roud;
    public SkeletonDataAsset aniLose;
    public GameObject catteHistory;
    public GameObject nodeBlockEvent;
    public SkeletonGraphic aniWinSpecial;
    public TextMeshProUGUI lbName;
    public GameObject avtSpecial;

    [Header("State")]
    public string langLocal = "eng";
    public bool istouching = false;
    public bool dangPhatBai = false;
    public float timeTurn = 0f;
    public Player turnCurrentPlayer;

    // Internal game state
    private bool isFinish;
    private int roundCurrent;
    private int turnIdCurrent;
    private long totalPot;
    private int idUserWinPot;
    private Card cardBefore;
    private Vector3 touchStartPos;
    private Card selectedCard = null;
    private Vector3 cardStartPos;
    private bool isDragging = false;
    private bool isCheckCardBefore;
    private float sizeCardW;
    private Coroutine offBlockEvent;
    private Vector2 POS_CARD;
    private List<int> listLightTurnRotation = new List<int> { 124, 98, 76, 5, 284, 264 };
    private List<float> listLightTurnScale = new List<float> { 1.8f, 2.2f, 2f, 1, 2, 2 };
    private List<GameObject> listLostRound = new List<GameObject>();
    private CatteHistory popupHistory;
    private string path_aniLost4Roud = "Assets/Resources/GameView/Catte_Asset/res/animations/lose chaybai/skeleton_SkeletonData";
    private bool isChiabai = false;
    protected override void Awake()
    {
        aniLost4Roud = UIManager.instance.loadSkeletonData(path_aniLost4Roud);
        base.Awake();
        for (int i = 0; i < 6; i++)
        {
            ListCardPlayer.Add(new List<Card>());
        }
    }
    protected override void Start()
    {
        base.Start();
        // player = GetComponent<Player>();
        // gameView = GetComponent<GameView>();

        isFinish = false;
        if (animStartGame != null)
        {
            animStartGame.gameObject.SetActive(false);
            animStartGame.transform.SetAsLastSibling();
        }

        // if (lbRound != null) lbRound.text = "";

        // roundCurrent = 0;
        // turnIdCurrent = 0;
        // UIManager.instance.totalPot = 0;
        // idUserWinPot = 0;
        cardBefore = null;
        isCheckCardBefore = false;

        if (lightTurn != null) lightTurn.SetActive(false);
        if (btnDown != null) btnDown.gameObject.SetActive(false);
        if (btnBeat != null) btnBeat.gameObject.SetActive(false);
        if (nodeWinHitPot != null)
        {
            nodeWinHitPot.SetActive(false);
            nodeWinHitPot.transform.SetAsLastSibling();
        }

        if (aniWinSpecial != null && aniWinSpecial.transform.parent != null)
        {
            aniWinSpecial.transform.parent.gameObject.SetActive(false);
            aniWinSpecial.transform.parent.SetAsLastSibling();
        }

        listLightTurnRotation = new List<int> { 124, 98, 76, 5, 284, 264 };
        listLightTurnScale = new List<float> { 1.8f, 2.2f, 2f, 1, 2, 2 };
        listLostRound = new List<GameObject>();
        popupHistory = null;

        if (listPosCard != null && listPosCard.Length > 0)
        {
            POS_CARD = listPosCard[0];
        }

        Card cardC = getCard();
        if (cardC != null)
        {
            RectTransform rect = cardC.GetComponent<RectTransform>();
            if (rect != null)
            {
                sizeCardW = rect.rect.width * SCALE_CARD;
            }
        }
        OnTouchCard(cardC);
    }


    void OnTouchCard(Card card)
    {
        if (istouching || isFinish || dangPhatBai || thisPlayer == null || stateGame != STATE_GAME.PLAYING)
            return;
        if (card.isMoved) return;
        cardSelect = card;
        posDefaultCard = card.transform.localPosition;
        istouching = true;

        // Nếu chỉ chạm nhẹ không kéo => xử lý chọn
        cardSelect.isSelect = !cardSelect.isSelect;
        float posY = cardSelect.isSelect ? POS_CARD.y + 30 : POS_CARD.y;

        card.transform.DOScale(new Vector3(0.62f, 0.62f, 1), 0.4f).SetEase(Ease.OutCubic);
        card.transform.DOLocalMove(new Vector3(posDefaultCard.x, posY, 0), 0.2f).SetEase(Ease.OutCubic);

        // Reset các lá khác
        foreach (Card c in thisPlayer.vectorCard)
        {
            if (c == cardSelect) continue;

            c.isSelect = false;
            c.transform.DOScale(new Vector3(0.62f, 0.62f, 1), 0.4f).SetEase(Ease.OutCubic);
            c.transform.DOLocalMove(new Vector3(c.transform.localPosition.x, POS_CARD.y, 0), 0.2f).SetEase(Ease.OutCubic);
        }

        // Hiện nút Beat hoặc Down
        if (cardSelect.isSelect && turnIdCurrent == thisPlayer.id)
        {
            if (cardBefore == null)
            {
                btnBeat.gameObject.SetActive(true);
                btnDown.gameObject.SetActive(false);
            }
            else
            {
                if (cardSelect.S == cardBefore.S &&
                    cardSelect.N > cardBefore.N &&
                    cardBefore.GetStateBorder() &&
                    isCheckCardBefore)
                {
                    btnBeat.gameObject.SetActive(true);
                    btnDown.gameObject.SetActive(false);
                }
                else
                {
                    btnBeat.gameObject.SetActive(false);
                    btnDown.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            btnBeat.gameObject.SetActive(false);
            btnDown.gameObject.SetActive(false);
        }

        istouching = false;
    }

    // public void OnPointerDown(PointerEventData eventData)
    // {
    //     TouchStart(eventData);
    // }

    // public void OnPointerUp(PointerEventData eventData)
    // {
    //     TouchEnd(eventData);
    // }
    // private void TouchStart(PointerEventData touch)
    // {
    //     if (istouching || isFinish || dangPhatBai || thisPlayer == null || stateGame != STATE_GAME.PLAYING)
    //         return;

    //     posTouchBegan = touch.position;

    //     for (int i = thisPlayer.vectorCard.Count - 1; i >= 0; i--)
    //     {
    //         Card card = thisPlayer.vectorCard[i];
    //         if (card.isTouch && RectTransformUtility.RectangleContainsScreenPoint(card.GetComponent<RectTransform>(), posTouchBegan))
    //         {
    //             cardSelect = card;
    //             posDefaultCard = card.transform.localPosition;
    //             istouching = true;

    //             Vector3 targetPos = new Vector3(posDefaultCard.x, POS_CARD.y + 30, 0);
    //             card.transform.DOScale(new Vector3(0.62f, 0.62f, 1), 0.2f).SetEase(Ease.OutCubic);
    //             card.transform.DOLocalMove(targetPos, 0.2f).SetDelay(0.2f).SetEase(Ease.OutCubic);
    //             return;
    //         }
    //     }
    // }
    // private void TouchEnd(PointerEventData touch)
    // {
    //     if (!istouching || isFinish || thisPlayer == null || stateGame != STATE_GAME.PLAYING)
    //     {
    //         cardSelect = null;
    //         return;
    //     }

    //     istouching = false;
    //     Vector2 touchPos = touch.position;

    //     btnBeat.gameObject.SetActive(false);
    //     btnDown.gameObject.SetActive(false);

    //     if (Vector2.Distance(posTouchBegan, touchPos) <= sizeCardW)
    //     {
    //         float posX = posDefaultCard.x;
    //         foreach (Card card in thisPlayer.vectorCard)
    //         {
    //             if (card == cardSelect)
    //             {
    //                 cardSelect.isSelect = !cardSelect.isSelect;
    //                 float posY = cardSelect.isSelect ? POS_CARD.y + 30 : POS_CARD.y;

    //                 card.transform.DOScale(new Vector3(0.62f, 0.62f, 1), 0.4f).SetEase(Ease.OutCubic);
    //                 card.transform.DOLocalMove(new Vector3(posX, posY, 0), 0.2f).SetEase(Ease.OutCubic);
    //             }
    //             else
    //             {
    //                 card.isSelect = false;
    //                 card.transform.DOScale(new Vector3(0.62f, 0.62f, 1), 0.4f).SetEase(Ease.OutCubic);
    //                 card.transform.DOLocalMove(new Vector3(card.transform.localPosition.x, POS_CARD.y, 0), 0.2f).SetEase(Ease.OutCubic);
    //             }
    //         }

    //         if (cardSelect.isSelect && turnIdCurrent == thisPlayer.id)
    //         {
    //             Card cardCompare = null;
    //             foreach (Card c in thisPlayer.vectorCard)
    //             {
    //                 if (c.isSelect)
    //                 {
    //                     cardCompare = c;
    //                     break;
    //                 }
    //             }

    //             if (cardBefore == null)
    //             {
    //                 btnBeat.gameObject.SetActive(true);
    //                 btnDown.gameObject.SetActive(false);
    //             }
    //             else
    //             {
    //                 if (cardCompare.S == cardBefore.S &&
    //                     cardCompare.N > cardBefore.N &&
    //                     cardBefore.GetStateBorder() &&
    //                     isCheckCardBefore)
    //                 {
    //                     btnBeat.gameObject.SetActive(true);
    //                 }
    //                 else
    //                 {
    //                     btnDown.gameObject.SetActive(true);
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             btnBeat.gameObject.SetActive(false);
    //             btnDown.gameObject.SetActive(false);
    //         }

    //         return;
    //     }

    //     cardSelect = null;
    //     SortCardView();
    // }

    public void SortCardView()
    {
        Debug.Log($"[SortCardView] count={thisPlayer.vectorCard.Count}, sizeCardW={sizeCardW}, SCALE={SCALE_CARD}");
        Debug.Log("!> sort card view " + thisPlayer.vectorCard.Count);

        if (thisPlayer.vectorCard.Count <= 1)
        {
            CancelInvoke(nameof(OffBlockEvent));
            OffTouchCard();
        }

        dangPhatBai = true;
        int index = Mathf.FloorToInt(thisPlayer.vectorCard.Count / -2f);
        Debug.Log($"thisPlayer.vectorCard.Count: {thisPlayer.vectorCard.Count}");
        for (int i = 0; i < thisPlayer.vectorCard.Count; i++)
        {
            Card card = thisPlayer.vectorCard[i];
            Transform cardTransform = card.transform;

            // cardTransform.DOKill(); // stopAllActions tương đương
            cardTransform.DOScale(SCALE_CARD, 0.2f).SetEase(Ease.OutCubic);
            // cardTransform.SetSiblingIndex(i); // zIndex = Z_CARD + i

            CanvasGroup canvasGroup = card.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = card.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            Vector2 posCard = listPosCard[0] + new Vector2(sizeCardW * i, 0);
            Debug.Log($"Poscard: {posCard}");
            if (isReconnect)
            {
                card.transform.localPosition = new Vector3(posCard.x, posCard.y, 0);
                card.isTouch = true;
                card.isSelect = false;
            }
            else
            {
                cardTransform.DOLocalMove(posCard, 0.1f).SetEase(Ease.InOutCubic).OnComplete(() =>
                {
                    card.isTouch = true;
                    card.isSelect = false;
                });
            }

            index++;
        }
        DOVirtual.DelayedCall(0.1f, () =>
        {
            dangPhatBai = false;
            isReconnect = false;
        });

        numCardSelect = 0;
    }
    private void OffTouchCard()
    {
        // Logic hủy touch như trước
    }

    private void OffBlockEvent()
    {
        if (nodeBlockEvent != null)
            nodeBlockEvent.SetActive(false);
    }
    #region HandleGame
    public void HandleStartGame(string strData)
    {
        CheckHitpotBeforeStartGame();

        if (animStartGame != null)
        {
            animStartGame.gameObject.SetActive(true);
            animStartGame.AnimationState.SetAnimation(0, "startgame2", false);
        }

        stateGame = STATE_GAME.PLAYING;

        clearAllCard();
    }

    public void HandleLc(JObject data)
    {
        isChiabai = true;
        animStartGame?.gameObject.SetActive(false);
        lightTurn?.SetActive(true);
        timeTurn = data["timeOut"]?.ToObject<float>() ?? 0f;
        int round = data["round"]?.ToObject<int>() ?? 1;
        bool firstRound = round == 1;
        roundCurrent = round;
        JArray arrCards = data["arr"] as JArray;
        if (arrCards == null || players == null) return;
        List<int> cardValues = arrCards.Select(c => (int)c).ToList();
        foreach (Player player in players)
        {
            player.vectorCard = new List<Card>();
            bool isMainPlayer = player == thisPlayer;
            Debug.Log($"CardValueCount: {cardValues.Count}");
            for (int j = 0; j < cardValues.Count; j++)
            {
                Debug.Log($"CardValue: {cardValues[j]}");
                // Card card = getCard();
                Card card = spawnCard();
                // Card card = Instantiate(cardPrefabs, m_ContainerCards);
                card.setTextureWithCode(0);
                card.StateCatteBorder(false);
                card.gameObject.SetActive(false);
                card.transform.SetParent(m_ContainerCards);
                card.transform.localScale = Vector3.one * SCALE_CARD_OTHER;
                card.transform.SetSiblingIndex(1000 - j);
                card.transform.localPosition = Vector3.zero;

                if (isMainPlayer)
                {
                    card.decodeCard(cardValues[j]);
                }
                player.vectorCard.Add(card);
            }

            player.typeCard = isMainPlayer ? 6 : 0;
        }

        StartCoroutine(HandleLcSequence(data));
    }

    // public override void handleCTable(string strData)
    // {
    //     base.handleCTable(strData);
    // }
    public override void handleSTable(string strData)
    {
        base.handleSTable(strData);

        var data = JsonUtility.FromJson<STableData>(strData);
        SetPot(data.pot, data.idUserWinPot);
        UpdateHitPot(data.idUserWinPot);

        Debug.Log($"Tinh=))Data.Pot: {data.pot}");
        Debug.Log($"Tinh=))Data.strData: {strData}");
        Debug.Log($"Tinh=))Data Json: {JsonUtility.ToJson(data, true)}");

        this.idUserWinPot = data.idUserWinPot;
    }

    public override void handleVTable(string strData)
    {
        Debug.LogError("xem data V" + strData);
        base.handleVTable(strData);
        connectGame(strData);
    }
    public override void handleRJTable(string strData)
    {
        Debug.LogError("xem data RJ" + strData);
        cleanUser();
        base.handleRJTable(strData);
        connectGame(strData);
    }
    private void cleanUser()
    {
        foreach (Player playerRemove in players)
        {
            if (playerRemove.playerView != null)
            {
                Destroy(playerRemove.playerView.gameObject);
                playerRemove.playerView = null;
            }
        }
        players.Clear();
        foreach (Transform child in playerContainer)
        {
            Destroy(child.gameObject);
        }


    }
    public override void handleLTable(JObject data)
    {
        string namePl = (string)data["Name"];
        Player playerLeft = getPlayer(namePl);

        if (playerLeft != null && playerLeft.id == idUserWinPot)
        {
            idUserWinPot = 0;
            CheckHitpotBeforeStartGame();
            for (int i = 0; i < nodePotIcons.Length; i++)
            {
                nodePotIcons[i].SetActive(false);
            }
        }
        base.handleLTable(data);
    }


    #endregion
    private PlayerViewTienlen getPlayerView(Player player)
    {
        if (player != null)
        {
            return (PlayerViewTienlen)player.playerView;
        }
        return null;

    }
    public void CheckHitpotBeforeStartGame()
    {
        for (int i = 0; i < players.Count; i++)
        {
            PlayerViewTienlen playerView = getPlayerView(players[i]);
            playerView.imageIconPot.gameObject.SetActive(false);
        }

        if (idUserWinPot == 0)
            return;

        Player player = getPlayerWithID(idUserWinPot);
        int index = getDynamicIndex(getIndexOf(player));

        Debug.Log($"players.Count: {players.Count}//index: {index}");
        PlayerViewTienlen playerViewWin = getPlayerView(player);
        playerViewWin.imageIconPot.gameObject.SetActive(true);
    }
    protected override void updatePositionPlayerView()
    {
        players.Sort((a, b) =>
        {
            return b.ag.CompareTo(a.ag);
        });
        for (int i = 0; i < players.Count; i++)
        {
            if (thisPlayer == players[i])
            {
                players.RemoveAt(i);
                break;
            }
        }
        players.Insert(0, thisPlayer);
        for (var i = 0; i < players.Count; i++)
        {
            players[i].playerView.transform.localScale = new Vector2(0.8f, 0.8f);
            var idPos = getDynamicIndex(getIndexOf(players[i]));
            players[i].playerView.transform.localPosition = listPosView[idPos];
            players[i]._indexDynamic = idPos;
            players[i].updateItemVip(players[i].vip, idPos);
            if (players.Count == 2
            && idPos == 2
            )
            {
                players[1].playerView.transform.localPosition = listPosView[idPos + 1];
                players[1]._indexDynamic = idPos + 1;
                players[1].updateItemVip(players[1].vip, idPos + 1);
            }
            else
            {
                players[i].playerView.transform.localPosition = listPosView[idPos];
                players[i]._indexDynamic = idPos;
                players[i].updateItemVip(players[i].vip, idPos);
            }
            if (players[i] == thisPlayer)
            {
                players[i].playerView.transform.localScale = Vector3.one;
            }
        }
    }

    public void connectGame(string strData)
    {
        isReconnect = true;
        updatePositionPlayerView();
        Debug.Log("-=-=connectGame " + strData);

        JObject data;
        try
        {
            data = JObject.Parse(strData);
        }
        catch (Exception e)
        {
            Debug.LogError("connectGame: JSON parse failed - " + e);
            return;
        }

        JArray listPlayer = (JArray?)data["ArrP"] ?? new JArray();
        int numberCardDanh = ((JArray?)listPlayer.FirstOrDefault()?["arrD"] ?? new JArray()).Count;
        roundCurrent = data["currRound"]?.ToObject<int>() ?? 0;
        int winRoundCurrent = 0;
        cardBefore = null;

        foreach (var itemToken in listPlayer)
        {
            JObject item = itemToken as JObject;
            if (item == null) continue;

            int pid = item["id"]?.ToObject<int>() ?? -1;
            Player player = getPlayerWithID(pid);
            if (player == null)
            {
                Debug.LogWarning($"connectGame: Không tìm thấy player ID {pid}");
                continue;
            }

            // clear cũ để tránh chồng card khi reconnect
            player.vectorCard.Clear();
            player.vectorCardD.Clear();

            // ====== vectorCard (bài đang cầm) ======
            foreach (var cardCodeToken in (JArray?)item["Arr"] ?? new JArray())
            {
                int cardCode = cardCodeToken?.ToObject<int>() ?? -1;
                if (cardCode < 0) continue;

                Card card = spawnCard();
                card.decodeCard(cardCode);
                player.vectorCard.Add(card);

                if (player == thisPlayer)
                {
                    SortCardView();
                    AddCardTouchEvents(card); // có thể click ngay
                    card.isTouch = true;
                    card.isSelect = false;
                }
            }

            // ====== vectorCardD (bài đã đánh) ======
            JArray arrD = (JArray?)item["arrD"] ?? new JArray();
            if (numberCardDanh < arrD.Count)
                numberCardDanh = arrD.Count;

            foreach (var cardCodeToken in arrD)
            {
                int cardCode = cardCodeToken?.ToObject<int>() ?? -1;
                if (cardCode < 0) continue;

                Card card = spawnCard();
                card.decodeCard(cardCode);
                card.setDark(true);
                player.vectorCardD.Add(card);
            }

            // ====== cardBefore ======
            if (roundCurrent > 0 && arrD.Count > roundCurrent - 1)
            {
                int codeAtRound = arrD[roundCurrent - 1]?.ToObject<int>() ?? -1;
                if (codeAtRound >= 0 && (cardBefore == null || codeAtRound > cardBefore.code))
                    cardBefore = player.vectorCardD.Find(c => c.code == codeAtRound);
            }

            // ====== listTon ======
            JArray listTon = (JArray?)item["listTon"] ?? new JArray();
            foreach (var tonItem in listTon)
            {
                int winRound = tonItem["winRound"]?.ToObject<int>() ?? 0;
                if (winRoundCurrent < winRound)
                    winRoundCurrent = winRound;

                JToken lstToken = tonItem["lstIdCardWinRound"];
                if (lstToken != null && lstToken.Type != JTokenType.Null)
                {
                    if (lstToken.Type == JTokenType.Array)
                    {
                        player.listTon.Add(lstToken.ToObject<List<int>>());
                    }
                    else if (lstToken.Type == JTokenType.Integer)
                    {
                        player.listTon.Add(new List<int> { lstToken.ToObject<int>() });
                    }
                }
            }
        }

        // Người chơi hiện tại
        int cnId = data["CN"]?.ToObject<int>() ?? -1;
        Player playerCurrent = getPlayerWithID(cnId);
        Debug.Log($"cnId: {cnId}");
        // init sau khi đã add card
        InitPlayerCard();

        // đảm bảo thisPlayer được sắp xếp lại bài sau reconnect
        SortCardView();

        SetTurn(playerCurrent?.namePl ?? "", data["CT"]?.ToObject<int>() ?? 0);
        // SetTurn(playerCurrent.namePl);
        turnIdCurrent = cnId;

        Debug.Log("-=-=data.currRound " + roundCurrent);
        SetRound(roundCurrent);

        // update pot nếu cần
        // UpdateHitPot(data["pot"]?.ToObject<int>() ?? 0, data["idUserWinPot"]?.ToObject<int>() ?? -1);
        totalPot = data["pot"]?.ToObject<int>() ?? 0;

        int indexPlayerTurn = getDynamicIndex(getIndexOf(playerCurrent));
        SetLightTurn(indexPlayerTurn);

        Debug.LogError("rj2 || " + turnIdCurrent + " || " + cardBefore);

        if (cardBefore != null)
        {
            cardBefore.StateCatteBorder(true);
            cardBefore.setDark(false);
            isCheckCardBefore = true;
        }
    }



    public Card spawnCard()
    {

        // Check tái sử dụng
        foreach (List<Card> cardList in ListCardPlayer)
        {
            foreach (Card card in cardList)
            {
                if (card != null && !card.gameObject.activeSelf)
                {
                    AddCardTouchEvents(card);
                    return card;
                }
            }
        }
        Card cardTemp = getCard();
        if (cardTemp == null)
        {
            return null;
        }

        cardTemp.setTextureWithCode(0);
        cardTemp.transform.localPosition = new Vector2(0f, 20f);
        cardTemp.transform.SetParent(m_ContainerCards);
        cardTemp.transform.localScale = new Vector3(SCALE_CARD, SCALE_CARD, SCALE_CARD);
        AddCardTouchEvents(cardTemp);
        return cardTemp;
    }
    private void AddCardTouchEvents(Card card)
    {
        if (card == null) return;

        // Check component Image
        Image img = card.GetComponent<Image>();
        if (img == null)
        {
            img = card.gameObject.AddComponent<Image>();
        }
        img.raycastTarget = true;

        // Check nếu là bài của người chơi chính
        int playerIndex = players.IndexOf(thisPlayer);
        Debug.Log($"playerIndex: {playerIndex}//ListCardPlayer.Count: {ListCardPlayer.Count}//ListCardPlayer[playerIndex]: {ListCardPlayer[playerIndex]}");
        if (playerIndex < 0 || playerIndex >= ListCardPlayer.Count /*|| ListCardPlayer[playerIndex].Contains(card)*/)
        {
            Debug.Log($"AAAAAAAAAAAAAAA");
            return;
        }

        // Add EventTrigger
        EventTrigger trigger = card.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = card.gameObject.AddComponent<EventTrigger>();
        }
        trigger.triggers.Clear();

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) =>
        {

            OnTouchCard(card);

        });
        trigger.triggers.Add(entryDown);

        // Drag
        EventTrigger.Entry entryDrag = new EventTrigger.Entry();
        entryDrag.eventID = EventTriggerType.Drag;
        entryDrag.callback.AddListener((data) =>
        {
        });
        trigger.triggers.Add(entryDrag);

        // EndDrag
        EventTrigger.Entry entryEndDrag = new EventTrigger.Entry();
        entryEndDrag.eventID = EventTriggerType.EndDrag;
        entryEndDrag.callback.AddListener((data) =>
        {
        });
        trigger.triggers.Add(entryEndDrag);

        // Add CanvasGroup
        CanvasGroup cg = card.gameObject.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = card.gameObject.AddComponent<CanvasGroup>();
        }
        cg.blocksRaycasts = true;
        cg.interactable = true;


    }
    public void InitPlayerCard()
    {
        if (sizeCardW <= 0)
        {
            Card tempCard = spawnCard();
            RectTransform rect = tempCard.GetComponent<RectTransform>();
            if (rect != null)
            {
                sizeCardW = rect.rect.width * SCALE_CARD;
                Debug.Log($"[InitPlayerCard] Recalculate sizeCardW={sizeCardW}, rectW={rect.rect.width}, scale={SCALE_CARD}");
            }
            Destroy(tempCard.gameObject);
        }
        int index = (int)(thisPlayer.vectorCard.Count / 2 * -1);
        Debug.Log("Tienlen index: " + index);

        foreach (var player in players)
        {
            if (player == thisPlayer && stateGame == STATE_GAME.VIEWING)
                continue;

            int siC = player.vectorCard.Count;
            int siD = player.vectorCardD.Count;

            player.numberCard = siC;

            int idexPos = player._indexDynamic;
            Vector2 posC = listPosCard[idexPos];
            Vector2 posCardD = listPosCardDanh[idexPos];

            for (int j = 0; j < siC; j++)
            {
                Card card = player.vectorCard[j];
                card.gameObject.SetActive(true);
                card.transform.SetParent(m_ContainerCards);
                card.transform.SetSiblingIndex((int)GAME_ZORDER.Z_CARD + j);

                card.setTextureWithCode(card.code);
                card.transform.SetParent(m_ContainerCards);
                if (player == thisPlayer)
                {
                    card.transform.localScale = Vector3.one * SCALE_CARD;
                    card.transform.localPosition = listPosCard[0]; // tạm đặt gốc
                }

                else
                {
                    card.transform.localScale = Vector3.one * SCALE_CARD_OTHER;
                    card.transform.localPosition = posC;
                }
            }

            for (int j = 0; j < player.vectorCardD.Count; j++)
            {
                Card card = player.vectorCardD[j];
                posCardD = GetPositionCardOnDesk(idexPos, j);

                card.gameObject.SetActive(true);
                card.transform.SetParent(m_ContainerCards);
                card.transform.SetSiblingIndex((int)GAME_ZORDER.Z_CARD + j);
                card.setTextureWithCode(card.code);

                foreach (var tonList in player.listTon)
                {
                    if (tonList.Contains(card.code))
                    {
                        card.StateCatteBorder(true);
                        card.setDark(false);
                    }
                }
                card.transform.localScale = Vector3.one * SCALE_CARD_DANH;
                card.transform.localPosition = posCardD;
            }
        }

        if (stateGame == STATE_GAME.PLAYING)
        {
            SortCardView();
        }
    }
    private Vector2 GetPositionCardOnDesk(int indexPlayer, int indexCard)
    {
        float posY = (indexPlayer == 0 || indexPlayer == 1 || indexPlayer == 5) ? 90f : -90f;
        if (indexCard <= 3)
            return new Vector2(
                listPosCardDanh[indexPlayer].x + (150f * SCALE_CARD_DANH) * indexCard,
                listPosCardDanh[indexPlayer].y
            );
        else
            return new Vector2(
                listPosCardDanh[indexPlayer].x + (150f * SCALE_CARD_DANH) * (indexCard - 3),
                listPosCardDanh[indexPlayer].y + posY
            );
    }

    public void UpdateHitPot(int idUserWinPot = 0)
    {
        for (int i = 0; i < players.Count; i++)
        {
            PlayerViewTienlen playerView = getPlayerView(players[i]);
            playerView.imageIconPot.gameObject.SetActive(false);
        }
        if (idUserWinPot != 0)
        {
            var playerWinPot = getPlayerWithID(idUserWinPot);
            if (playerWinPot != null)
            {
                nodePotIcons[0].SetActive(true);

                int index = playerWinPot._indexDynamic;
                // Debug.Log($"players.Count: {players.Count}//index: {index}");
                PlayerViewTienlen playerView = getPlayerView(playerWinPot);
                playerView.imageIconPot.gameObject.SetActive(true);
            }
            // Debug.Log($"idUserWinPot: {idUserWinPot}//playerView: {getPlayerView(playerWinPot)}");
        }
    }

    private IEnumerator HandleLcSequence(JObject data)
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("Chia bai");

        ChiaBai();

        yield return new WaitForSeconds(2.8f);
        dangPhatBai = false;

        HandleGame.nextEvt();

        bool isFinish = data["isFinish"]?.ToObject<bool>() ?? false;
        if (isFinish)
        {
            HandleShowCard(data);
            DOVirtual.DelayedCall(0.5f, () =>
                       {
                           isChiabai = false;
                       });
            yield break;
        }

        int nextTurn = data["nextTurn"]?.ToObject<int>() ?? -1;
        int round = data["round"]?.ToObject<int>() ?? 1;

        Player player = getPlayerWithID(nextTurn);
        if (player == null) yield break;

        SetTurn(player.namePl, timeTurn);
        turnIdCurrent = nextTurn;

        int index = player._indexDynamic;
        SetRound(round);
        SetLightTurn(index);
        isChiabai = false;
    }


    public void SetTurn(string turnName, float time = 20f)
    {
        // 1. Tắt trạng thái của player cũ
        if (turnCurrentPlayer == null)
        {
            foreach (var player in players)
            {
                player.setTurn(false);
            }
        }
        else
        {
            turnCurrentPlayer.setTurn(false);
        }
        turnCurrentPlayer = getPlayer(turnName);
        Debug.Log($"------------------------------> TURN    {turnName}");
        Debug.Log(turnCurrentPlayer);

        // 3. Nếu tìm thấy player
        if (turnCurrentPlayer != null)
        {
            turnCurrentPlayer.setTurn(true, time);

            if (turnCurrentPlayer == thisPlayer)
            {
                btnBeat.gameObject.SetActive(false);
                btnDown.gameObject.SetActive(false);

                Card cardCompare = null;
                foreach (var card in thisPlayer.vectorCard)
                {
                    if (card.isSelect)
                    {
                        cardCompare = card;
                        break;
                    }
                }

                if (cardBefore == null && cardCompare != null)
                {
                    btnBeat.gameObject.SetActive(true);
                }
                else if (cardCompare == null)
                {
                    // Không làm gì nếu chưa chọn lá nào
                }
                else
                {
                    if (cardCompare.S == cardBefore.S &&
                        cardCompare.N > cardBefore.N &&
                        cardBefore.GetStateBorder() &&
                        isCheckCardBefore)
                    {
                        btnBeat.gameObject.SetActive(true);
                    }
                    else
                    {
                        btnDown.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
    private void SetRound(int round)
    {
        if (lbRound != null)
        {
            lbRound.gameObject.SetActive(true);
            lbRound.text = "Round " + round;
        }
    }
    public void SetLightTurn(int id)
    {
        if (lightTurn == null || id < 0 || id >= listLightTurnRotation.Count || id >= listLightTurnScale.Count)
            return;

        lightTurn.SetActive(true);
        StopCoroutine("AnimateLightTurn");
        StartCoroutine(AnimateLightTurn(id));
    }
    private IEnumerator AnimateLightTurn(int id)
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Quaternion startRot = lightTurn.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0, 0, listLightTurnRotation[id]);

        Vector3 startScale = lightTurn.transform.localScale;
        Vector3 targetScale = new Vector3(1f, listLightTurnScale[id], 1f);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = Mathf.Pow(t - 1, 3) + 1; // EaseOutCubic

            lightTurn.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            lightTurn.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        lightTurn.transform.rotation = targetRot;
        lightTurn.transform.localScale = targetScale;
    }
    private void ChiaBai() { StartCoroutine(ChiaBaiRoutine()); }
    private IEnumerator ChiaBaiRoutine()
    {
        dangPhatBai = true;

        int numCards = thisPlayer != null ? thisPlayer.vectorCard.Count : 0;

        for (int cardIndex = 0; cardIndex < numCards; cardIndex++)
        {
            List<IEnumerator> runningCoroutines = new List<IEnumerator>();

            foreach (Player player in players)
            {
                if (player == null) continue;
                if (cardIndex >= player.vectorCard.Count) continue;

                Card card = player.vectorCard[cardIndex];
                if (card == null || card.gameObject == null) continue;

                card.isTouch = true;
                card.gameObject.SetActive(true);
                card.transform.localEulerAngles = new Vector3(0, 0, -90);

                if (player == thisPlayer)
                {
                    Vector3 targetPos = listPosCard[0] + new Vector2(sizeCardW * cardIndex, 0);
                    yield return StartCoroutine(MoveScaleFlipCard(card, targetPos, cardIndex));
                }
                else
                {
                    int idex = player._indexDynamic;
                    Vector3 targetPos = listPosCard[idex];
                    yield return StartCoroutine(MoveCardToOther(card, targetPos, cardIndex, player));
                }
            }

            yield return new WaitForSeconds(0.02f);
        }

        dangPhatBai = false;
    }




    private IEnumerator MoveScaleFlipCard(Card card, Vector3 targetPos, int zOrder)
    {
        float duration = 0.2f;

        Sequence seq = DOTween.Sequence();
        seq.Join(card.transform.DOLocalMove(targetPos, duration).SetEase(Ease.OutCubic));
        seq.Join(card.transform.DOScale(Vector3.one * SCALE_CARD, duration).SetEase(Ease.OutCubic));
        seq.Join(card.transform.DOLocalRotate(Vector3.zero, duration).SetEase(Ease.OutCubic));
        yield return seq.WaitForCompletion();
        card.transform.localScale = new Vector3(0f, SCALE_CARD, 1f);
        yield return new WaitForSeconds(0.08f);
        SoundManager.instance.playEffectFromPath("Sounds/chiabai");
        card.setTextureWithCode(card.code);
        card.transform.localScale = Vector3.one * SCALE_CARD;
        yield return new WaitForSeconds(0.02f);
    }



    private IEnumerator MoveCardToOther(Card card, Vector3 targetPos, int zOrder, Player player)
    {
        float duration = 0.12f;
        card.isMoved = true;
        Sequence seq = DOTween.Sequence();
        seq.Join(card.transform.DOLocalMove(targetPos, duration).SetEase(Ease.InOutCubic));
        seq.Join(card.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic));

        yield return seq.WaitForCompletion();

        player.numberCard++;

        SoundManager.instance.playEffectFromPath("Sounds/chiabai");
    }


    public void AllCardDown()
    {
        if (thisPlayer.vectorCard.Count < 1) return;

        Vector2 POS_CARD = listPosCard[0];
        numCardSelect = 0;

        foreach (var card in thisPlayer.vectorCard)
        {
            float posX = card.transform.localPosition.x;
            card.isSelect = false;

            // Bắt đầu coroutine để scale và move
            StartCoroutine(MoveAndScaleCard(card, new Vector3(posX, POS_CARD.y, 0), SCALE_CARD));
        }
    }
    private IEnumerator MoveAndScaleCard(Card card, Vector3 targetPosition, float targetScale)
    {
        Debug.Log($"MoveAndScaleCard");
        float moveDuration = 0.2f;
        float scaleDuration = 0.4f;

        float elapsedMove = 0f;
        float elapsedScale = 0f;

        Vector3 startPos = card.transform.localPosition;
        Vector3 startScale = card.transform.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        while (elapsedMove < moveDuration || elapsedScale < scaleDuration)
        {
            if (elapsedMove < moveDuration)
            {
                elapsedMove += Time.deltaTime;
                float tMove = Mathf.Clamp01(elapsedMove / moveDuration);
                card.transform.localPosition = Vector3.Lerp(startPos, targetPosition, EaseOutCubic(tMove));
            }

            if (elapsedScale < scaleDuration)
            {
                elapsedScale += Time.deltaTime;
                float tScale = Mathf.Clamp01(elapsedScale / scaleDuration);
                card.transform.localScale = Vector3.Lerp(startScale, endScale, EaseOutCubic(tScale));
            }

            yield return null;
        }
        card.transform.localPosition = targetPosition;
        card.transform.localScale = endScale;
    }
    public void HandleShowCard(JObject data)
    {
        if (data == null)
        {
            Debug.LogError("HandleShowCard: data = null");
            return;
        }
        int uid = data["uid"]?.Value<int>() ?? -1;
        int idCard = data["idCard"]?.Value<int>() ?? -1;
        int round = data["round"]?.Value<int>() ?? 0;
        int nextUserId = data["nextUserId"]?.Value<int>() ?? -1;
        int timeOut = data["timeOut"]?.Value<int>() ?? 0;
        bool isFinishGame = data["isfinishGame"]?.Value<bool>() ?? false;

        if (uid == -1 || idCard == -1)
        {
            Debug.LogWarning($"HandleShowCard: dữ liệu không hợp lệ => {data}");
            return;
        }

        Player player = getPlayerWithID(uid);
        if (player == null) return;

        int indexPlayer = player._indexDynamic;

        if (offBlockEvent != null)
            StopCoroutine(offBlockEvent);

        OffTouchCard();
        offBlockEvent = StartCoroutine(DelayAction(1.3f, () =>
        {
            // if (this != null && this.gameObject != null)
            //     OnTouchCard();
        }));

        Card card = null;
        Vector3 posShowCard = GetPositionCardOnDesk(indexPlayer, 6 - player.vectorCard.Count);

        if (player != thisPlayer)
        {
            if (player.vectorCard.Count > 0 && player.vectorCard[0] != null)
            {
                card = player.vectorCard[0];
                card.decodeCard(idCard);
                card.setTextureWithCode(idCard);

                player.vectorCardD.Add(card);
                player.vectorCard.RemoveAt(0);
            }
        }
        else
        {
            for (int i = 0; i < player.vectorCard.Count; i++)
            {
                Card c = player.vectorCard[i];
                if (c == null) continue;

                if (c.code == idCard)
                {
                    card = c;
                    card.isTouch = false;
                    card.isSelect = false;

                    player.vectorCardD.Add(card);
                    player.vectorCard.RemoveAt(i);
                    break;
                }
            }
        }

        if (card == null) return;

        card.setOpacity(255);

        if (cardBefore != null)
        {
            cardBefore.StateCatteBorder(false);
            cardBefore.setDark(true);
        }

        card.StateCatteBorder(true);
        card.setDark(false);

        if (roundCurrent == round)
        {
            cardBefore = card;
            isCheckCardBefore = true;
        }
        else
        {
            isCheckCardBefore = false;
            cardBefore = null;
            roundCurrent = round;
        }

        int id = idCard;
        StartCoroutine(AnimateCardMoveAndScale(card, posShowCard, Vector3.one * SCALE_CARD_DANH, id));

        player.setTurn(false);

        if (player == thisPlayer)
        {
            SortCardView();
            btnBeat.gameObject.SetActive(false);
            btnDown.gameObject.SetActive(false);
        }

        if (isFinishGame) return;

        Player playerNextTurn = getPlayerWithID(nextUserId);
        if (playerNextTurn == null) return;

        int indexPlayerNextTurn = getDynamicIndex(getIndexOf(playerNextTurn));

        SetRound(round);
        if (indexPlayerNextTurn == 2 && players.Count == 2)
            SetLightTurn(indexPlayerNextTurn + 1);
        else
            SetLightTurn(indexPlayerNextTurn);

        SetTurn(playerNextTurn.namePl, timeOut);
        turnIdCurrent = nextUserId;
    }

    private IEnumerator DelayAction(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
    private IEnumerator AnimateCardMoveAndScale(Card card, Vector3 targetPos, Vector3 targetScale, int zOrder)
    {
        float duration = 0.4f;
        float elapsed = 0f;

        Vector3 startPos = card.transform.localPosition;
        Vector3 startScale = card.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = EaseOutCubic(t);

            card.transform.localPosition = Vector3.Lerp(startPos, targetPos, easedT);
            card.transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);

            yield return null;
        }

        card.transform.localPosition = targetPos;
        card.transform.localScale = targetScale;
        card.transform.SetSiblingIndex((int)GAME_ZORDER.Z_CARD + zOrder);
        card.isMoved = true;
    }
    public void HandleDropCard(JObject data)
    {
        // Parse dữ liệu từ JObject
        int uid = data["uid"]?.ToObject<int>() ?? -1;
        int idCard = data["idCard"]?.ToObject<int>() ?? -1;
        int round = data["round"]?.ToObject<int>() ?? 1;
        int nextUserId = data["nextUserId"]?.ToObject<int>() ?? -1;
        float timeOut = data["timeOut"]?.ToObject<float>() ?? 0f;
        bool isFinishGame = data["isfinishGame"]?.ToObject<bool>() ?? false;

        Player player = getPlayerWithID(uid);
        if (player == null) return;

        int indexPlayer = player._indexDynamic;

        CancelInvoke(nameof(OnTouchCard));
        OffTouchCard();
        Invoke(nameof(OnTouchCard), 1.3f);

        for (int i = 0; i < player.vectorCard.Count; i++)
        {
            Card card = null;
            Vector3 posDropCard = GetPositionCardOnDesk(indexPlayer, 6 - player.vectorCard.Count);

            if (player != thisPlayer)
            {
                card = player.vectorCard[0];
                card.decodeCard(idCard);
                player.vectorCardD.Add(card);
                player.vectorCard.RemoveAt(0);
            }
            else
            {
                if (player.vectorCard[i].code == idCard)
                {
                    card = player.vectorCard[i];
                    card.isTouch = false;
                    card.isSelect = false;
                    player.vectorCardD.Add(card);
                    player.vectorCard.RemoveAt(i);
                }
                else
                {
                    continue;
                }
            }

            // Xử lý opacity
            var cardCanvasGroup = card.GetComponent<CanvasGroup>();
            if (cardCanvasGroup == null) cardCanvasGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardCanvasGroup.alpha = 1f;

            if (roundCurrent > 4)
            {
                cardCanvasGroup.alpha = 0.78f;
                card.setTextureWithCode(idCard);
            }
            else
            {
                card.setTextureWithCode(-1);
            }

            card.StateCatteBorder(false);

            if (roundCurrent == round)
            {
                isCheckCardBefore = true;
            }
            else
            {
                roundCurrent = round;
                isCheckCardBefore = false;
                cardBefore = null;
            }

            // Animate card move
            card.transform.SetAsLastSibling();
            card.transform.DOLocalMove(posDropCard, 0.4f).SetEase(Ease.OutCubic);
            card.transform.DOScale(new Vector3(SCALE_CARD_DANH, SCALE_CARD_DANH, 1), 0.4f)
                .OnComplete(() => player.setTurn(false));
            card.isMoved = true;
            break;
        }

        if (roundCurrent == round)
        {
            roundCurrent = round;
        }

        if (player == thisPlayer)
        {
            SortCardView();
            btnBeat.gameObject.SetActive(false);
            btnDown.gameObject.SetActive(false);
        }

        if (isFinishGame) return;

        Player playerNextTurn = getPlayerWithID(nextUserId);
        int indexPlayerNextTurn = getDynamicIndex(getIndexOf(playerNextTurn));
        SetTurn(playerNextTurn.namePl, timeOut);
        turnIdCurrent = nextUserId;
        SetRound(round);
        if (indexPlayerNextTurn == 2 && players.Count == 2)
        {
            SetLightTurn(indexPlayerNextTurn + 1);
        }
        else
        {
            SetLightTurn(indexPlayerNextTurn);
        }

    }

    public async Task HandleFinishGame(JObject data)
    {
        while (isChiabai)
        {
            await UniTask.Delay(50);
        }
        Debug.Log($"[HandleFinishGame] Raw JObject: {data?.ToString(Formatting.None)}");
        btnBeat.gameObject.SetActive(false);
        btnDown.gameObject.SetActive(false);

        lightTurn.transform.localScale = Vector3.zero;

        string jsonData = data["data"]?.ToString();
        if (string.IsNullOrEmpty(jsonData)) return;

        List<PlayerResultData> dataListPlayer = JsonConvert.DeserializeObject<List<PlayerResultData>>(jsonData);
        int agPot = data["agPot"]?.ToObject<int>() ?? 0;

        float timeWinSpecial = 0f;
        foreach (var playerData in dataListPlayer)
        {
            if (playerData.typeWin > 0 && playerData.isBigWin)
            {
                timeWinSpecial = 3.0f;
                break;
            }
        }

        Sequence finishSequence = DOTween.Sequence();

        finishSequence.AppendCallback(() =>
        {
            EffectAnimFinish();
        });

        finishSequence.AppendInterval(1.5f);

        finishSequence.AppendCallback(() =>
        {
            EffectWinSpecial(dataListPlayer);
        });

        finishSequence.AppendInterval(timeWinSpecial);

        finishSequence.AppendCallback(() =>
        {
            EffectBeforeFinish(dataListPlayer);
        });

        finishSequence.AppendInterval(1.5f);

        finishSequence.AppendCallback(() =>
        {
            totalPot = agPot;
            EffectHitpot(agPot, dataListPlayer);
            stateGame = STATE_GAME.WAITING;
        });

        finishSequence.AppendInterval(4f);

        finishSequence.AppendCallback(() =>
        {
            nodeWinHitPot.SetActive(false);
            lbRound.text = "";
            // OnTouchCard();

            cardBefore = null;
            isCheckCardBefore = false;
            numCardSelect = 0;

            clearAllCard();
            ClearLostRound();

            HandleGame.nextEvt();
        });
        foreach (List<Card> cardList in ListCardPlayer)
        {
            if (cardList != null)
            {
                for (int i = cardList.Count - 1; i >= 0; i--)
                {
                    if (cardList[i] != null)
                    {
                        cardList[i].gameObject.SetActive(false);
                    }
                    cardList.RemoveAt(i);
                }
            }
        }
        HandleData.DelayHandleLeave = 0f;
        stateGame = Globals.STATE_GAME.WAITING;
        checkAutoExit();
        foreach (Transform child in m_ContainerCards)
        {
            DOTween.Kill(child);
            Destroy(child.gameObject);
        }
    }

    public void OnClickShowCard()
    {
        List<int> vtCard = new List<int>();

        foreach (var card in thisPlayer.vectorCard)
        {
            if (card.isSelect)
            {
                Debug.Log("-=-==--=-=-=-=-=-==---=-=-=->>>>>>>>> catte: " + card.nameCard);
                vtCard.Add(card.code);
            }
        }

        if (vtCard.Count == 1)
        {
            SocketSend.SendShowCardCatte(vtCard[0]);
            numCardSelect = 0;
        }
    }
    public void OnClickDropCard()
    {
        List<int> vtCard = new List<int>();

        foreach (var card in thisPlayer.vectorCard)
        {
            if (card.isSelect)
            {
                Debug.Log("=-=-==--=-=-=-=-=-==---=-=-=->>>>>>>>> catte: " + card.nameCard);
                vtCard.Add(card.code);
            }
        }

        if (vtCard.Count == 1)
        {
            SocketSend.SendDropCardCatte(vtCard[0]);
            numCardSelect = 0;
        }
    }
    public int GetIndexCard(int idCard, Player player)
    {
        for (int i = 0; i < player.vectorCard.Count; i++)
        {
            if (player.vectorCard[i].code == idCard)
            {
                return i;
            }
        }
        return -1;
    }
    public void HandleLost4Round(JObject data)
    {
        int pid = data["pid"]?.ToObject<int>() ?? -1;

        Player player = getPlayerWithID(pid);
        if (player == null) return;

        GameObject nodeLost4Round = new GameObject("nodeLost4Round");
        SkeletonGraphic animLose = nodeLost4Round.AddComponent<SkeletonGraphic>();

        animLose.skeletonDataAsset = aniLost4Roud;
        // animLose.loop = true;
        animLose.Initialize(true);
        animLose.AnimationState.SetAnimation(0, "cam_chay", true);/* = "cam_chay";*/
        Vector3 posAnimLose = new();
        // if (players.Count == 2)
        // {
        //     if (player._indexDynamic == 3)
        //     {
        //         posAnimLose = nodeTableBoxs[player._indexDynamic - 1].transform.localPosition;
        //     }
        //     else
        //     {
        //         posAnimLose = nodeTableBoxs[player._indexDynamic].transform.localPosition;
        //     }
        // }
        // else
        {
            posAnimLose = nodeTableBoxs[player._indexDynamic].transform.localPosition;
        }
        Debug.Log($"Tinh=)) posAnimLose: {player._indexDynamic}");
        nodeLost4Round.transform.SetParent(this.transform);
        nodeLost4Round.transform.localScale = Vector3.one;
        nodeLost4Round.transform.SetAsLastSibling();
        nodeLost4Round.transform.localPosition = posAnimLose;

        listLostRound.Add(nodeLost4Round);
        StartCoroutine(DestroyAfterDelay(nodeLost4Round, animLose.gameObject, 1.5f));
    }

    public void HandleAddPot(JObject data)
    {
        Debug.Log("xem là data handleAddpot như nào" + data);
        int pid = data["pid"]?.ToObject<int>() ?? -1;
        int agAddPot = data["agAddPot"]?.ToObject<int>() ?? 0;
        long serverPot = data["pot"]?.ToObject<long>() ?? -1;
        long agUser = data["agUser"]?.ToObject<long>() ?? 0;

        Player player = getPlayerWithID(pid);
        if (player == null) return;
        player.ag = agUser;
        player.updateMoney();
        long newPot = serverPot >= 0 ? serverPot : (totalPot + agAddPot);
        int deltaPot = (int)(newPot - totalPot);
        if (deltaPot > 0 && agAddPot > 0)
        {
            player.playerView.effectFlyMoney(-Math.Abs(agAddPot));
        }
        SetPot(newPot);
        Debug.Log($"newpot: {newPot}//totalpot: {totalPot}//serverpot: {serverPot}");
    }

    public void HandlePunish(JObject data, bool showWhenZero = false)
    {
        JArray playersArray = data["players"] as JArray;
        if (playersArray == null)
        {
            Debug.LogWarning("[HandlePunish] Không có mảng 'players' trong data");
            return;
        }

        foreach (JObject playerData in playersArray)
        {
            int pid = playerData["pid"]?.ToObject<int>() ?? -1;
            long ag = playerData["ag"]?.ToObject<long>() ?? 0;
            int M = playerData["M"]?.ToObject<int>() ?? 0;

            Debug.Log($"[HandlePunish] pid={pid} | ag={ag} | M={M}");

            Player player = getPlayerWithID(pid);
            if (player == null)
            {
                Debug.LogWarning($"[HandlePunish] Không tìm thấy Player ID={pid}");
                continue;
            }

            // Cập nhật tiền hiện tại
            player.ag = ag;
            player.setAg();

            // Đảm bảo playerView đã active
            if (player.playerView == null)
            {
                Debug.LogWarning($"[HandlePunish] playerView null cho Player ID={pid}");
                continue;
            }
            if (!player.playerView.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[HandlePunish] playerView inactive cho Player ID={pid}, bật lại để hiển thị text");
                player.playerView.gameObject.SetActive(true);
            }

            // Nếu muốn luôn hiện text khi M=0 => bỏ qua điều kiện trong effectFlyMoney
            if (M == 0 && !showWhenZero)
            {
                Debug.Log($"[HandlePunish] Bỏ qua Player ID={pid} vì M=0");
            }
            else
            {
                player.playerView.effectFlyMoney(M);
            }

            player.updateMoney();
        }
    }


    public void OnClickHistory()
    {
        // if (stateGame == STATE_GAME.VIEWING)
        //     return;

        if (popupHistory == null)
        {
            GameObject popupObj = Instantiate(catteHistory, transform);
            popupHistory = popupObj.GetComponent<CatteHistory>();
        }
        else
        {
            popupHistory.OnPopOn();
            SocketSend.SendHistoryCatte();
        }

        if (popupHistory.transform.parent == null)
        {
            popupHistory.transform.SetParent(transform, false);
            popupHistory.OnPopOn();
            SocketSend.SendHistoryCatte();
        }
    }
    public void HandleHistory(JObject data)
    {
        if (data == null || data["gameInfo"] == null) return;
        List<HistoryData> gameInfoList = data["gameInfo"].ToObject<List<HistoryData>>();
        gameInfoList.Sort((a, b) => b.m.CompareTo(a.m));
        popupHistory?.Init(gameInfoList);
    }
    public void OnClickShop()
    {
        string sceneName = Globals.CURRENT_VIEW.getCurrentSceneName();
        Application.ExternalCall("SMLSocketIO.getInstance().emitSIOCCCNew", $"ClickShop_{sceneName}");
        UIManager.instance.openShop();
        SoundManager.instance.soundClick();
    }


    public void ClearLostRound()
    {
        foreach (var node4LostRound in listLostRound)
        {
            if (node4LostRound != null)
            {
                Destroy(node4LostRound);
            }
        }
        listLostRound.Clear();
    }
    public void EffectHitpot(long agPotCurrent, List<PlayerResultData> dataListPlayer)
    {
        Debug.Log($"Xem chỗ effectHitpot: {agPotCurrent}");
        if (agPotCurrent > 0)
        {
            nodePotIcons[0].SetActive(true);
            for (int i = 0; i < players.Count; i++)
            {
                PlayerViewTienlen playerView = getPlayerView(players[i]);
                playerView.imageIconPot.gameObject.SetActive(false);
            }
            foreach (var dataPlayer in dataListPlayer)
            {
                Player player = getPlayerWithID(dataPlayer.uid);
                if (dataPlayer.M > 0)
                {
                    Debug.Log($"players.Count: {players.Count}//dataPlayer.uid: {dataPlayer.uid}");
                    PlayerViewTienlen playerView = getPlayerView(player);
                    playerView.imageIconPot.gameObject.SetActive(true);
                    idUserWinPot = dataPlayer.uid;
                    break;
                }
            }
        }
        else
        {
            foreach (var dataPlayer in dataListPlayer)
            {
                Player player = getPlayerWithID(dataPlayer.uid);
                if (dataPlayer.agwinPot > 0)
                {
                    void Act1()
                    {
                        nodePotIcons[0].SetActive(true);
                        PlayerViewTienlen playerView = getPlayerView(player);
                        playerView.imageIconPot.gameObject.SetActive(false);
                        // nodePlayerIcons[player._indexDynamic].SetActive(false);

                        // GameObject nodeInstance = Instantiate(nodePlayerIcons[player._indexDynamic], node.transform);
                        // GameObject nodeInstanceNext = Instantiate(nodePlayerIcons[player._indexDynamic + 1], node.transform);
                        // Debug.Log($"players.Count: {players.Count}//index: {player._indexDynamic}");
                        // if (players.Count == 2 && player._indexDynamic == 1)
                        // {
                        //     nodeInstanceNext.SetActive(true);
                        // }
                        // else
                        // {
                        //     nodeInstance.SetActive(true);
                        // }
                        Vector3 targetPos = new Vector3(232, 303, 0);
                        // nodeInstance.transform.DOMove(targetPos, 2f).SetEase(Ease.InOutCubic).OnComplete(() =>
                        // {
                        //     Destroy(nodeInstance);
                        // });
                    }

                    void Act2()
                    {
                        nodeWinHitPot.SetActive(true);
                        nodePotIcons[0].SetActive(false);

                        // GameObject animWinHitpot = nodeWinHitPot.transform.Find("animWinHitpot").gameObject;
                        // var skeleton = animWinHitpot.GetComponent<SkeletonAnimation>();
                        // skeleton.AnimationName = "animation";
                        // skeleton.loop = false;

                        // var lbWinHitpot = animWinHitpot.transform.Find("lbWinHitpot").GetComponent<Text>();
                        // lbWinHitpot.text = Config.FormatNumber(dataPlayer.agwinPot);
                        animWinHitPot.AnimationState.SetAnimation(0, "animation", false);
                        lbWinHitPot.text = Config.FormatNumber(dataPlayer.agwinPot);
                        DOVirtual.DelayedCall(1f, () =>
                        {
                            Debug.Log("có khi đây ");

                            foreach (var dataPlayer in dataListPlayer)
                            {
                                Player player = getPlayerWithID(dataPlayer.uid);
                                if (player == null) continue;
                                player.updateMoney();
                            }
                        });
                    }

                    StartCoroutine(SequenceActions(Act1, 2.0f, Act2));
                    idUserWinPot = 0;
                    break;
                }
            }
        }
        totalPot = (int)agPotCurrent;
        lbValuePot.text = Config.FormatNumber(totalPot);
    }

    private IEnumerator SequenceActions(System.Action act1, float delay, System.Action act2)
    {
        act1?.Invoke();
        yield return new WaitForSeconds(delay);
        act2?.Invoke();
    }

    public void EffectWinSpecial(List<PlayerResultData> dataListPlayer)
    {
        foreach (var dataPlayer in dataListPlayer)
        {
            if (dataPlayer.typeWin > 0 && dataPlayer.isBigWin)
            {
                Player player = getPlayerWithID(dataPlayer.uid);
                if (player == null) continue;

                // Load avatar
                Avatar avatar = avtSpecial.GetComponent<Avatar>();
                if (avatar != null)
                {
                    avatar.loadAvatar(player.avatar_id, player.namePl, player.fid);
                }

                // Hiển thị các lá bài
                for (int i = 0; i < dataPlayer.arrWin.Count; i++)
                {
                    // Tạo card từ prefab
                    GameObject cardObj = Instantiate(cardPrefab, aniWinSpecial.transform, false);

                    if (cardObj == null)
                    {
                        Debug.LogError($"[EffectWinSpecial] cardObj[{i}] == null khi Instantiate từ prefab");
                        continue;
                    }

                    cardObj.transform.localScale = Vector3.one * SCALE_CARD;

                    // Gán texture theo code
                    var cardComp = cardObj.GetComponent<Card>();
                    if (cardComp != null)
                    {
                        Debug.Log($"[EffectWinSpecial] SetTexture card {i} với code={dataPlayer.arrWin[i]}");
                        cardComp.setTextureWithCode(dataPlayer.arrWin[i]);
                    }
                    else
                    {
                        Debug.LogWarning($"[EffectWinSpecial] cardObj[{i}] không có component Card");
                    }

                    // Set vị trí
                    var rect = cardObj.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        Vector2 posCard = listPosCard[0] + new Vector2(sizeCardW * i, 0);
                        rect.anchoredPosition = posCard;
                    }

                    cardObj.transform.SetAsLastSibling();
                }




                // Bật hiệu ứng
                if (aniWinSpecial != null && aniWinSpecial.transform.parent != null)
                {
                    aniWinSpecial.transform.parent.gameObject.SetActive(true);
                }
                lbName.gameObject.SetActive(true);
                lbName.text = player.namePl;

                string animationName = "";
                switch (dataPlayer.typeWin)
                {
                    case 1: animationName = "Four of a kind_cam"; break;
                    case 2: animationName = "Same Color_cam"; break;
                    case 3: animationName = "All Under 6_cam"; break;
                }

                if (!string.IsNullOrEmpty(animationName))
                {
                    aniWinSpecial.AnimationState.SetAnimation(0, animationName, false);
                }

                break; // chỉ xử lý 1 người thắng đặc biệt
            }
        }
    }


    public void EffectAnimFinish()
    {
        animStartGame.gameObject.SetActive(true);

        animStartGame.AnimationState.SetAnimation(0, "finish2", false);
        StartCoroutine(DisableAnimStartGameAfterDelay(1.5f));
    }
    public void EffectBeforeFinish(List<PlayerResultData> dataListPlayer)
    {
        aniWinSpecial.transform.parent.gameObject.SetActive(false);
        foreach (var dataPlayer in dataListPlayer)
        {
            if (dataPlayer.M < 0)
            {
                Player player = getPlayerWithID(dataPlayer.uid);
                if (player == null) continue;
                Debug.Log($"EffectBeforeFinish - uid: {dataPlayer.uid}, M: {dataPlayer.M}");
                player.playerView.effectFlyMoney(dataPlayer.M);
                player.updateMoney();
                player.ag += dataPlayer.M;
                player.setAg();
            }
        }
        foreach (var dataPlayer in dataListPlayer)
        {
            if (dataPlayer.M > 0)
            {
                Player player = getPlayerWithID(dataPlayer.uid);
                if (player == null) continue;
                Debug.Log($"EffectBeforeFinish - uid: {dataPlayer.uid}, M: {dataPlayer.M}");
                player.playerView.effectFlyMoney(dataPlayer.M);
                player.updateMoney();
                player.ag += dataPlayer.M;
                player.setAg();
            }
        }
        foreach (var dataPlayer in dataListPlayer)
        {
            Player player = getPlayerWithID(dataPlayer.uid);
            if (player == null) continue;

            player.M = dataPlayer.M;
            player.ag = dataPlayer.ag;
            player.isPenaltyUser = dataPlayer.isPenaltyUser;
            player.idCardPenalty = dataPlayer.idCardPenalty;
            player.agwinPot = dataPlayer.agwinPot;

            // player.playerView.effectFlyMoney(dataPlayer.M);
            // int effectWinLose = (dataPlayer.M > 0) ? 1 : -1;
            // int effectWinLose;
            // if (dataPlayer.M < 0) effectWinLose = 0;   // lose
            // else if (dataPlayer.M == 0) effectWinLose = 1; // draw
            // else effectWinLose = 2;
            // player.playerView.ShowEffectWinLose(effectWinLose);
            // player.updateMoney();
            foreach (var card in player.vectorCard)
            {
                card.StateCatteBorder(false);
                card.setDark(true);
            }

            if (player.isPenaltyUser)
            {
                GameObject nodeAniPenalty = new GameObject("nodeAniPenalty");
                var animPenalty = nodeAniPenalty.AddComponent<SkeletonGraphic>();
                animPenalty.skeletonDataAsset = aniLost4Roud;
                animPenalty.initialSkinName = "default";
                animPenalty.Initialize(true);
                animPenalty.AnimationState.SetAnimation(0, "penalty", true);/* = "penalty";*/
                // animPenalty.loop = true;

                Vector3 posAnimPen = nodeTableBoxs[player._indexDynamic].transform.localPosition;
                nodeAniPenalty.transform.SetParent(m_ContainerCards);
                nodeAniPenalty.transform.localPosition = posAnimPen;
                nodeAniPenalty.transform.SetAsLastSibling();
                Card cardPenalty = getCard();
                cardPenalty.transform.localScale = Vector3.one * 0.2f;
                cardPenalty.setTextureWithCode(player.idCardPenalty);
                cardPenalty.StateCatteBorder(false);
                cardPenalty.setDark(true);
                cardPenalty.transform.SetParent(m_ContainerCards);
                cardPenalty.transform.localPosition = posAnimPen + new Vector3(80, 0, 0);
                StartCoroutine(DestroyAfterDelay(nodeAniPenalty, cardPenalty.gameObject, 3f));
                player.updateMoney();
            }
        }
    }

    private IEnumerator DestroyAfterDelay(GameObject obj1, GameObject obj2, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj1 != null) Destroy(obj1);
        if (obj2 != null) Destroy(obj2);
    }

    private IEnumerator DisableAnimStartGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        animStartGame.gameObject.SetActive(false);
    }
    private void SetPot(long pot, int idUserWinPot = 0)
    {
        totalPot = pot < 0 ? 0 : pot;
        lbValuePot.text = Globals.Config.FormatNumber(totalPot);
    }

    private float EaseOutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
    private float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
}
