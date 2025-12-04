// Chuyển đổi từ JavaScript sang C#
// LogicManager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LogicCatteManager : MonoBehaviour
{
    public bool IsInvalid = false;

    public int CheckListGetScore(List<Card> listIn, int numSanh = 0)
    {
        var list = new List<Card>(listIn);
        IsInvalid = false;
        int countSanh = list.Count(card =>
        {
            var type = GetTypeCardRummy(new List<Card> { card });
            return type == TypeCardRummy.TCR_PURE || type == TypeCardRummy.IMPURE;
        });

        if (countSanh >= numSanh)
        {
            IsInvalid = true;
            list.RemoveAll(card =>
            {
                var type = GetTypeCardRummy(new List<Card> { card });
                return type == TypeCardRummy.TCR_PURE || type == TypeCardRummy.IMPURE;
            });
        }

        int score = 0;
        if (IsInvalid)
        {
            score = list.Where(card => GetTypeCardRummy(new List<Card> { card }) == TypeCardRummy.NONE)
                        .Sum(card => GetScore(new List<Card> { card }));
        }
        else
        {
            score = list.Sum(card => GetScore(new List<Card> { card }));
        }

        return Mathf.Min(score, 80);
    }

    public TypeCardRummy GetTypeCardRummy(List<Card> listIn)
    {
        var list = new List<Card>(listIn);
        if (CheckJoker(list))
            return list.Count > 2 ? TypeCardRummy.IMPURE : TypeCardRummy.JOKER;
        // else if (CheckPureXam(list) || CheckPureTPS(list))
        // return TypeCardRummy.TCR_PURE;
        else if (CheckImpure(list))
            return TypeCardRummy.IMPURE;
        else if (CheckSet(list))
            return TypeCardRummy.SET;

        return TypeCardRummy.NONE;
    }

    public bool CheckPureXam(List<Card> listIn)
    {
        var list = new List<Card>(listIn);
        // if (list.Count != 3 || CheckDoubleEat(list)) return false;
        return list.All(c => c.code == list[0].code);
    }

    // public static bool CheckPureTPS(List<Card> listIn)
    // {
    //     var list = new List<Card>(listIn);
    //     // if (list.Count < 3 || list.Count > 4 || CheckDoubleEat(list)) return false;
    //     list.Sort(CompareTalaForN);
    //     // return CheckThungPhaSanh(list, list.Count);
    // }

    public bool CheckImpure(List<Card> listIn)
    {
        // ... chuyển đổi chi tiết checkImpure tương tự như trên
        // Để rút gọn, phần này bạn có thể yêu cầu mình hoàn thiện sau
        return false;
    }

    public bool CheckSet(List<Card> listIn)
    {
        var list = new List<Card>(listIn);
        // if (list.Count < 3 || list.Count > 4 || CheckDoubleEat(list)) return false;
        list.RemoveAll(c => c.isJoker);
        if (list.Count <= 1) return false;
        return list.All(c => c.N == list[0].N);
    }

    public bool CheckJoker(List<Card> listIn)
    {
        var list = new List<Card>(listIn);
        // if (list.Count < 3 || list.Count > 4 || CheckDoubleEat(list)) return false;
        return list.All(c => c.isJoker);
    }

    // public static bool CheckDoubleEat(List<Card> listIn)
    // {
    //     return listIn.Count(c => c.GetIsEat()) >= 2;
    // }

    public int GetScore(List<Card> listIn)
    {
        int score = 0;
        foreach (var c in listIn)
        {
            if (c.isJoker) continue;
            if (c.N >= 14) score += 1;
            else if (c.N >= 1 && c.N <= 9) score += c.N;
            else if (c.N >= 10 && c.N < 14) score += 10;
        }
        return score;
    }

    public int CompareTalaForN(Card x, Card y)
    {
        int xN = x.N > 13 ? x.N - 13 : x.N;
        int yN = y.N > 13 ? y.N - 13 : y.N;
        if (xN > yN || (xN == yN && x.S > y.S)) return 1;
        return -1;
    }

    public int CompareTalaForN_2(Card x, Card y)
    {
        if (x.N > y.N || (x.N == y.N && x.S > y.S)) return 1;
        return -1;
    }

    public int CompareTalaForS(Card x, Card y)
    {
        return x.S > y.S ? 1 : -1;
    }

    // TODO: Các hàm còn lại như CheckTuQuy, CheckCulu, CheckThung, CheckSanh,...
    // sẽ tiếp tục được chuyển nếu bạn yêu cầu.
}

public enum TypeCardRummy
{
    NONE,
    TCR_PURE,
    IMPURE,
    SET,
    JOKER
}

// Card class và các enum CODE_JOKER_BLACK, CODE_JOKER_RED cần được định nghĩa sẵn
