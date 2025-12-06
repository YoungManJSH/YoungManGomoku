using System;
using UnityEngine;

public class Board
{
    public const int BoardSize = 15;
    public const int MaxCoord = BoardSize - 1;

    private readonly Stone[,] _nowBoard;
    private readonly JudgeType[,] _blackJudges;

    private int _nowTurn;
    public int NowTurn
    {
        get => _nowTurn;
        private set
        {
            _nowTurn = value;
            OnTurnChanged?.Invoke(value);
        }
    }

    public event Action<int> OnTurnChanged;
    public event Action BlackWin;
    public event Action WhiteWin;
    public event Func<Awaitable> OnBlackUnmovable;
    
    public Stone this[int row, int col] => _nowBoard[row, col];

    public Board()
    {
        _nowBoard = new Stone[BoardSize, BoardSize];
        _blackJudges = new JudgeType[BoardSize, BoardSize];
        NowTurn = 0;
    }

    public bool TryMoveStone(int row, int col)
    {
        if (NowTurn % 2 == 0)
        {
            if (MoveBlack(row, col) is false)
                return false;
        }
        else
        {
            MoveWhite(row, col);
            if (NowTurn > 6) // 흑돌이 4개 이상 있고 백돌 착수 이후
            {
                UpdateBlackJudges();
            }
        }

        ++NowTurn;
        
        return true;
    }

    private void UpdateBlackJudges()
    {
        bool isMovable = false;
        
        for (int row = 0; row < BoardSize; ++row)
        {
            for (int col = 0; col < BoardSize; ++col)
            {
                if (_nowBoard[row, col] != Stone.Empty)
                {
                    _blackJudges[row, col] = JudgeType.None;
                    continue;
                }

                _blackJudges[row, col] = JudgeMove.JudgeBlackMove(this, row, col);
                if (_blackJudges[row, col] != JudgeType.Forbidden) isMovable = true;
            }
        }

        if (isMovable is false)
        {
            _ = InvokeOnBlackUnmovable();
        }
    }

    private async Awaitable InvokeOnBlackUnmovable()
    {
        Delegate[] invokeList = OnBlackUnmovable!.GetInvocationList();
        foreach (Delegate del in invokeList)
        {
            Func<Awaitable> subscriber = (Func<Awaitable>)del;
            await subscriber();
        }
        
        WhiteWin!.Invoke();
    }

    private bool MoveBlack(int row, int col)
    {
        Debug.Assert(0 <= row && row < BoardSize);
        Debug.Assert(0 <= col && col < BoardSize);
        Debug.Assert(_nowBoard[row, col] == Stone.Empty);

        if (_blackJudges[row, col] == JudgeType.Forbidden)
            return false;

        _nowBoard[row, col] = Stone.Black;

        if (_blackJudges[row, col] == JudgeType.Omok)
        {
            BlackWin!.Invoke();
        }
        
        return true;
    }

    private void MoveWhite(int row, int col)
    {
        Debug.Assert(0 <= row && row < BoardSize);
        Debug.Assert(0 <= col && col < BoardSize);
        Debug.Assert(_nowBoard[row, col] == Stone.Empty);

        _nowBoard[row, col] = Stone.White;

        if (JudgeMove.JudgeWhiteOmok(this, row, col))
        {
            WhiteWin!.Invoke();
        }
    }
}