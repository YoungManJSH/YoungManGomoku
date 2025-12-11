using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using YoungManGomoku_Protocol;

public class Board
{
    public const int BoardSize = 15;
    public const int MaxCoord = BoardSize - 1;
    
    private readonly Stone[,] _nowBoard;
    private readonly JudgeType[,] _blackJudges;
    private readonly List<(int row, int col)> _record;

    private int _nowTurn;
    public int NowTurn
    {
        get => _nowTurn;
        private set
        {
            _nowTurn = value;
            if (value == BoardSize * BoardSize)
            {
                OverMaxTurn!.Invoke();
                return;
            }
            
            if (value == 3)
            {
                OnTurnBackActivate?.Invoke();
            }
            
            OnTurnChanged?.Invoke(value);
        }
    }
    
    public event Action<int> OnTurnChanged;
    public event Action OnTurnBackActivate;
    public event Action BlackWin;
    public event Action WhiteWin;
    public event Action OverMaxTurn;
    public event Action FailedSaveRecord;
    public event Func<Awaitable> OnBlackUnmovable;
    
    public Stone this[int row, int col] => _nowBoard[row, col];

    public Board()
    {
        _nowBoard = new Stone[BoardSize, BoardSize];
        _blackJudges = new JudgeType[BoardSize, BoardSize];
        _record = new List<(int row, int col)>();
        NowTurn = 0;
    }

    public bool TryMoveStone(int row, int col)
    {
        if (row < 0 || MaxCoord < row ||
            col < 0 || MaxCoord < col ||
            _nowBoard[row, col] != Stone.Empty)
        {
            Debug.LogError("잘못된 Board 좌표 입력");
            return false;
        }
        
        if (NowTurn % 2 == 0)
        {
            if (MoveBlack(row, col) is false)
                return false;
        }
        else
        {
            MoveWhite(row, col);
        }
        
        return true;
    }

    public bool TryTakeBack()
    {
        if (NowTurn < 3 || NowTurn != _record.Count)
        {
            return false;
        }

        _nowBoard[_record[^1].row, _record[^1].col] = Stone.Empty;
        _nowBoard[_record[^2].row, _record[^2].col] = Stone.Empty;
        
        _record.RemoveRange(_record.Count - 2, 2);
        NowTurn -= 2;
        
        return true;
    }

    public void SaveRecord(PlayerData blackUser, PlayerData whiteUser, string result)
    {
        try
        {
            string fileName = $"{DateTime.Now:yyyyMMddHHmm}-{blackUser.Nickname},{whiteUser.Nickname}.gibo";
        
#if UNITY_STANDALONE || UNITY_EDITOR
            string basePath = Environment.CurrentDirectory;
#elif UNITY_ANDROID
            string basePath = Application.persistentDataPath;
#endif

            string giboPath = Path.Combine(basePath, "Gibo");
            // Gibo 폴더가 없으면 생성
            if (Directory.Exists(giboPath) is false)
            {
                Directory.CreateDirectory(giboPath);
            }

            giboPath = Path.Combine(giboPath, fileName);

            // finally 블럭에서 Dispose를 적는 대신 using으로 처리
            using StreamWriter writer = new StreamWriter(giboPath);
            
            writer.Write($"{blackUser.Nickname}({blackUser.WinCount},{blackUser.DrawCount},{blackUser.LoseCount}),");
            writer.WriteLine($"{whiteUser.Nickname}({whiteUser.WinCount},{whiteUser.DrawCount},{whiteUser.LoseCount})");
            
            writer.WriteLine($"{NowTurn},{result}");

            foreach ((int row, int col) in _record)
            {
                writer.WriteLine($"{row},{col}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"기보 저장 실패! : {e}");
            FailedSaveRecord?.Invoke();
        }
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
        {
            return false;
        }

        _nowBoard[row, col] = Stone.Black;
        _record.Add((row, col));
        ++NowTurn;

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
        _record.Add((row, col));
        ++NowTurn;

        if (JudgeMove.JudgeWhiteOmok(this, row, col))
        {
            WhiteWin!.Invoke();
            return;
        }
        
        if (NowTurn > 7) // 흑돌이 4개 이상 있고 백돌 착수 이후
        {
            UpdateBlackJudges();
        }
    }
}