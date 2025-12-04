using System.Collections.Generic;
using DG.Tweening;
using System.Linq;
using Newtonsoft.Json.Linq;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections;

public class TienlenView : GameView
{
    public static TienlenView instance;
    private int timeToStart = 0;
    [SerializeField] private TextMeshProUGUI m_TimeToStartText;
    [SerializeField] private GameObject m_BgStart;
    [SerializeField] private Transform m_ContainerCards;
    [SerializeField] private GameObject m_ButtonDiscard;
    [SerializeField] private GameObject m_ButtonCancel;
    [SerializeField] public SkeletonGraphic m_AniStart;
    [SerializeField] public SkeletonGraphic m_AniFinish;
    [SerializeField] public SkeletonGraphic m_AniCardSpecial;
    [SerializeField] public SkeletonGraphic m_AniWinSpecial;
    [SerializeField] public Image m_AvatarSpecial;
    [SerializeField] public TextMeshProUGUI m_NameWin;
    [SerializeField] public List<GameObject> m_TxtPass;
    [SerializeField] public GameObject m_BtnSort;
    [SerializeField] public List<SkeletonGraphic> m_ListAniChay;
    [SerializeField] private GameObject m_PrefabChip; // Prefab của chip
    [SerializeField] private Transform m_ContainerChip;
    [SerializeField] private List<GameObject> m_ListScore;
    [SerializeField] private GameObject m_BgToast;
    [SerializeField] private List<TextMeshProUGUI> m_NumberCardLast;

    public List<List<Card>> ListCardPlayer = new List<List<Card>>();
    public List<List<Card>> ListCardPlayerD = new List<List<Card>>();

    private string turnNameCurrent = "";

    private string lastTurnName = "";
    private int timeTurn = 0;
    private Card selectedCard = null;
    private Vector3 touchStartPos;
    private Vector3 cardStartPos;
    private bool isDragging = false;
    private float cardSpacing = 30f;
    private float dragThreshold = 20f; // Ngưỡng để xác định drag hay tap
    private List<Card> selectedCards = new List<Card>();
    private bool isSpecialHand = false;
    private int typeSort = 2; // Thêm biến để track kiểu sắp xếp
    private Queue<GameObject> chipPool = new Queue<GameObject>(); // Pool các chip
    private int initialPoolSize = 50; // Số lượng chip khởi tạo ban đầu
    private List<GameObject> vtChipFinish = new List<GameObject>();

    private List<Card> listCardSuggest = new List<Card>(); // List chứa các lá bài được gợi ý
    private UniTask? _currentDiscardAnimationTask = null;
    private bool isFirstTurnAndCtable = false;


