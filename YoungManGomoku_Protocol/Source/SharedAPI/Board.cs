using System;
using System.Collections.Generic;
using YoungManGomoku_Protocol.TypeEnum.InGame;

public class Board
{
    public const int BoardSize = 15;
    public const int MaxCoord = BoardSize - 1;
    
    private readonly StoneColorType[,] _nowBoard;
    private readonly JudgeType[,] _blackJudges;
    private readonly List<(int row, int col)> _record;

    /// <summary> 마지막 백돌 착수 시점 기준으로 흑돌이 지정한 좌표에 착수할 경우 오목 또는 금수인지를 반환 </summary>
    /// <param name="coord"> 확인할 좌표 </param>
    /// <returns>
    /// <para>None: 오목 또는 금수 자리가 아님, 혹은 이미 착수되어 있는 자리</para>
    /// <para>Gomoku: 오목이 되는 자리</para>
    /// <para>Forbidden: 금수인 자리</para>
    /// </returns>
    public JudgeType BlackJudge((int row, int col) coord)
        => _blackJudges[coord.row, coord.col];
    /// <summary> 기보를 방어적 복사로 전달하는 프로퍼티 </summary>
    public List<(int row, int col)> Record => new List<(int row, int col)>(_record); // C# 8


	private int _nowTurn;
    public int NowTurn
    {
        get => _nowTurn;
        private set
        {
            _nowTurn = value;
            if (value == BoardSize * BoardSize)
            {
                OverMaxTurn?.Invoke();
                return;
            }
            
            if (value == 3)
            {
                OnTurnBackActivate?.Invoke();
            }
            
            OnTurnChanged?.Invoke(value);
        }
    }
    
    public event Action<int> OnTurnChanged; // 현재 턴을 매개변수로 전달
    public event Action OnTurnBackActivate; // 무르기 활성화 이벤트 (3수 착수)
    public event Action OnBlackGomoku;      // 흑돌 오목 상황
    public event Action OnWhiteGomoku;      // 백돌 오목(장목 포함) 상황
    public event Action OverMaxTurn;        // 판 꽉 채운 경우 (무승부 처리로 연결)
    public event Action OnBlackUnmovable;   // 흑돌 금수패 상황 (백돌 승리로 연결)
    
    public StoneColorType this[int row, int col] => _nowBoard[row, col];

    public Board()
    {
        _nowBoard = new StoneColorType[BoardSize, BoardSize];
        _blackJudges = new JudgeType[BoardSize, BoardSize];
        _record = new List<(int row, int col)>();
        NowTurn = 0;
    }

    public bool TryMoveStone(int row, int col)
    {
        if (row < 0 || MaxCoord < row ||
            col < 0 || MaxCoord < col ||
            _nowBoard[row, col] != StoneColorType.Empty)
        {
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

        _nowBoard[_record[^1].row, _record[^1].col] = StoneColorType.Empty;
        _nowBoard[_record[^2].row, _record[^2].col] = StoneColorType.Empty;
        
        _record.RemoveRange(_record.Count - 2, 2);
        NowTurn -= 2;
        
        return true;
    }
    
    private void UpdateBlackJudges()
    {
        bool isMovable = false;
        
        for (int row = 0; row < BoardSize; ++row)
        {
            for (int col = 0; col < BoardSize; ++col)
            {
                if (_nowBoard[row, col] != StoneColorType.Empty)
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
            OnBlackUnmovable?.Invoke();
        }
    }

    private bool MoveBlack(int row, int col)
    {
        if (_blackJudges[row, col] == JudgeType.Forbidden)
        {
            return false;
        }

        _nowBoard[row, col] = StoneColorType.Black;
        _record.Add((row, col));
        ++NowTurn;

        if (_blackJudges[row, col] == JudgeType.Gomoku)
        {
            OnBlackGomoku?.Invoke();
        }

        return true;
    }

    private void MoveWhite(int row, int col)
    {
        _nowBoard[row, col] = StoneColorType.White;
        _record.Add((row, col));
        ++NowTurn;

        if (JudgeMove.JudgeWhiteOmok(this, row, col))
        {
            OnWhiteGomoku?.Invoke();
            return;
        }
        
        if (NowTurn > 7) // 흑돌이 4개 이상 있고 백돌 착수 이후
        {
            UpdateBlackJudges();
        }
    }
}