    private List<Player> playerBackUp = new List<Player>();
    private Player thisPlayerBackup;
    private bool isDanhbai = false;
    private bool isBoluot = false;
    private bool isChiabai = false;
    private List<Vector2> listPostCardChia = new List<Vector2>
    {
        new Vector3(0f,0f,0f),
    new Vector3(498f, 80f, 0),
    new Vector3(40f, 250f, 0),
   new Vector3(-498f, 80f, 0)
};
    private List<Vector2> listPostCardDanh = new List<Vector2>
    {
        new Vector3(0f, -60f, 0),
         new Vector3(358f, 0f, 0),
         new Vector3(20f, 120f, 0),
         new Vector3(-358f, 0f, 0)


    };
    protected override void updatePositionPlayerView()
    {
        int indexPlayer = players.IndexOf(thisPlayer);
        if (indexPlayer > 0)
        {
            List<Player> rotated = new List<Player>();
            rotated.AddRange(players.Skip(indexPlayer));
            rotated.AddRange(players.Take(indexPlayer));
            players = rotated;
        }
        for (int i = 0; i < players.Count; i++)
        {

            if (i < listPosView.Count)
            {
                players[i].playerView.transform.localScale = new Vector2(0.8f, 0.8f);
                players[i].updatePlayerView();
                players[i].playerView.gameObject.SetActive(true);
                players[i].updateItemVip(players[i].vip);
                players[i].playerView.transform.localPosition = listPosView[i];
                if (Globals.Config.typeTable != 1 && players.Count() == 2 && i == 1)
                {
                    players[i].playerView.transform.localPosition = listPosView[2];
                }
            }
            else
            {
                players[i].playerView.gameObject.SetActive(false);
            }
        }
    }
    public override void handleSTable(string data)
    {
        JObject JData = JObject.Parse(data);
        if ((int)JData["tableType"] == 1)
        {
            Globals.Config.typeTable = 1;
            listPosView = new List<Vector2>
   {
    new Vector2(-498f, -220f),
    new Vector2(140f, 263f)
   };
        }
        else
        {
            Globals.Config.typeTable = 0;
        }
        JArray listPlayer = (JArray)JData["ArrP"];
        playerBackUp.Clear();
        for (var i = 0; i < listPlayer.Count; i++)
        {
            var player = new Player();
            playerBackUp.Add(player);
            readDataPlayer(player, (JObject)listPlayer[i]);
            if (player.id == Globals.User.userMain.Userid)
            { //che do test
                thisPlayerBackup = player;
            }
        }
        base.handleSTable(data);
    }
    public override async void handleJTable(string strData)
    {
        if (_finishTaskSource != null)
            await _finishTaskSource.Task;
        Debug.Log(HandleData.DelayHandleLeave + "xem hết bao lâu j");
        var listPlayer = JObject.Parse(strData);
        int newId = (int)listPlayer["id"];
        Player found = playerBackUp.FirstOrDefault(p => p.id == newId);
        if (found != null)
        {
            readDataPlayer(found, listPlayer);
            if (found.id == Globals.User.userMain.Userid)
                thisPlayerBackup = found;
        }
        else
        {
            var player = new Player();
            playerBackUp.Add(player);
            readDataPlayer(player, listPlayer);
            if (player.id == Globals.User.userMain.Userid)
                thisPlayerBackup = player;
        }
        changePlayerToPlayerView();
    }
    private void changePlayerToPlayerView()
    {
        if (instance == null)
        {
            return;
        }
        foreach (Player playerRemove in players)
        {
            if (playerRemove.playerView != null)
            {
                Destroy(playerRemove.playerView.gameObject);
                playerRemove.playerView = null;
            }
        }
        players.Clear();
        players = new List<Player>(playerBackUp);
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].playerView == null)
            {
                players[i].playerView = createPlayerView();
            }
            players[i].updatePlayerView();
            if (players[i].id ==
             thisPlayerBackup.id)
            {
                thisPlayer = players[i];
            }
        }
        Debug.Log("có chạy đếncập nhật");

        updatePositionPlayerView();
    }

    public override void handleCTable(string data)
    {
        JObject JData = JObject.Parse(data);
        if ((int)JData["tableType"] == 1)
        {
            Globals.Config.typeTable = 1;
            listPosView = new List<Vector2>
{
    new Vector2(-498f, -220f),
    new Vector2(140f, 263f)
};
        }
        else
        {
            Globals.Config.typeTable = 0;
        }
        var listPlayer = (JObject)((JArray)JData["ArrP"])[0];
        thisPlayerBackup = new Player();
        playerBackUp.Add(thisPlayerBackup);
        readDataPlayer(thisPlayerBackup, listPlayer);
        thisPlayerBackup.setHost(true);
        Debug.Log("xem có thằng thisplayer ko" + playerBackUp.Contains(thisPlayerBackup));
        base.handleCTable(data);
        isFirstTurnAndCtable = true;
        Debug.Log("xem có thằng thisplayer ko" + playerBackUp.Contains(thisPlayerBackup));
    }
    public void countDownTimeToStart(int time)
    {
        if (time <= 0 || players.Count == 1)
        {
            m_TimeToStartText.gameObject.SetActive(false);
            m_BgStart.SetActive(false);
            return;
        }
        else
        {
            timeToStart = time;
            m_TimeToStartText.gameObject.SetActive(true);
            m_BgStart.SetActive(true);
            TweenCallback callback = () =>
                        {
                            if (timeToStart > 0)
                            {
                                m_TimeToStartText.text = timeToStart.ToString();
                                timeToStart--;
                            }
                            else
                            {
                                m_TimeToStartText.gameObject.SetActive(false);
                                m_BgStart.SetActive(false);
                            }
                        };
            DOTween.Sequence()
                         .AppendCallback(callback)
                         .AppendInterval(1f)
                         .SetLoops(timeToStart + 1);
        }
    }
    private void connectGame(JObject data)
    {
        JArray ArrP = getJArray(data, "ArrP");

        // Clear existing cards first
        foreach (List<Card> cardList in ListCardPlayer)
        {
            cardList.Clear();
        }

        for (int i = 0; i < ArrP.Count; i++)
        {
            JObject dataPlayer = (JObject)ArrP[i];
            Player player = getPlayerWithID(getInt(dataPlayer, "id"));
            JArray Arr = getJArray(dataPlayer, "Arr");
            int position = players.IndexOf(player);
            List<Card> listCard = ListCardPlayer[position];

            for (int j = 0; j < Arr.Count; j++)
            {
                int cardCode = (int)Arr[j];
                Card card = spawnCard();
                if (card != null)
                {
                    card.setTextureWithCode(cardCode);
                    card.gameObject.SetActive(true);
                    listCard.Add(card);

                    // Add touch events after adding to list
                    if (player == thisPlayer)
                    {
                        Debug.Log("Adding touch events for card: " + cardCode);
                        AddCardTouchEvents(card);
                    }
                }
            }
        }

        turnNameCurrent = (string)data["CN"];
        lastTurnName = (string)data["lp"];
        timeTurn = (int)data["T"];

        Player playerD = getPlayer(lastTurnName);
        int positionD = players.IndexOf(playerD);
        if (playerD != null)
        {
            List<Card> listCardD = ListCardPlayerD[positionD];
            JArray Arr = getJArray(data, "CardsInTurn");
            for (int j = 0; j < Arr.Count; j++)
            {
                int cardCode = (int)Arr[j];
                Card card = spawnCard();
                card.setTextureWithCode(cardCode);
                card.gameObject.SetActive(true);
                listCardD.Add(card);
            }
            playerD.setTurn(true, timeTurn);
        }

        initPlayerCard();

        // Force rearrange cards after setup
        if (thisPlayer != null)
        {
            RearrangeCards();
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
        if (playerIndex < 0 || playerIndex >= ListCardPlayer.Count || !ListCardPlayer[playerIndex].Contains(card))
        {
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

            OnCardTouch(card);

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
    public async void danhBai(string turnName, string nextTurn, JArray vtCard, bool newTurn)
    {
        while (isDanhbai || isBoluot)
        {
            await UniTask.Delay(50); // Đợi 50ms rồi kiểm tra lại
        }
        isDanhbai = true;
        // 1. Initial setup
        //  playSound(Globals.SOUND_GAME.CARD_DISCARD);
        playSound(Globals.SOUND_GAME.CARD_FLIP_1);
        Player player = getPlayer(turnName);
        turnNameCurrent = nextTurn;
        lastTurnName = turnName;
        int position = players.IndexOf(player);
        Player playerTurnCurr = getPlayer(lastTurnName);
        if (playerTurnCurr == thisPlayer)
        {
            handleButton(false, false, false);
        }
        // 2. Reset animation và clear bài cũ

        if (_currentDiscardAnimationTask != null)
        {
            await _currentDiscardAnimationTask.Value;
        }
        foreach (List<Card> cardList in ListCardPlayerD)
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
        foreach (GameObject img in m_TxtPass)
        {
            img.SetActive(false);
        }
        List<Card> listCard = ListCardPlayer[position];
        List<Card> listCardD = ListCardPlayerD[position];
        await ProcessPlayCards(player, listCard, listCardD, vtCard, position);
        if (position == 0)
        {
            RearrangeCards();
        }
        player.setTurn(false, 0);
        Player nextPlayer = getPlayer(nextTurn);
        if (nextPlayer != null)
        {
            nextPlayer.setTurn(true, timeTurn);
            if (nextPlayer == thisPlayer)
            {
                handleButton(true, true, false);
            }
        }
        setNumberCardLast();
        isDanhbai = false;
        Debug.Log("có chạy vào hàm đánh bài");
    }
    private void setNumberCardLast(bool isFinish = false)
    {
        for (int i = 1; i < players.Count; i++)
        {
            if (ListCardPlayer[i].Count == 0 || isFinish || agTable > 1000)
            {
                if (Globals.Config.typeTable == 1 || players.Count() == 2)
                {
                    m_NumberCardLast[i].text = "";
                }
                else
                {
                    m_NumberCardLast[i - 1].text = "";
                }
            }
            else
            {
                if (Globals.Config.typeTable == 1 || players.Count() == 2)
                {
                    m_NumberCardLast[i].text = ListCardPlayer[i].Count.ToString();
                }
                else
                {
                    m_NumberCardLast[i - 1].text = ListCardPlayer[i].Count.ToString();
                }

            }
        }
    }

    private async UniTask ProcessPlayCards(Player player, List<Card> listCard, List<Card> listCardD, JArray vtCard, int position)
    {
        if (player == thisPlayer)
        {
            for (int i = 0; i < vtCard.Count; i++)
            {
                Card cardToPlay = listCard.Find(c => c.code == (int)vtCard[i]);
                if (cardToPlay != null)
                {
                    listCard.Remove(cardToPlay);
                    listCardD.Add(cardToPlay);
                    cardToPlay.transform.DOScale(0.4f, 0.2f);
                    cardToPlay.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            for (int i = 0; i < vtCard.Count; i++)
            {
                if (listCard.Count > 0)
                {
                    Card cardToPlay = listCard[0];
                    listCard.RemoveAt(0);
                    cardToPlay.setTextureWithCode((int)vtCard[i]);
                    cardToPlay.transform.DOScale(0.4f, 0.2f);
                    cardToPlay.gameObject.SetActive(true);
                    listCardD.Add(cardToPlay);
                }
            }
        }

        _currentDiscardAnimationTask = SetDiscardCardPosition(listCardD, position);
        await _currentDiscardAnimationTask.Value;
        _currentDiscardAnimationTask = null;
    }

    public void danhBaiError(string error)
    {
        UIManager.instance.showToast(error, m_BgToast.transform);
        selectedCards.Clear();
        RearrangeCards();
    }

    public async void initPlayerCard()
    {
        if (stateGame == Globals.STATE_GAME.PLAYING)
        {
            if (turnNameCurrent == thisPlayer.namePl)
            {
                handleButton(true, true, false);
            }

        }

        // 2. Setup bài cho từng người chơi
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];

            // Skip nếu đang xem và là người chơi chính
            if (player == thisPlayer && stateGame == Globals.STATE_GAME.VIEWING)
                continue;

            int position = players.IndexOf(player);
            List<Card> listCard = ListCardPlayer[position];
            List<Card> listCardD = ListCardPlayerD[position];

            // 3. Hiển thị bài trên tay
            foreach (Card card in listCard)
            {
                card.gameObject.SetActive(true);

                if (player == thisPlayer)
                {
                    card.transform.localScale = new Vector3(0.7f, 0.7f, 1);
                    // Tính toán vị trí căn giữa
                    float posX = -((listCard.Count * 30f) / 2) + (listCard.IndexOf(card) * 30f);
                    card.transform.localPosition = new Vector3(posX, -250f, 0);
                }
                else
                {
                    // Setup bài người chơi khác
                    card.transform.localScale = new Vector3(0.45f, 0.45f, 1);
                    SetCardPositionByPlayerIndex(card, position);
                }
            }
            await SetDiscardCardPosition(listCardD, position);
        }
    }

    // Helper method cho vị trí bài
    private void SetCardPositionByPlayerIndex(Card card, int playerIndex)
    {
        card.transform.localPosition = listPostCardChia[playerIndex];
        if ((Globals.Config.typeTable == 1 || players.Count() == 2) && playerIndex == 1)
        {
            card.transform.localPosition = listPostCardChia[2];
        }
        // if (Globals.Config.typeTable == 1)
        // {
        //     switch (playerIndex)
        //     {
        //         case 1:
        //             card.transform.localPosition = new Vector3(40f, 250f, 0);
        //             break;
        //     }
        //     return;
        // }

        // switch (playerIndex)
        // {
        //     case 1:
        //         card.transform.localPosition = new Vector3(498f, 80f, 0);
        //         break;
        //     case 2:
        //         card.transform.localPosition = new Vector3(40f, 250f, 0);
        //         break;
        //     case 3:
        //         card.transform.localPosition = new Vector3(-498f, 80f, 0);
        //         break;
        // }
    }

    private async UniTask SetDiscardCardPosition(List<Card> cards, int playerIndex)
    {
        cards.Sort((a, b) => a.N.CompareTo(b.N));
        float spacing = 30f;
        float centerSpacing = 50f;
        isSpecialHand =
           check3DoiThong(cards) ||
           check4DoiThong(cards) ||
           checkTuQuy(cards) ||
           checkSetOfTwos(cards);

        Vector3 basePos = listPostCardDanh[playerIndex];
        if ((Globals.Config.typeTable == 1 || players.Count() == 2) && playerIndex == 1)
        {
            basePos = listPostCardDanh[2];
        }

        // Vector3 basePos = playerIndex switch
        // {
        //     0 => new Vector3(0f, -60f, 0),
        //     1 => Globals.Config.typeTable == 1 ? new Vector3(20f, 120f, 0) : new Vector3(358f, 0f, 0),
        //     2 => new Vector3(20f, 120f, 0),
        //     3 => new Vector3(-358f, 0f, 0),
        //     _ => Vector3.zero
        // };
        if (playerIndex == 1 && Globals.Config.typeTable != 1 && players.Count() != 2)
        {
            basePos = new Vector3(basePos.x - (cards.Count - 1) * spacing, basePos.y, basePos.z);
        }

        float totalWidthCenter = (cards.Count - 1) * centerSpacing;
        Vector3 centerOrigin = Vector3.zero - new Vector3(totalWidthCenter / 2f, 0f, 0f);

        Vector3 discardStartPos = basePos;
        if (playerIndex == 0 || playerIndex == 2)
        {
            float totalWidthDiscard = (cards.Count - 1) * spacing;
            discardStartPos = basePos - new Vector3(totalWidthDiscard / 2f, 0f, 0f);
        }

        // ✅ Gom toàn bộ anim vào đây để chờ
        List<UniTask> waitTasks = new List<UniTask>();

        // ✅ Bật hiệu ứng đặc biệt ngay
        if (isSpecialHand)
        {
            m_AniCardSpecial.gameObject.SetActive(true);

            if (check3DoiThong(cards))
                m_AniCardSpecial.AnimationState.SetAnimation(0, "3 consecutive pairs", false);
            else if (check4DoiThong(cards))
                m_AniCardSpecial.AnimationState.SetAnimation(0, "4 consecutive pairs", false);
            else if (checkTuQuy(cards))
                m_AniCardSpecial.AnimationState.SetAnimation(0, "4 of a kind", false);
            else if (checkSetOfTwos(cards))
                m_AniCardSpecial.AnimationState.SetAnimation(0, "set of twos", false);
        }
        else
        {
            m_AniCardSpecial.gameObject.SetActive(false);
        }

        for (int i = 0; i < cards.Count; i++)
        {
            Card card = cards[i];
            int code = card.code;
            Vector3 finalPos;
            EventTrigger trigger = card.GetComponent<EventTrigger>();
            if (trigger != null)
            {
                trigger.triggers.Clear();
                UnityEngine.Object.Destroy(trigger);
            }

            Image img = card.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;

            CanvasGroup cg = card.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }

            card.transform.SetAsLastSibling();
            finalPos = discardStartPos + new Vector3(i * spacing, 0f, 0f);


            Vector3 centerPos = centerOrigin + new Vector3(i * centerSpacing, 0f, 0f);
            card.transform.localRotation = Quaternion.identity;
            card.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
            float delay = i * 0.03f;

            if (isSpecialHand)
            {
                card.setTextureWithCode(0);
                card.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                card.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                Vector3 startPos = card.transform.localPosition;

                Vector3 controlPoint = startPos + (centerPos - startPos) * 0.5f + Vector3.up * 80f;
                Vector3[] bezierPoints = new Vector3[] { startPos, controlPoint, centerPos };
                float moveDuration = 0.6f;
                float twinkleDuration = 1.6f;
                var tcs = new UniTaskCompletionSource();

                DOVirtual.DelayedCall(delay, () =>
                {
                    bool flipped = false;
                    Sequence seq = DOTween.Sequence();
                    seq.AppendCallback(() => card.setEffect_Twinkle(true, twinkleDuration));
                    seq.Append(DOTween.To(() => 0f, t =>
                    {
                        Vector3 m1 = Vector3.Lerp(bezierPoints[0], bezierPoints[1], t);
                        Vector3 m2 = Vector3.Lerp(bezierPoints[1], bezierPoints[2], t);
                        card.transform.localPosition = Vector3.Lerp(m1, m2, t);
                        float scale = Mathf.Lerp(0.4f, 1.1f, t);
                        float scaleX;
                        if (t < 0.5f)
                            scaleX = Mathf.Lerp(1f, 0f, t * 2);     // Lật đi
                        else
                            scaleX = Mathf.Lerp(0f, 1f, (t - 0.5f) * 2); // Lật về
                        card.transform.localScale = new Vector3(scaleX * scale, scale, 1f);
                        if (!flipped && t >= 0.5f)
                        {
                            flipped = true;
                            card.setTextureWithCode(code);
                        }

                    }, 1f, moveDuration).SetEase(Ease.InOutCubic));
                    seq.Append(card.transform.DOLocalRotate(new Vector3(0, 0, -10), 0.2f).SetEase(Ease.InOutBack));
                    seq.Append(card.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack));
                    seq.Append(card.transform.DOLocalMove(finalPos, 0.3f).SetEase(Ease.OutBack));
                    seq.Join(card.transform.DOScale(new Vector3(0.4f, 0.4f, 1f), 0.3f));
                    seq.OnComplete(() =>
                    {
                        tcs.TrySetResult();
                    });
                });
                waitTasks.Add(tcs.Task);
            }
            else
            {
                Sequence moveSeq = DOTween.Sequence();
                moveSeq.AppendInterval(delay);
                moveSeq.Append(card.transform.DOLocalMove(finalPos, 0.25f).SetEase(Ease.OutQuad));
            }
        }
        if (waitTasks.Count > 0)
        {
            try
            {
                await UniTask.WhenAll(waitTasks).Timeout(System.TimeSpan.FromSeconds(3)); // Timeout 3 giây
            }
            catch (System.TimeoutException)
            {
                Debug.LogWarning("SetDiscardCardPosition: Animation timeout!");
            }
        }

        // ✅ Sau khi tất cả hiệu ứng xong, mới tắt anim đặc biệt
        if (isSpecialHand)
        {
            m_AniCardSpecial.gameObject.SetActive(false);
        }
    }




    public override void handleVTable(string data)
    {
        JObject JData = JObject.Parse(data);
        if ((int)JData["tableType"] == 1)
        {
            Globals.Config.typeTable = 1;
            listPosView = new List<Vector2>
{
    new Vector2(-498f, -220f),
    new Vector2(140f, 263f)
};
        }
        else
        {
            Globals.Config.typeTable = 0;
        }
        bool isHasThis = false;
        JArray listPlayer = (JArray)JData["ArrP"];
        playerBackUp.Clear();
        for (var i = 0; i < listPlayer.Count; i++)
        {
            var player = new Player();
            playerBackUp.Add(player);
            readDataPlayer(player, (JObject)listPlayer[i]);
            if (player.id == Globals.User.userMain.Userid)
            {
                isHasThis = true;
                thisPlayerBackup = player;
            }
        }
        if (!isHasThis)
        {
            var player = new Player();
            player.id = Globals.User.userMain.Userid;
            if (Globals.User.userMain.Tinyurl.IndexOf("fb.") != -1)
            {
                player.fid = Globals.User.userMain.Tinyurl.Substring(3);
            }
            player.namePl = Globals.User.userMain.Username;
            player.displayName = Globals.User.userMain.displayName;
            player.ag = Globals.User.userMain.AG;
            player.vip = Globals.User.userMain.VIP;
            player.avatar_id = Globals.User.userMain.Avatar;
            player.is_ready = true;
            thisPlayerBackup = player;
        }
        base.handleVTable(data);
        updatePositionPlayerView();
        connectGame(JData);

    }
    public void startGame(JObject data)
    {
        stateGame = Globals.STATE_GAME.PLAYING;
        typeSort = 2;
        JArray arr = getJArray(data, "arr");
        timeTurn = getInt(data, "T");
        turnNameCurrent = getString(data, "nameturn");
        bool firstRound = getBool(data, "firstRound");
        TweenCallback start = () =>
        {
            playSound(Globals.SOUND_HILO.START_GAME);
            m_AniStart.gameObject.SetActive(true);
            m_AniStart.AnimationState.SetAnimation(0, "start", false);
            m_BgStart.SetActive(false);
        };
        Sequence mainSequence = DOTween.Sequence().AppendCallback(start);
        mainSequence.AppendInterval(0.8f);
        mainSequence.AppendCallback(async () =>
                        {
                            m_AniStart.gameObject.SetActive(false);
                            Vector3 deckPosition = new Vector3(0f, 20f, 0f); // Vị trí bộ bài giữa bàn
                            float stackOffset = 0.16f; // Khoảng cách giữa các lá trong stack
                            for (int i = 0; i < players.Count; i++)
                            {
                                Player player = players[i];
                                int position = players.IndexOf(player);
                                ListCardPlayer[position].Clear();

                                for (int j = 0; j < arr.Count; j++)
                                {
                                    Card card = spawnCard();
                                    card.transform.localScale = new Vector3(0.4f, 0.4f, 1);
                                    card.transform.localPosition = new Vector3(
                                        deckPosition.x,
                                        deckPosition.y - (stackOffset * (i * arr.Count + j)),
                                        0
                                    );

                                    card.transform.Rotate(0, 0, 90);
                                    ListCardPlayer[position].Add(card);
                                    AddCardTouchEvents(card);

                                    if (player == thisPlayer)
                                        card.setTextureWithCode((int)arr[j]);
                                    else
                                        card.setTextureWithCode(0);

                                    card.gameObject.SetActive(true);
                                }
                            }
                            chiaBai();


                        });
        mainSequence.Play();
    }






    private void chiaBai()
    {
        isChiabai = true;
        float dealTime = 0.25f;
        float dealDelay = 0.05f;

        Sequence dealSequence = DOTween.Sequence();

        for (int cardIndex = 0; cardIndex < 13; cardIndex++)
        {
            for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
            {

                int cardOrder = cardIndex * players.Count + playerIndex;
                float delay = cardOrder * dealDelay;

                Player player = players[playerIndex];
                List<Card> playerCards = ListCardPlayer[playerIndex];

                if (cardIndex >= playerCards.Count)
                    continue;

                Card card = playerCards[cardIndex];

                // Tính vị trí và scale cuối cùng
                Vector3 finalPos;
                Vector3 finalScale;

                if (player == thisPlayer)
                {
                    float spacing = 60f;
                    float totalWidth = (playerCards.Count - 1) * spacing;
                    float startX = -totalWidth / 2f;
                    finalPos = new Vector3(startX + (cardIndex * spacing), -250f, 0f);
                    finalScale = Vector3.one * 0.8f;
                }
                else
                {
                    finalPos = listPostCardChia[playerIndex];
                    if ((Globals.Config.typeTable == 1 || players.Count() == 2) && playerIndex == 1)
                    {
                        finalPos = listPostCardChia[2];
                    }
                    // switch (playerIndex)
                    // {
                    //     case 1: finalPos = Globals.Config.typeTable == 1 ? new Vector3(40f, 250f, 0) : new Vector3(498f, 80f, 0); break;   // Phải
                    //     case 2: finalPos = new Vector3(40f, 250f, 0); break;   // Trên
                    //     case 3: finalPos = new Vector3(-498f, 80f, 0); break;  // Trái
                    //     default: finalPos = Vector3.zero; break;
                    // }
                    finalScale = Vector3.one * 0.45f;
                }
                // Capture để tránh lỗi closure
                Card capturedCard = card;
                Vector3 capturedPos = finalPos;
                Vector3 capturedScale = finalScale;

                dealSequence.InsertCallback(delay, () =>
                {
                    playSound(Globals.SOUND_GAME.CARD_FLIP_1);
                    capturedCard.gameObject.SetActive(true);

                    // Góc nghiêng ban đầu
                    capturedCard.transform.localRotation = Quaternion.Euler(0, 0, 30f);

                    // Tween đến vị trí & xoay về đúng góc
                    capturedCard.transform.DOLocalMove(capturedPos, dealTime).SetEase(Ease.OutQuint);
                    capturedCard.transform.DOScale(capturedScale, dealTime).SetEase(Ease.OutQuint);
                    capturedCard.transform.DOLocalRotate(Vector3.zero, dealTime).SetEase(Ease.OutQuint);
                });
            }
        }
        dealSequence.AppendInterval(0.5f);
        dealSequence.OnComplete(() =>
        {
            SortCard();
            setNumberCardLast();
            Player playerFirst = getPlayer(turnNameCurrent);
            if (playerFirst != null)
            {
                playerFirst.setTurn(true, timeTurn);
            }
            if (turnNameCurrent == thisPlayer.namePl)
            {
                handleButton(false, true, true);
                if (isFirstTurnAndCtable)
                {
                    isFirstTurnAndCtable = false;
                    UIManager.instance.showToast("អ្នកលេងមាន3 ♠ ប៉ះប្រយុទ្ធជាមួយអ្នកលេងមាន(ប្រាំបីឆ្លង3 ♠ )", m_BgToast.transform);
                }
            }

            StartCoroutine(DelayEndChiaBai());
        });
        dealSequence.Play();
    }

    private IEnumerator DelayEndChiaBai()
    {
        yield return new WaitForSeconds(0.8f);
        isChiabai = false;
    }




    public async void boLuot(string turnName, string nextTurn, bool newTurn)
    {
        while (isDanhbai || isBoluot)
        {
            await UniTask.Delay(50); // Đợi 50ms rồi kiểm tra lại
        }
        isBoluot = true;
        playSound(Globals.SOUND_GAME.FOLD);
        Player player = getPlayer(turnName);
        int indexPos = players.IndexOf(player);
        turnNameCurrent = nextTurn;
        player.setTurn(false, 0);
        if (indexPos >= 0 && indexPos < m_TxtPass.Count)
        {
            GameObject obj = (Globals.Config.typeTable == 1 || players.Count() == 2) && indexPos != 0 ? m_TxtPass[indexPos + 1] : m_TxtPass[indexPos];
            DOVirtual.DelayedCall(0f, () =>
            {
                obj.SetActive(true);
                obj.transform.localScale = Vector3.zero;
                obj.transform.DOScale(0.7f, 0.2f).SetEase(Ease.OutBack);
                CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = 0;
                    canvasGroup.DOFade(1f, 0.2f);
                }
                DOVirtual.DelayedCall(1f, () =>
                {
                    if (m_TxtPass[indexPos] != null)
                    {
                        obj.SetActive(false);
                    }
                });
            });
        }

        Debug.Log("xem thằng này" + player.namePl);
        if (player == thisPlayer)
        {
            selectedCards.Clear();
            handleButton(false, false, false);
        }
        if (newTurn)
        {
            Debug.Log("có chạy vào đây ko");
            lastTurnName = "";
            Debug.Log("hnhu");
            foreach (List<Card> cardList in ListCardPlayerD)
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
            DOVirtual.DelayedCall(0.4f, () =>
          {
              if (turnNameCurrent == thisPlayer.namePl)
              {
                  handleButton(false, true, true);

              }
          });
        }
        else
        {
            DOVirtual.DelayedCall(0.4f, () =>
      {
          if (turnNameCurrent == thisPlayer.namePl)
          {
              handleButton(true, true, false);
          }
      });
        }
        Player nextPlayer = getPlayer(nextTurn);
        if (nextPlayer != null)
        {
            nextPlayer.setTurn(true, timeTurn);
        }
        isBoluot = false;
    }
    public void handleButton(bool isCancel, bool isDiscard, bool isCenter)
    {


        m_ButtonCancel.SetActive(isCancel);
        m_ButtonDiscard.SetActive(isDiscard);
        m_ButtonCancel.GetComponent<Button>().interactable = isCancel;
        m_ButtonDiscard.GetComponent<Button>().interactable = isDiscard;
        if (!isCenter)
        {
            m_ButtonDiscard.transform.localPosition = new Vector3(100, m_ButtonDiscard.transform.localPosition.y, 0);
        }
        else
        {
            m_ButtonCancel.SetActive(false);
            m_ButtonDiscard.transform.localPosition = new Vector3(0, m_ButtonDiscard.transform.localPosition.y, 0);
        }

    }
    public void OnCardTouch(Card card)
    {
        touchStartPos = Input.mousePosition;
        cardStartPos = card.transform.localPosition;
        selectedCard = card;
        isDragging = true;
    }
    private void suggestCardDanh(List<Card> selectedCards)
    {
        if (selectedCards == null || selectedCards.Count != 2) return;
        Debug.Log(" xem là gợi ý sao");
        List<Card> playerCards = ListCardPlayer[players.IndexOf(thisPlayer)];
        Card card1 = selectedCards[0];
        Card card2 = selectedCards[1];

        listCardSuggest.Clear();
        ClearSuggestion();

        // 1. Tứ quý (4 lá cùng số, chứa cả 2 lá đã chọn)
        Debug.Log(" xem là gợi ý ");
        var tuQuy = playerCards
            .GroupBy(c => c.N)
            .Where(g => g.Count() == 4 && selectedCards.All(sel => g.Contains(sel)))
            .OrderByDescending(g => g.Key)
            .FirstOrDefault();
        if (tuQuy != null)
        {
            listCardSuggest.AddRange(tuQuy);
            HighlightSuggestedCards(card2);
            Debug.Log("tứ quý");
            return;
        }

        // 2. 4 đôi thông (4 đôi liên tiếp, mỗi đôi 2 lá, chứa cả 2 lá đã chọn) - lấy bộ lớn nhất
        var pairs = playerCards
            .GroupBy(c => c.N)
            .Where(g => g.Count() >= 2 && g.Key != 2)
            .OrderBy(g => g.Key)
            .ToList();
        List<List<Card>> candidates4 = new List<List<Card>>();
        for (int i = 0; i <= pairs.Count - 4; i++)
        {
            int n1 = pairs[i].Key;
            int n2 = pairs[i + 1].Key;
            int n3 = pairs[i + 2].Key;
            int n4 = pairs[i + 3].Key;
            if (n2 == n1 + 1 && n3 == n2 + 1 && n4 == n3 + 1)
            {
                var result = pairs[i].Take(2)
                    .Concat(pairs[i + 1].Take(2))
                    .Concat(pairs[i + 2].Take(2))
                    .Concat(pairs[i + 3].Take(2))
                    .ToList();
                if (result.Count == 8 && selectedCards.All(sel => result.Contains(sel)) && result.All(c => c.N != 2))
                    candidates4.Add(result);
            }
        }
        if (candidates4.Count > 0)
        {
            var best = candidates4.OrderByDescending(c => c[7].N).First();
            listCardSuggest.AddRange(best);
            HighlightSuggestedCards(card2);
            Debug.Log("4 đôi thông");
            return;
        }

        // 3. 3 đôi thông (3 đôi liên tiếp, mỗi đôi 2 lá, chứa cả 2 lá đã chọn) - lấy bộ lớn nhất
        List<List<Card>> candidates3 = new List<List<Card>>();
        for (int i = 0; i <= pairs.Count - 3; i++)
        {
            int n1 = pairs[i].Key;
            int n2 = pairs[i + 1].Key;
            int n3 = pairs[i + 2].Key;
            if (n2 == n1 + 1 && n3 == n2 + 1)
            {
                var result = pairs[i].Take(2)
                    .Concat(pairs[i + 1].Take(2))
                    .Concat(pairs[i + 2].Take(2))
                    .ToList();
                if (result.Count == 6 && selectedCards.All(sel => result.Contains(sel)) && result.All(c => c.N != 2))
                    candidates3.Add(result);
            }
        }
        if (candidates3.Count > 0)
        {
            var best = candidates3.OrderByDescending(c => c[5].N).First();
            listCardSuggest.AddRange(best);
            HighlightSuggestedCards(card2);
            Debug.Log("3 dôi thông");
            return;
        }

        // 4. 2 đôi thông (2 đôi liên tiếp, mỗi đôi 2 lá, chứa cả 2 lá đã chọn) - lấy bộ lớn nhất
        List<List<Card>> candidates2 = new List<List<Card>>();
        for (int i = 0; i <= pairs.Count - 2; i++)
        {
            int n1 = pairs[i].Key;
            int n2 = pairs[i + 1].Key;
            if (n2 == n1 + 1)
            {
                var result = pairs[i].Take(2)
                    .Concat(pairs[i + 1].Take(2))
                    .ToList();
                if (result.Count == 4 && selectedCards.All(sel => result.Contains(sel)) && result.All(c => c.N != 2))
                    candidates2.Add(result);
            }
        }
        if (candidates2.Count > 0)
        {
            var best = candidates2.OrderByDescending(c => c[3].N).First();
            listCardSuggest.AddRange(best);
            HighlightSuggestedCards(card2);
            Debug.Log("2 dôi thông");
            return;
        }

        // 5. Sám cô (3 lá cùng số, chứa cả 2 lá đã chọn)
        var samCo = playerCards
            .GroupBy(c => c.N)
            .Where(g => g.Count() == 3 && selectedCards.All(sel => g.Contains(sel)))
            .OrderByDescending(g => g.Key)
            .FirstOrDefault();
        if (samCo != null)
        {
            listCardSuggest.AddRange(samCo);
            HighlightSuggestedCards(card2);
            Debug.Log("sám cô");
            return;
        }

        // 6. Sảnh cùng chất (>=3 lá liên tiếp cùng chất, chứa cả 2 lá đã chọn) - lấy sảnh dài nhất, lớn nhất
        var sameSuit = playerCards.Where(c => c.S == card1.S).ToList();
        var allStraightsSameSuit = GetAllLongestStraightCombosWithDuplicates(sameSuit)
     .Where(seq => selectedCards.All(sel => seq.Contains(sel)) && seq.All(c => c.S == card1.S))
     .ToList();
        if (allStraightsSameSuit.Count > 0)
        {
            int maxLen = allStraightsSameSuit.Max(seq => seq.Count);
            var best = allStraightsSameSuit
                .Where(seq => seq.Count == maxLen)
                .OrderByDescending(seq => seq.Max(c => c.N))
                .First();
            listCardSuggest.AddRange(best);
            HighlightSuggestedCards(card2);
            Debug.Log("sảnh cùng chất");
            return;
        }

        // 7. Sảnh khác chất (>=3 lá liên tiếp, chứa cả 2 lá đã chọn) - lấy sảnh dài nhất, lớn nhất
        var allStraights = GetAllLongestStraightCombosWithDuplicates(playerCards)
      .Where(seq => selectedCards.All(sel => seq.Contains(sel)))
      .ToList();
        if (allStraights.Count > 0)
        {
            int maxLen = allStraights.Max(seq => seq.Count);
            var best = allStraights
                .Where(seq => seq.Count == maxLen)
                .OrderByDescending(seq => seq.Max(c => c.N))
                .First();
            listCardSuggest.AddRange(best);
            HighlightSuggestedCards(card2);
            Debug.Log("sảnh khác chất");
            return;
        }
    }
    private void SuggestCardsChan(Card selectedCard)
    {
        Player lastPlayer = getPlayer(lastTurnName);
        if (lastPlayer == null) return;

        List<Card> lastCards = ListCardPlayerD[players.IndexOf(lastPlayer)];
        if (lastCards.Count == 0) return;
        bool isDouble = checkDoiTL(lastCards);
        bool isTriple = checkXamTL(lastCards);
        bool isStraight = checkSanhTL(lastCards);
        bool isTwoPair = check2DoiThong(lastCards);
        bool isThreepair = check3DoiThong(lastCards);
        bool isFlushStraight = checkThungPhaSanhTL(lastCards);
        bool isFourDoiThong = check4DoiThong(lastCards);
        bool isTwo = lastCards[0].N == 2 && lastCards.Count == 1;
        bool isTuQuy = checkTuQuy(lastCards); // không phân biệt chất ở đây

        List<Card> playerCards = ListCardPlayer[players.IndexOf(thisPlayer)];
        listCardSuggest.Clear();
        ClearSuggestion();

        int GetCardStrength(int n) => n == 2 ? 15 : n; // ưu tiên 2 > A > K...

        if (isDouble || isTriple)
        {
            int neededCount = isDouble ? 2 : 3;
            int targetStrength = GetCardStrength(lastCards[0].N);

            var validGroups = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() >= neededCount && GetCardStrength(g.Key) > targetStrength)
                .Where(g => g.Any(c => c == selectedCard)) // nhóm có chứa quân được chọn
                .OrderBy(g => GetCardStrength(g.Key))
                .ToList();

            if (validGroups.Count > 0)
            {
                // Ưu tiên selectedCard nằm trong kết quả gợi ý
                var group = validGroups[0].ToList();
                if (!group.Contains(selectedCard))
                {
                    // fallback, không xảy ra với filter trên
                }
                // Lấy selectedCard + (neededCount-1) lá còn lại trong group
                listCardSuggest.Add(selectedCard);
                foreach (var c in group)
                {
                    if (listCardSuggest.Count >= neededCount) break;
                    if (c != selectedCard) listCardSuggest.Add(c);
                }
            }
        }
        else if (isFourDoiThong)
        {
            // Lấy giá trị lớn nhất của 4 đôi thông đối thủ
            var sortedLast = lastCards.OrderBy(c => c.N).ToList();
            int lastHigh = sortedLast[7].N;

            // Tìm tất cả các đôi trong bài mình (trừ 2)
            var pairs = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() >= 2 && g.Key != 2)
                .OrderBy(g => g.Key)
                .ToList();

            List<List<Card>> candidates = new List<List<Card>>();
            for (int i = 0; i <= pairs.Count - 4; i++)
            {
                int n1 = pairs[i].Key;
                int n2 = pairs[i + 1].Key;
                int n3 = pairs[i + 2].Key;
                int n4 = pairs[i + 3].Key;
                if (n2 == n1 + 1 && n3 == n2 + 1 && n4 == n3 + 1 && n4 > lastHigh)
                {
                    var pairs1 = GetAllPairsFromList(pairs[i].ToList());
                    var pairs2 = GetAllPairsFromList(pairs[i + 1].ToList());
                    var pairs3 = GetAllPairsFromList(pairs[i + 2].ToList());
                    var pairs4 = GetAllPairsFromList(pairs[i + 3].ToList());

                    foreach (var p1 in pairs1)
                        foreach (var p2 in pairs2)
                            foreach (var p3 in pairs3)
                                foreach (var p4 in pairs4)
                                {
                                    var bonDoiThong = new List<Card>();
                                    bonDoiThong.AddRange(p1);
                                    bonDoiThong.AddRange(p2);
                                    bonDoiThong.AddRange(p3);
                                    bonDoiThong.AddRange(p4);

                                    if (bonDoiThong.Distinct().Count() != 8) continue;
                                    if (bonDoiThong.Contains(selectedCard) && bonDoiThong.All(c => c.N != 2))
                                    {
                                        candidates.Add(bonDoiThong);
                                    }
                                }
                }
            }
            if (candidates.Count > 0)
            {
                // Lấy bộ có đôi lớn nhất
                var best = candidates.OrderByDescending(c => c[7].N).First();
                listCardSuggest.Clear();
                listCardSuggest.AddRange(best);
            }
        }
        else if (isDouble && lastCards[0].N == 2)
        {
            // Ưu tiên tứ quý có selectedCard
            var tuQuyGroups = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() == 4 && g.Any(c => c == selectedCard))
                .ToList();

            if (tuQuyGroups.Count > 0)
            {
                listCardSuggest.Clear();
                listCardSuggest.AddRange(tuQuyGroups[0]);
            }
            else
            {
                // Nếu không có tứ quý, tìm 4 đôi thông có selectedCard
                var pairs = playerCards
                    .GroupBy(c => c.N)
                    .Where(g => g.Count() >= 2 && g.Key != 2)
                    .OrderBy(g => g.Key)
                    .ToList();

                List<List<Card>> candidates = new List<List<Card>>();
                for (int i = 0; i <= pairs.Count - 4; i++)
                {
                    int n1 = pairs[i].Key;
                    int n2 = pairs[i + 1].Key;
                    int n3 = pairs[i + 2].Key;
                    int n4 = pairs[i + 3].Key;
                    if (n2 == n1 + 1 && n3 == n2 + 1 && n4 == n3 + 1)
                    {
                        var pairs1 = GetAllPairsFromList(pairs[i].ToList());
                        var pairs2 = GetAllPairsFromList(pairs[i + 1].ToList());
                        var pairs3 = GetAllPairsFromList(pairs[i + 2].ToList());
                        var pairs4 = GetAllPairsFromList(pairs[i + 3].ToList());

                        foreach (var p1 in pairs1)
                            foreach (var p2 in pairs2)
                                foreach (var p3 in pairs3)
                                    foreach (var p4 in pairs4)
                                    {
                                        var bonDoiThong = new List<Card>();
                                        bonDoiThong.AddRange(p1);
                                        bonDoiThong.AddRange(p2);
                                        bonDoiThong.AddRange(p3);
                                        bonDoiThong.AddRange(p4);

                                        if (bonDoiThong.Distinct().Count() != 8) continue;
                                        if (bonDoiThong.Contains(selectedCard) && bonDoiThong.All(c => c.N != 2))
                                        {
                                            candidates.Add(bonDoiThong);
                                        }
                                    }
                    }
                }
                if (candidates.Count > 0)
                {
                    // Lấy bộ có đôi lớn nhất
                    var best = candidates.OrderByDescending(c => c[7].N).First();
                    listCardSuggest.Clear();
                    listCardSuggest.AddRange(best);
                }
            }
        }
        else if (isTwo)
        {
            // Ưu tiên tứ quý bất kỳ có selectedCard
            var tuQuyGroups = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() == 4 && g.Any(c => c == selectedCard))
                .ToList();

            if (tuQuyGroups.Count > 0)
            {
                listCardSuggest.Clear();
                listCardSuggest.AddRange(tuQuyGroups[0]);
            }
            else
            {
                // Nếu không có tứ quý, tìm 4 đôi thông bất kỳ có selectedCard
                var pairs = playerCards
                    .GroupBy(c => c.N)
                    .Where(g => g.Count() >= 2 && g.Key != 2)
                    .OrderBy(g => g.Key)
                    .ToList();

                List<List<Card>> candidates4 = new List<List<Card>>();
                for (int i = 0; i <= pairs.Count - 4; i++)
                {
                    int n1 = pairs[i].Key;
                    int n2 = pairs[i + 1].Key;
                    int n3 = pairs[i + 2].Key;
                    int n4 = pairs[i + 3].Key;
                    if (n2 == n1 + 1 && n3 == n2 + 1 && n4 == n3 + 1)
                    {
                        var pairs1 = GetAllPairsFromList(pairs[i].ToList());
                        var pairs2 = GetAllPairsFromList(pairs[i + 1].ToList());
                        var pairs3 = GetAllPairsFromList(pairs[i + 2].ToList());
                        var pairs4 = GetAllPairsFromList(pairs[i + 3].ToList());

                        foreach (var p1 in pairs1)
                            foreach (var p2 in pairs2)
                                foreach (var p3 in pairs3)
                                    foreach (var p4 in pairs4)
                                    {
                                        var bonDoiThong = new List<Card>();
                                        bonDoiThong.AddRange(p1);
                                        bonDoiThong.AddRange(p2);
                                        bonDoiThong.AddRange(p3);
                                        bonDoiThong.AddRange(p4);

                                        if (bonDoiThong.Distinct().Count() != 8) continue;
                                        if (bonDoiThong.Contains(selectedCard) && bonDoiThong.All(c => c.N != 2))
                                        {
                                            candidates4.Add(bonDoiThong);
                                        }
                                    }
                    }
                }
                if (candidates4.Count > 0)
                {
                    // Lấy bộ có đôi lớn nhất
                    var best = candidates4.OrderByDescending(c => c[7].N).First();
                    listCardSuggest.Clear();
                    listCardSuggest.AddRange(best);
                }
                else
                {
                    // Nếu không có 4 đôi thông, tìm 3 đôi thông bất kỳ có selectedCard
                    List<List<Card>> candidates3 = new List<List<Card>>();
                    for (int i = 0; i <= pairs.Count - 3; i++)
                    {
                        int n1 = pairs[i].Key;
                        int n2 = pairs[i + 1].Key;
                        int n3 = pairs[i + 2].Key;
                        if (n2 == n1 + 1 && n3 == n2 + 1)
                        {
                            var pairs1 = GetAllPairsFromList(pairs[i].ToList());
                            var pairs2 = GetAllPairsFromList(pairs[i + 1].ToList());
                            var pairs3 = GetAllPairsFromList(pairs[i + 2].ToList());

                            foreach (var p1 in pairs1)
                                foreach (var p2 in pairs2)
                                    foreach (var p3 in pairs3)
                                    {
                                        var baDoiThong = new List<Card>();
                                        baDoiThong.AddRange(p1);
                                        baDoiThong.AddRange(p2);
                                        baDoiThong.AddRange(p3);

                                        if (baDoiThong.Distinct().Count() != 6) continue;
                                        if (baDoiThong.Contains(selectedCard) && baDoiThong.All(c => c.N != 2))
                                        {
                                            candidates3.Add(baDoiThong);
                                        }
                                    }
                        }
                    }
                    if (candidates3.Count > 0)
                    {
                        // Lấy bộ có đôi lớn nhất
                        var best = candidates3.OrderByDescending(c => c[5].N).First();
                        listCardSuggest.Clear();
                        listCardSuggest.AddRange(best);
                    }
                }
            }
        }
        else if (isTuQuy)
        {
            int targetStrength = GetCardStrength(lastCards[0].N);

            // Tìm tất cả tứ quý mạnh hơn, ưu tiên có selectedCard
            var validGroups = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() == 4 && GetCardStrength(g.Key) > targetStrength)
                .Where(g => g.Any(c => c == selectedCard)) // chỉ lấy bộ có selectedCard
                .OrderBy(g => GetCardStrength(g.Key))
                .ToList();

            if (validGroups.Count > 0)
            {
                listCardSuggest.Clear();
                listCardSuggest.AddRange(validGroups[0]);
            }
        }
        else if (isThreepair)
        {
            int targetStrength = GetCardStrength(lastCards[5].N);

            // 1. Ưu tiên tứ quý bất kỳ có selectedCard
            var tuQuyGroups = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() == 4 && g.Any(c => c == selectedCard))
                .ToList();

            if (tuQuyGroups.Count > 0)
            {
                listCardSuggest.Clear();
                listCardSuggest.AddRange(tuQuyGroups[0]);
            }
            else
            {
                // 2. Nếu không có tứ quý, tìm 3 đôi thông mạnh hơn có selectedCard
                var pairs = playerCards
                    .GroupBy(c => c.N)
                    .Where(g => g.Count() >= 2 && g.Key != 2)
                    .OrderBy(g => g.Key)
                    .ToList();

                List<List<Card>> candidates = new List<List<Card>>();
                for (int i = 0; i <= pairs.Count - 3; i++)
                {
                    int n1 = pairs[i].Key;
                    int n2 = pairs[i + 1].Key;
                    int n3 = pairs[i + 2].Key;
                    if (n2 == n1 + 1 && n3 == n2 + 1 && n3 > targetStrength)
                    {
                        var pairs1 = GetAllPairsFromList(pairs[i].ToList());
                        var pairs2 = GetAllPairsFromList(pairs[i + 1].ToList());
                        var pairs3 = GetAllPairsFromList(pairs[i + 2].ToList());

                        foreach (var p1 in pairs1)
                            foreach (var p2 in pairs2)
                                foreach (var p3 in pairs3)
                                {
                                    var baDoiThong = new List<Card>();
                                    baDoiThong.AddRange(p1);
                                    baDoiThong.AddRange(p2);
                                    baDoiThong.AddRange(p3);

                                    if (baDoiThong.Distinct().Count() != 6) continue;
                                    if (baDoiThong.Contains(selectedCard) && baDoiThong.All(c => c.N != 2))
                                    {
                                        candidates.Add(baDoiThong);
                                    }
                                }
                    }
                }
                if (candidates.Count > 0)
                {
                    // Lấy bộ có đôi lớn nhất
                    var best = candidates.OrderByDescending(c => c[5].N).First();
                    listCardSuggest.Clear();
                    listCardSuggest.AddRange(best);
                }
            }
        }
        else if (isStraight || isFlushStraight)
        {
            int length = lastCards.Count;
            int targetNumber = lastCards[^1].N;

            List<Card> usableCards = playerCards.Where(c => c.N != 2).ToList();
            var sequences = GetAllValidStraightCombosWithDuplicates(usableCards, length);

            // Tìm thùng phá sảnh
            var flushStraights = sequences
                .Where(seq => seq.All(c => c.S == seq[0].S)) // tất cả cùng chất
                .Where(seq => seq.Contains(selectedCard))
                 .Where(seq =>
    seq[^1].N > targetNumber ||
    (seq[^1].N == targetNumber && (seq[^1].S > lastCards[^1].S || (targetNumber == 5)))
)
                .OrderByDescending(seq => seq[^1].N) // Sắp xếp giảm dần để lấy bộ cao nhất
                .ThenByDescending(seq => seq[0].N)
                .ToList();

            if (isFlushStraight)
            {
                if (flushStraights.Count > 0)
                {
                    listCardSuggest.AddRange(flushStraights[0]);
                }
            }
            else
            {
                if (flushStraights.Count > 0)
                {
                    listCardSuggest.AddRange(flushStraights[0]);
                }
                else
                {
                    var validSequences = sequences
                        .Where(seq => seq.Contains(selectedCard))
                       .Where(seq =>
    seq[^1].N > targetNumber ||
    (seq[^1].N == targetNumber && (seq[^1].S > lastCards[^1].S || (targetNumber == 5)))
)
                        .OrderByDescending(seq => seq[^1].N) // Sắp xếp giảm dần để lấy bộ cao nhất
                        .ThenByDescending(seq => seq[0].N)
                        .ToList();

                    if (validSequences.Count > 0)
                    {
                        listCardSuggest.AddRange(validSequences[0]);
                    }
                }
            }
        }
        else if (isTwoPair)
        {
            // --- Gợi ý 2 đôi thông ---
            var sortedLast = lastCards.OrderBy(c => c.N).ToList();
            int lastHigh = sortedLast[3].N; // Đôi lớn hơn của đối thủ

            // Tìm tất cả các đôi trong bài mình
            var pairs = playerCards
                .GroupBy(c => c.N)
                .Where(g => g.Count() >= 2 && g.Key != 2) // Không tính đôi 2
                .OrderBy(g => g.Key)
                .ToList();

            // Tìm các bộ ứng viên cho 2 đôi thông
            List<List<Card>> candidates = new List<List<Card>>();
            for (int i = 0; i < pairs.Count - 1; i++)
            {
                int n1 = pairs[i].Key;
                int n2 = pairs[i + 1].Key;
                if (n2 == n1 + 1 && n2 > lastHigh)
                {
                    var candidate = pairs[i].Take(2).Concat(pairs[i + 1].Take(2)).ToList();
                    if (candidate.Contains(selectedCard) && candidate.All(c => c.N != 2))
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            if (candidates.Count > 0)
            {
                // Lấy bộ có đôi lớn nhất (n2 lớn nhất)
                var best = candidates.OrderByDescending(c => c[3].N).First();
                listCardSuggest.Clear();
                listCardSuggest.AddRange(best);
            }
        }

        else // bài đơn
        {
            // int targetStrength = GetCardStrength(lastCards[0].N);
            // int selectedStrength = GetCardStrength(selectedCard.N);
            // if (selectedStrength > targetStrength)
            // {
            //     listCardSuggest.Add(selectedCard);
            // }
        }

        HighlightSuggestedCards(selectedCard);
    }

    List<List<Card>> GetAllPairsFromList(List<Card> cards)
    {
        var result = new List<List<Card>>();
        for (int i = 0; i < cards.Count - 1; i++)
            for (int j = i + 1; j < cards.Count; j++)
                result.Add(new List<Card> { cards[i], cards[j] });
        return result;
    }
    private List<List<Card>> GetAllLongestStraightCombosWithDuplicates(List<Card> cards)
    {
        // Loại bỏ quân 2
        var groups = cards
            .Where(c => c.N != 2)
            .GroupBy(c => c.N)
            .OrderBy(g => g.Key)
            .ToList();

        List<List<Card>> results = new List<List<Card>>();
        int maxLen = 0;

        void Backtrack(int idx, List<Card> current)
        {
            if (current.Count >= 3)
            {
                if (current.Count > maxLen)
                {
                    results.Clear();
                    maxLen = current.Count;
                }
                if (current.Count == maxLen)
                {
                    results.Add(new List<Card>(current));
                }
            }
            if (idx >= groups.Count) return;
            if (current.Count > 0 && groups[idx].Key != current.Last().N + 1) return;
            foreach (var card in groups[idx])
            {
                current.Add(card);
                Backtrack(idx + 1, current);
                current.RemoveAt(current.Count - 1);
            }
        }
        for (int i = 0; i <= groups.Count - 3; i++)
        {
            foreach (var card in groups[i])
            {
                var current = new List<Card> { card };
                Backtrack(i + 1, current);
            }
        }
        return results;
    }
    // Hàm mới: tìm tất cả tổ hợp sảnh hợp lệ, kể cả khi có nhiều lá cùng N
    private List<List<Card>> GetAllValidStraightCombosWithDuplicates(List<Card> cards, int length)
    {
        var groups = cards
            .Where(c => c.N != 2)
            .GroupBy(c => c.N)
            .OrderBy(g => g.Key)
            .ToList();
        List<List<Card>> results = new List<List<Card>>();
        void Backtrack(int idx, List<Card> current)
        {
            if (current.Count == length)
            {
                results.Add(new List<Card>(current));
                return;
            }
            if (idx >= groups.Count) return;
            if (current.Count > 0 && groups[idx].Key != current.Last().N + 1) return;
            foreach (var card in groups[idx])
            {
                current.Add(card);
                Backtrack(idx + 1, current);
                current.RemoveAt(current.Count - 1);
            }
        }
        for (int i = 0; i <= groups.Count - length; i++)
        {
            foreach (var card in groups[i])
            {
                var current = new List<Card> { card };
                Backtrack(i + 1, current);
            }
        }
        return results;
    }
    private void HighlightSuggestedCards(Card selectedCard)
    {

        if (listCardSuggest.Contains(selectedCard))
        {
            // Lấy ra các lá khác trong gợi ý (trừ selectedCard)
            List<Card> otherSuggestedCards = listCardSuggest
                .Where(card => card != selectedCard && !selectedCards.Contains(card))
                .ToList();

            // Add các lá còn lại vào danh sách selectedCards
            foreach (Card card in otherSuggestedCards)
            {
                selectedCards.Add(card);
                card.transform.DOLocalMoveY(-210f, 0.2f)
                    .SetEase(Ease.OutQuint);
            }
        }
    }


    private void ClearSuggestion()
    {
        listCardSuggest.Clear();

    }
    private void Update()
    {
        if (!isDragging || selectedCard == null) return;

        if (Input.GetMouseButton(0))
        {
            Vector3 dragPos = Input.mousePosition;
            float dragDistance = Vector3.Distance(dragPos, touchStartPos);

            if (dragDistance > dragThreshold)
            {
                Vector3 newPos = cardStartPos;
                newPos.x += (dragPos.x - touchStartPos.x);
                newPos.x = Mathf.Clamp(newPos.x, -400f, 400f);
                selectedCard.transform.localPosition = newPos;
                HandleCardSwapping();
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;

            float releaseDistance = Vector3.Distance(Input.mousePosition, touchStartPos);
            if (releaseDistance <= dragThreshold)
            {
                ToggleCardSelection(selectedCard);
            }
            else
            {
                RearrangeCards();
            }
            selectedCard = null;
        }
    }

    // Toggle chọn/bỏ chọn bài
    private void ToggleCardSelection(Card card)
    {
        float normalY = -250f;    // Vị trí y bình thường
        float raisedY = -210f;    // Vị trí y khi nâng lên (nâng 40 đơn vị)
        float animDuration = 0.1f; // Thời gian animation
        bool isCurrentlySelected = selectedCards.Contains(card);
        bool isCurrentlyRaised = Mathf.Approximately(card.transform.localPosition.y, raisedY);
        if (isCurrentlySelected && isCurrentlyRaised)
        {
            selectedCards.Remove(card);
            card.transform.DOLocalMoveY(normalY, animDuration)
                .SetEase(Ease.OutQuint);
        }
        // Nếu lá bài chưa được chọn -> nâng lên
        else if (!isCurrentlySelected)
        {
            selectedCards.Add(card);
            card.transform.DOLocalMoveY(raisedY, animDuration)
                .SetEase(Ease.OutQuint);
            Player lastPlayer = getPlayer(lastTurnName);
            if (lastPlayer == null)
            {

                // List<Card> lastCards = ListCardPlayerD[players.IndexOf(lastPlayer)];
                // Debug.Log("xem có bao lá" + lastCards.Count + " " + selectedCards.Count);
                if (selectedCards.Count == 2 && selectedCards.IndexOf(card) == 1)
                {
                    Debug.Log("có chạy vào hàm gợi ý đánh");
                    suggestCardDanh(selectedCards);
                }
                return;
            }
            if (selectedCards.Count == 1)
            {
                SuggestCardsChan(card);
            }

        }

        // Enable/disable nút đánh bài

        // Play sound effect khi click
        // playSound(Globals.SOUND_GAME.CARD_SELECT);

    }

    // Xử lý hoán đổi vị trí bài khi kéo
    private void HandleCardSwapping()
    {
        List<Card> playerCards = ListCardPlayer[players.IndexOf(thisPlayer)];
        int currentIndex = playerCards.IndexOf(selectedCard);

        // Sử dụng spacing và scale như trong chia bài
        float spacing = 60f;
        float totalWidth = (playerCards.Count - 1) * spacing;
        float startX = -totalWidth / 2f;

        float cardX = selectedCard.transform.localPosition.x;
        int newIndex = Mathf.RoundToInt((cardX - startX) / spacing);
        newIndex = Mathf.Clamp(newIndex, 0, playerCards.Count - 1);

        if (newIndex != currentIndex)
        {

            // Cập nhật thứ tự trong list
            playerCards.RemoveAt(currentIndex);
            playerCards.Insert(newIndex, selectedCard);

            // Sắp xếp lại
            RearrangeCards();
        }
    }

    // Sắp xếp lại vị trí các lá bài
    private void RearrangeCards()
    {
        List<Card> playerCards = ListCardPlayer[players.IndexOf(thisPlayer)];
        for (int i = 0; i < playerCards.Count; i++)
        {
            Card card = playerCards[i];
            float dealTime = 0.15f;
            float spacing = 60f;
            float totalWidth = (playerCards.Count - 1) * spacing;
            float startX = -totalWidth / 2f;

            // Kiểm tra xem lá bài có được chọn không
            bool isSelected = selectedCards.Contains(card);
            float targetY = isSelected ? -210f : -250f;

            Vector3 finalPos = new Vector3(startX + (i * spacing), targetY, 0f);
            Vector3 finalScale = new Vector3(0.8f, 0.8f, 1f);

            // Animation di chuyển
            card.transform.DOLocalMove(finalPos, dealTime)
                .SetEase(Ease.OutQuint)
                .OnComplete(() =>
                {
                    // Nếu lá bài ở vị trí thấp, đảm bảo nó không nằm trong danh sách chọn
                    if (card.transform.localPosition.y <= -250f && selectedCards.Contains(card))
                    {
                        selectedCards.Remove(card);
                        // Update trạng thái nút đánh bà
                    }
                });

            card.transform.DOScale(finalScale, dealTime);
            card.transform.SetSiblingIndex(i + 50);
        }
    }
    public void onClickDanhBai()
    {
        SoundManager.instance.soundClick();
        JArray arrCard = new JArray();

        for (int i = 0; i < selectedCards.Count; i++)
        {
            Card card = selectedCards[i];
            arrCard.Add(card.code);
        }
        SocketSend.danhBai(arrCard);
        selectedCards.Clear();
    }


    public void onClickBoLuot()
    {
        SocketSend.boLuot();
        handleButton(false, false, false);
    }
    public void OnClickSortCard()
    {
        SoundManager.instance.soundClick();
        SortCard();
    }

    public void SortCard()
    {
        typeSort = (typeSort + 1) % 3;

        // Lấy list bài của người chơi chính 
        List<Card> playerCards = ListCardPlayer[players.IndexOf(thisPlayer)];

        // Sắp xếp theo kiểu tương ứng
        switch (typeSort)
        {
            case 0: // Sắp tăng dần theo số + chất
                playerCards.Sort((x, y) =>
                {
                    int result = x.N.CompareTo(y.N);
                    return result != 0 ? result : x.S.CompareTo(y.S);
                });
                break;

            case 1: // Sắp giảm dần theo số + chất 
                playerCards.Sort((x, y) =>
                {
                    int result = y.N.CompareTo(x.N);
                    return result != 0 ? result : x.S.CompareTo(y.S);
                });
                break;

            case 2: // Sắp theo chất + số
                playerCards.Sort((x, y) =>
                {
                    int result = x.S.CompareTo(y.S);
                    return result != 0 ? result : x.N.CompareTo(y.N);
                });
                break;
        }

        // Disable nút sort tạm thời để tránh spam
        m_BtnSort.GetComponent<Button>().interactable = false;
        DOVirtual.DelayedCall(0.3f, () =>
        {
            m_BtnSort.GetComponent<Button>().interactable = true;
        });

        // Sắp xếp lại vị trí các lá bài
        RearrangeCards();
    }

    public override void handleRJTable(string data)
    {
        JObject JData = JObject.Parse(data);
        if ((int)JData["tableType"] == 1)
        {
            Globals.Config.typeTable = 1;
            listPosView = new List<Vector2>
{
    new Vector2(-498f, -220f),
    new Vector2(140f, 263f)
};
        }
        else
        {
            Globals.Config.typeTable = 0;
        }
        JArray listPlayer = (JArray)JData["ArrP"];
        playerBackUp.Clear();
        for (var i = 0; i < listPlayer.Count; i++)
        {
            var player = new Player();
            playerBackUp.Add(player);
            readDataPlayer(player, (JObject)listPlayer[i]);
            if (player.id == Globals.User.userMain.Userid)
                thisPlayerBackup = player;
        }
        base.handleRJTable(data);
        changePlayerToPlayerView();
        connectGame(JData);
        RearrangeCards();

    }
    public void cutCard(string nameLose, long agPlayerLose, string nameWin, long agPlayerWin, long agCut)
    {
        Player playerLose = getPlayer(nameLose);
        Player playerWin = getPlayer(nameWin);
        playerLose.ag = agPlayerLose;
        playerWin.ag = agPlayerWin;
        playerLose.updateMoney();
        playerWin.updateMoney();
        playerLose.playerView.effectFlyMoney(-agCut);
        playerWin.playerView.effectFlyMoney(agCut);


    }
    public void updateMoney(string data)
    {
        JObject dataS = JObject.Parse(data);
        Player player = getPlayer(getString(dataS, "N"));
        if (player != null)
        {
            player.ag = getLong(dataS, "AG");
            player.updateMoney();
        }
    }
    private void FadeOutAndInCards()
    {
        foreach (List<Card> cardList in ListCardPlayer)
        {
            if (cardList != null)
            {

                foreach (Card card in cardList)
                {
                    if (ListCardPlayer.IndexOf(cardList) == 0)
                    {
                        card.transform.DOLocalMoveY(-250f, 0.1f);
                    }
                    if (card != null)
                    {
                        CanvasGroup cg = card.GetComponent<CanvasGroup>();
                        if (cg == null)
                        {
                            cg = card.gameObject.AddComponent<CanvasGroup>();
                        }

                        // Set alpha về 0 ngay lập tức
                        cg.alpha = 0f;

                        // Sau 2.3s thì alpha về 1
                        DOVirtual.DelayedCall(1.5f, () =>
                        {
                            cg.alpha = 1f;
                        });
                    }
                }
            }
        }
    }



    private UniTaskCompletionSource _finishTaskSource;

    public async UniTask FinishGameTienLenAsync(string strData)
    {
        stateGame = Globals.STATE_GAME.VIEWING;
        _finishTaskSource = new UniTaskCompletionSource();
        JArray data = JArray.Parse(strData);
        bool hasTypeWin = data.Any(p => getInt((JObject)p, "TypeWin") > 0);
        Player playerSpecial = null;
        Player winPlayer = null;
        setNumberCardLast(true);
        HandleData.DelayHandleLeave = 10f;
        int type = -1;
        long moneyWin = 0;
        while (isChiabai)
        {
            await UniTask.Delay(50);
        }
        foreach (Transform child in m_BgToast.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Player player in players)
        {
            player.setTurn(false, 0);
        }
        playSound(Globals.SOUND_GAME.ALERT);
        foreach (List<Card> cardList in ListCardPlayerD)
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
        if (!hasTypeWin)
        {
            FadeOutAndInCards();
        }
        foreach (JObject playerData in data)
        {
            string playerName = getString(playerData, "N");
            Player player = getPlayer(playerName);
            if (player != null)
            {
                player.ag = getLong(playerData, "AG");
                player.point = getInt(playerData, "point");
                int typeWin = getInt(playerData, "TypeWin");
                JArray arrCard = getJArray(playerData, "ArrCard");
                List<Card> playerCards = ListCardPlayer[players.IndexOf(player)];
                for (int i = 0; i < playerCards.Count; i++)
                {
                    playerCards[i].setTextureWithCode((int)arrCard[i]);
                    Debug.Log("111");
                }
                if (typeWin > 0)
                {
                    type = typeWin;
                    playerSpecial = player;
                    m_AvatarSpecial.sprite = player.playerView.avatar.image.sprite;
                    m_NameWin.text = player.displayName;
                }
            }
        }
        if (hasTypeWin)
        {
            m_AniWinSpecial.gameObject.SetActive(true);
            List<Card> playerCards = ListCardPlayer[players.IndexOf(playerSpecial)];

            float spacing = 40f;
            int cardCount = playerCards.Count;
            float totalWidth = (cardCount - 1) * spacing;
            float startX = -totalWidth / 2f;


            foreach (Player player in players)
            {
                player.setTurn(false, 0);
            }
            setNumberCardLast(true);
            handleButton(false, false, false);
            for (int i = 0; i < cardCount; i++)
            {
                Card card = playerCards[i];
                card.gameObject.SetActive(true);
                card.transform.SetParent(m_AniWinSpecial.transform, false);
                float x = startX + i * spacing;
                float y = -144f;
                Vector3 targetPos = new Vector3(x, y, 0f);
                card.transform.DOLocalMove(targetPos, 0.4f).SetEase(Ease.OutQuint);
                card.transform.DOScale(new Vector3(0.6f, 0.6f, 1f), 0.25f).SetEase(Ease.OutQuint);
                card.transform.SetAsLastSibling();
            }
            Debug.Log(playerCards[0].transform.localPosition.y + " " + playerCards[0].transform.position.y + "xem position");


            if (playerSpecial != null)
            {

                string aniName = type switch
                {
                    1 => "four 2s",
                    2 => "dragon",
                    3 => "6 pairs",
                    4 => "four triples",
                    5 => "five Consecutive Pairs",
                    6 => "four 3s",
                    _ => "none"
                };

                if (aniName != "none")
                {
                    m_AniWinSpecial.AnimationState.SetAnimation(0, aniName, false);
                }
            }

            DOVirtual.DelayedCall(2.5f, () =>
            {
                m_AniWinSpecial?.gameObject.SetActive(false);
            });
        }
        else
        {
            m_AniFinish.gameObject.SetActive(true);
            SkeletonGraphic skeleton = m_AniFinish.GetComponent<SkeletonGraphic>();
            skeleton.timeScale = 1.4f;
            m_AniFinish.AnimationState.SetAnimation(0, "animation", true);
            m_AniCardSpecial.gameObject.SetActive(false);
            DOVirtual.DelayedCall(1.2f, () =>
            {
                if (m_AniFinish != null)
                {
                    m_AniFinish.gameObject.SetActive(false);
                }
            });

        }


        Sequence finishSequence = DOTween.Sequence();
        finishSequence.AppendInterval(0.5f);
        finishSequence.AppendCallback(() => { handleButton(false, false, false); });
        finishSequence.AppendInterval(2f);
        finishSequence.AppendCallback(() => ShowAllCards(data));
        finishSequence.AppendInterval(1.5f);
        finishSequence.AppendCallback(() =>
        {
            Vector3 centerPos = new Vector3(0, 0, 0);
            foreach (JObject playerData in data)
            {
                string playerName = getString(playerData, "N");
                Player player = getPlayer(playerName);
                long money = getLong(playerData, "M");
                if (money < 0)
                {
                    playSound(Globals.SOUND_GAME.LOSE);
                    player.playerView.effectFlyMoney(money);
                    float radius = 50f;
                    float delayStep = 0.04f;
                    Sequence chipSequence = DOTween.Sequence();
                    for (int i = 0; i < 8; i++)
                    {
                        GameObject chip = GetChipFromPool();
                        vtChipFinish.Add(chip);
                        chip.transform.position = player.playerView.transform.position;
                        float angle = Random.Range(0f, Mathf.PI * 2);
                        float distance = Random.Range(0f, radius);
                        Vector2 randomOffset = new Vector2(
                            Mathf.Cos(angle) * distance,
                            Mathf.Sin(angle) * distance
                        );
                        Vector3 destination = new Vector3(randomOffset.x, randomOffset.y, 0f);
                        Tween moveTween = chip.transform.DOLocalMove(destination, 0.8f).SetEase(Ease.OutQuint);
                        chipSequence.Insert(i * delayStep, moveTween);
                    }
                    playSound(Globals.SOUND_GAME.THROW_CHIP);
                    chipSequence.Play();
                }
                else if (money > 0)
                {
                    moneyWin = money;
                    winPlayer = player;
                }
            }
            if (winPlayer != null)
            {
                DOVirtual.DelayedCall(2.8f, () =>
                {
                    playSound(Globals.SOUND_GAME.WIN);
                    PlayerViewTienlen playerViewTienlen = getPlayerView(winPlayer);
                    winPlayer.playerView.effectFlyMoney(moneyWin);
                    playerViewTienlen.ShowAniWin();
                    playSound(Globals.SOUND_HILO.CHIP_WINNER);
                    for (int i = 0; i < vtChipFinish.Count; i++)
                    {
                        GameObject chip = vtChipFinish[i];
                        float delay = i * 0.06f;
                        Vector3 positionPlayer = winPlayer.playerView.transform.position;
                        Sequence chipSequence = DOTween.Sequence();
                        chipSequence.AppendInterval(delay)
                            .Append(chip.transform
                                .DOMove(positionPlayer, 0.3f)
                                .SetEase(Ease.OutQuint))
                            .AppendCallback(() =>
                            {
                                ReturnChipToPool(chip);
                            });
                    }
                    vtChipFinish.Clear();
                });
            }
            foreach (Player player in players)
            {
                player.updateMoney();
                for (int k = 0; k < playerBackUp.Count; k++)
                {
                    if (player.id == playerBackUp[k].id)
                    {
                        playerBackUp[k].ag = player.ag;
                        playerBackUp[k].updateMoney();
                        break;
                    }
                }
                if (player == thisPlayer)
                {
                    thisPlayerBackup.ag = thisPlayer.ag;
                    thisPlayerBackup.updateMoney();
                }
            }
        });
        finishSequence.AppendInterval(3f);
        finishSequence.AppendCallback(() =>
        {
            m_AniFinish.gameObject.SetActive(false);
            m_AniCardSpecial.gameObject.SetActive(false);
            m_AniWinSpecial.gameObject.SetActive(false);
            foreach (SkeletonGraphic anim in m_ListAniChay)
            {
                if (anim != null)
                {
                    anim.gameObject.SetActive(false);
                }
            }
            foreach (GameObject score in m_ListScore)
            {
                if (score != null)
                {
                    score.SetActive(false);
                }
            }

            selectedCards.Clear();
            turnNameCurrent = "";
            lastTurnName = "";
        });
        finishSequence.AppendInterval(1.2f);
        Debug.Log("xem stateGame" + stateGame);
        finishSequence.AppendCallback(() =>
        {
            HandleFinishGame();
            _finishTaskSource.TrySetResult();
        });
        finishSequence.Play();
        await _finishTaskSource.Task;
    }
    public override async void handleLTable(JObject data)
    {
        if (_finishTaskSource != null)
            await _finishTaskSource.Task;
        Debug.Log(HandleData.DelayHandleLeave + "xem hết bao lâu j");

        var namePl = (string)data["Name"];
        var player = getPlayer(namePl);
        if (player == null) return;

        if (player != thisPlayer && instance != null)
        {
            // Remove player theo id
            playerBackUp.RemoveAll(p => p.id == player.id);
            if (player.playerView != null)
            {
                Destroy(player.playerView.gameObject);
            }
            changePlayerToPlayerView();
            Debug.Log(HandleData.DelayHandleLeave + "xem hết bao l");
        }
        else
        {
            // Ở đây có thể mở TableView hoặc Lobby nếu cần
        }
    }

    public void HandleFinishGame()
    {
        Debug.Log("xem stateGame" + stateGame);
        isFirstTurnAndCtable = false;
        foreach (Transform chip in m_ContainerChip)
        {
            chip.gameObject.SetActive(false);
        }
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
        checkAutoExit();
        stateGame = Globals.STATE_GAME.WAITING;

    }


    private void ShowAllCards(JArray data)
    {

        // Animation sequence cho việc lật bài
        Sequence showSequence = DOTween.Sequence();
        foreach (JObject playerData in data)
        {
            string playerName = getString(playerData, "N");
            Player player = getPlayer(playerName);
            int playerIndex = players.IndexOf(player);
            List<Card> playerCards = ListCardPlayer[playerIndex];
            int typeWin = getInt(playerData, "TypeWin");
            if (playerCards.Count == 13 && ListCardPlayerD[playerIndex].Count == 0)
            {
                if (playerIndex >= 0 && playerIndex < m_ListAniChay.Count && typeWin < 0)
                {

                    SkeletonGraphic burnAnim = m_ListAniChay[playerIndex];
                    if ((Globals.Config.typeTable == 1 || players.Count() == 2) && playerIndex == 1)
                    {
                        burnAnim = m_ListAniChay[2];
                    }
                    burnAnim.gameObject.SetActive(true);
                    burnAnim.AnimationState.SetAnimation(0, "animation", true);
                }
            }
            if ((int)playerData["point"] != 0)
            {
                if ((Globals.Config.typeTable == 1 || players.Count() == 2) && playerIndex == 1)
                {
                    m_ListScore[playerIndex + 1].SetActive(true);
                    m_ListScore[playerIndex + 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = ((int)playerData["point"]).ToString();
                }
                else
                {
                    m_ListScore[playerIndex].SetActive(true);
                    m_ListScore[playerIndex].transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = ((int)playerData["point"]).ToString();
                }
            }
            if (player == thisPlayer)
            {
                continue;
            }
            float spacing = 30f;
            float startX = playerIndex == 1 ? 498f : (playerIndex == 2 ? 40f : -498f);
            if (Globals.Config.typeTable == 1 || players.Count() == 2)
            {
                startX = 40;
            }
            List<(Card card, float xPos)> cardWithPositions = new List<(Card, float)>();

            for (int i = 0; i < playerCards.Count; i++)
            {
                Card card = playerCards[i];
                float delay = i * 0.01f;

                // Tính vị trí X mục tiêu
                float xPos = (playerIndex == 1 || playerIndex == 2)
                    ? startX - (i * spacing)
                    : startX + (i * spacing);
                // Thêm vào danh sách tạm để sắp thứ tự z-index sau
                cardWithPositions.Add((card, xPos));

                // Tạo animation
                showSequence.Insert(delay, card.transform.DOLocalMoveX(xPos, 0.1f));
                showSequence.Insert(delay, card.transform.DOScale(0.4f, 0.05f));

                card.gameObject.SetActive(true);
                card.setTextureWithCode(card.code); // Lật mặt bài
            }

            // Sắp xếp theo vị trí thực tế từ trái qua phải (x tăng dần)
            cardWithPositions.Sort((a, b) => a.xPos.CompareTo(b.xPos));

            // Đặt z-index theo thứ tự trái → phải
            for (int i = 0; i < cardWithPositions.Count; i++)
            {
                cardWithPositions[i].card.transform.SetSiblingIndex(i);
            }

        }
        showSequence.Play();
    }

    private void InitChipPool()
    {
        // Tạo sẵn một số lượng chip và cho vào pool
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject chip = Instantiate(m_PrefabChip, m_ContainerChip);
            chip.SetActive(false);
            chipPool.Enqueue(chip);
        }
    }

    protected override void Awake()
    {
        isBoluot = false;
        isDanhbai = false;
        HandleData.DelayHandleLeave = 0f;
        // if (Globals.Config.typeTable == 1)
        // {
        //     listPosView = new List<Vector2>
        //     {
        //         new Vector2(-498f, -220f),
        //         new Vector2(140f, 263f)
        //     };
        // }
        base.Awake();
        instance = this;
        for (int i = 0; i < 4; i++)
        {
            ListCardPlayer.Add(new List<Card>());
            ListCardPlayerD.Add(new List<Card>());
        }

        // Khởi tạo animations cháy bài
        InitChipPool();
    }
    public static bool check3DoiThong(List<Card> listIn)
    {

        List<Card> list = new List<Card>(listIn);

        // Yêu cầu đủ 6 lá
        if (list.Count != 6)
            return false;

        // Không được chứa quân 2 (giả sử N = 15 là quân 2)
        if (list.Any(card => card.N == 2))
        {
            return false;
        }

        // Sắp xếp tăng dần theo số
        list.Sort((x, y) => x.N.CompareTo(y.N));

        // Kiểm tra từng cặp đôi và tính liên tiếp
        for (int i = 0; i < 6; i += 2)
        {
            // Mỗi đôi phải là 2 lá bằng nhau
            if (list[i].N != list[i + 1].N)
            {
                return false;
            }

            // Đảm bảo các đôi là liên tiếp nhau
            if (i < 4 && list[i].N + 1 != list[i + 2].N)
            {
                return false;
            }
        }
        return true;
    }


    // Check 4 đôi thông
    public static bool check4DoiThong(List<Card> listIn)
    {
        List<Card> list = new List<Card>(listIn);

        // Đúng 8 lá
        if (list.Count != 8)
            return false;

        // Không cho chứa quân 2 (giả sử N = 15)
        if (list.Any(card => card.N == 2))
        {
            return false;
        }

        // Sắp xếp theo số
        list.Sort((x, y) => x.N.CompareTo(y.N));

        // Kiểm tra từng đôi và tính liên tiếp
        for (int i = 0; i < 8; i += 2)
        {
            // Mỗi đôi phải là 2 lá bằng nhau
            if (list[i].N != list[i + 1].N)
            {
                return false;
            }

            // Đảm bảo đôi hiện tại liên tiếp đôi sau (trừ đôi cuối)
            if (i < 6 && list[i].N + 1 != list[i + 2].N)
            {
                return false;
            }
        }
        return true;
    }


    // Check tứ quý
    public static bool checkTuQuy(List<Card> list)
    {
        if (list.Count < 4) return false;
        list.Sort((x, y) => x.N.CompareTo(y.N));

        for (int i = 0; i < list.Count - 1; i++)
        {
            int count = 0;
            for (int j = i + 1; j < list.Count; j++)
            {
                if (list[j].N == list[i].N) count++;
            }
            if (count == 3) return true;
        }

        return false;
    }


    // Check bộ quân 2
    public static bool checkSetOfTwos(List<Card> list)
    {
        if (list.Count > 3 || list.Count < 2) return false;

        foreach (var card in list)
        {
            if (card.N != 2) return false;
        }
        return true;
    }
    public static bool check2DoiThong(List<Card> listIn)
    {
        if (listIn.Count != 4) return false;
        if (listIn.Any(card => card.N == 2)) return false;
        var list = new List<Card>(listIn);
        list.Sort((x, y) => x.N.CompareTo(y.N));
        if (list[0].N == list[1].N && list[2].N == list[3].N && list[1].N + 1 == list[2].N)
        {
            return true;
        }
        return false;
    }

    // Check đôi Tiến Lên
    public static bool checkDoiTL(List<Card> listIn)
    {
        if (listIn.Count != 2) return false;
        return listIn[0].N == listIn[1].N;
    }

    // Check sám Tiến Lên
    public static bool checkXamTL(List<Card> listIn)
    {
        if (listIn.Count != 3) return false;
        return listIn[0].N == listIn[1].N && listIn[1].N == listIn[2].N;
    }

    // Check thùng phá sảnh
    public static bool checkThungPhaSanhTL(List<Card> list)
    {
        if (!checkSanhTL(list)) return false;

        // Check cùng chất
        for (int i = 0; i < list.Count - 1; i++)
        {
            if (list[i].S != list[i + 1].S) return false;
        }
        return true;
    }

    // Check sảnh
    public static bool checkSanhTL(List<Card> list)
    {
        if (list.Count < 3) return false;

        list.Sort((x, y) => x.N.CompareTo(y.N));

        // Check liên tiếp
        for (int i = 0; i < list.Count - 1; i++)
        {
            if (list[i].N + 1 != list[i + 1].N) return false;
        }
        return true;
    }

    private GameObject GetChipFromPool()
    {
        GameObject chip;
        if (chipPool.Count > 0)
        {
            chip = chipPool.Dequeue();
        }
        else
        {
            chip = Instantiate(m_PrefabChip, m_ContainerChip);
        }
        chip.SetActive(true);
        return chip;
    }

    private void ReturnChipToPool(GameObject chip)
    {
        if (chip != null)
        {
            chip.SetActive(false);
            chipPool.Enqueue(chip);
        }
    }
    private PlayerViewTienlen getPlayerView(Player player)
    {
        if (player != null)
        {
            return (PlayerViewTienlen)player.playerView;
        }
        return null;

    }
}