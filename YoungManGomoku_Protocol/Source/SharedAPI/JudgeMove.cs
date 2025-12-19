using YoungManGomoku_Protocol.TypeEnum.InGame;

using LineDir = YoungManGomoku_Protocol.TypeEnum.InGame.LineDirection;
public static class JudgeMove
{
    /// <summary> 3의 종류: Solid, Ic(Indirect Closed), Broken(틈 3) </summary>
    private enum CaseOf3
    {
        /// <summary> 3이 아님 </summary>
        Not,

        /// <summary> 연속된 3, 양쪽에서 열린 4로 이을 수 있음 </summary>
        Solid,

        /// <summary> 앞쪽에서 열린 4로 이을 수 없는 연속된 3, 뒷쪽에서만 열린 4로 이어짐 </summary>
        PrevInDirectClosed,

        /// <summary> 뒷쪽에서 열린 4로 이을 수 없는 연속된 3, 앞쪽에서만 열린 4로 이어짐 </summary>
        NextInDirectClosed,

        /// <summary> 틈 3, 탐색 좌표보다 앞쪽에 틈이 존재 </summary>
        PrevBroken,

        /// <summary> 틈 3, 탐색 좌표보다 뒷쪽에 틈이 존재 </summary>
        NextBroken
    }

    /// <summary> 특정 3에 대해 열린 4로 이어지는 좌표의 정보를 담는 구조체 </summary>
    private struct Open4Place
    {
        public readonly LineDir dir;
        public readonly CaseOf3 type;
        public int row1;
        public int col1;
        public int row2;
        public int col2;

        public Open4Place(LineDir dir, CaseOf3 type)
        {
            this.dir = dir;
            this.type = type;
            row1 = col1 = row2 = col2 = -1;
        }

        /// <summary>
        /// Solid type일 경우 열린 4로 이어지는 앞쪽 좌표 입력
        /// <para>Solid가 아닌 type은 열린 4로 이어지는 유일한 좌표 입력</para>
        /// </summary>
        public void InputCoord1((int row, int col) coord)
        {
            row1 = coord.row;
            col1 = coord.col;
        }

        /// <summary>
        /// Solid type일 경우 열린 4로 이어지는 뒷쪽 좌표 입력
        /// </summary>
        public void InputCoord2((int row, int col) coord)
        {
            row2 = coord.row;
            col2 = coord.col;
        }
    };

    /// <summary>
    /// 흑돌의 [row, col] 위치 착수가 오목 혹은 금수인지 판정
    /// </summary>
    /// <returns>
    /// <para>None: 금수 또는 오목 위치 아님</para>
    /// <para>Omok: 오목</para>
    /// <para>Forbidden: 금수</para>
    /// </returns>
    public static JudgeType JudgeBlackMove(Board board, int row, int col)
    {
        JudgeType omokJudge = JudgeBlackOmok(board, row, col);

        if (omokJudge == JudgeType.Gomoku) return JudgeType.Gomoku;

        return omokJudge == JudgeType.Forbidden || Is44(board, row, col) || Is33(board, row, col)
            ? JudgeType.Forbidden
            : JudgeType.None;
    }

    /// <summary>
    /// 백돌의 [row, col] 위치 착수가 오목(장목 포함)인지 판정
    /// </summary>
    /// <returns>
    /// <para>true: 오목 (장목 포함)</para>
    /// <para>false: 오목 아님</para>
    /// </returns>
    public static bool JudgeWhiteOmok(Board board, int row, int col)
    {
        for (LineDir dir = 0; dir < LineDir.Max; ++dir)
        {
            if (LineCount(board, row, col, StoneColorType.White, dir) >= 5)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 흑돌의 [row,col] 위치 착수가 오목 혹은 장목인지 판정
    /// </summary>
    /// <param name="board">오목판 정보 개체</param>
    /// <param name="row">착수 위치 row</param>
    /// <param name="col">착수 위치 col</param>
    /// <param name="passing">선택 매개변수, 탐색 제외 방향</param>
    /// <returns>
    /// <para>None: 오목 또는 장목 위치 아님</para>
    /// <para>Omok: 오목</para>
    /// <para>Forbidden: 장목(금수)</para>
    /// </returns>
    private static JudgeType JudgeBlackOmok(Board board, int row, int col, LineDir passing = LineDir.None)
    {
        JudgeType result = JudgeType.None;

        for (LineDir dir = 0; dir < LineDir.Max; ++dir)
        {
            if (dir == passing) continue;
            int count = LineCount(board, row, col, StoneColorType.Black, dir);
            if (count == 5) return JudgeType.Gomoku;
            if (count > 5) result = JudgeType.Forbidden;
        }

        return result;
    }

    /// <summary>
    /// 좌표를 지정된 방향으로 지정된 칸만큼 이동시키는 함수 
    /// </summary>
    /// <param name="coord">이동시킬 좌표</param>
    /// <param name="dir">이동시킬 방향</param>
    /// <param name="offset">이동시킬 칸 수</param>
    /// <returns>
    /// <para>true : 이동된 좌표가 오목판 범위 안에 있음</para>
    /// <para>false : 이동 시 오목판 범위를 벗어남. 좌표 변경은 이루어지지 않음</para>
    /// </returns>
    private static bool CoordinateMove(ref (int row, int col) coord, LineDir dir, int offset)
    {
        switch (dir)
        {
            case LineDir.Horizontal:
                int col = coord.col + offset;
                if (col < 0 || Board.MaxCoord < col) return false;
                coord.col = col;
                return true;
            case LineDir.Vertical:
                int row = coord.row + offset;
                if (row < 0 || Board.MaxCoord < row) return false;
                coord.row = row;
                return true;
            case LineDir.DiagonalUp:
                row = coord.row - offset;
                col = coord.col + offset;
                if (row < 0 || Board.MaxCoord < row || col < 0 || Board.MaxCoord < col)
                    return false;
                coord.row = row;
                coord.col = col;
                return true;
            case LineDir.DiagonalDown:
                row = coord.row + offset;
                col = coord.col + offset;
                if (row < 0 || Board.MaxCoord < row || col < 0 || Board.MaxCoord < col)
                    return false;
                coord.row = row;
                coord.col = col;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// [row, col] 위치 착수 시 지정된 방향으로 몇 개의 돌이 연속되는지 탐색
    /// </summary>
    /// <param name="board">오목판 정보 개체</param>
    /// <param name="row">착수 위치 row</param>
    /// <param name="col">착수 위치 col</param>
    /// <param name="stone">착수할 돌 색깔, Black or White</param>
    /// <param name="dir">탐색 방향</param>
    /// <returns>[row, col] 위치를 포함해서 연속된 돌의 개수</returns>
    private static int LineCount(Board board, int row, int col, StoneColorType stone, LineDir dir)
    {
        var startCoord = (row, col);
        var endCoord = (row, col);

        while (CoordinateMove(ref startCoord, dir, -1))
        {
            if (board[startCoord.row, startCoord.col] != stone)
            {
                CoordinateMove(ref startCoord, dir, +1);
                break;
            }
        }

        while (CoordinateMove(ref endCoord, dir, +1))
        {
            if (board[endCoord.row, endCoord.col] != stone)
            {
                CoordinateMove(ref endCoord, dir, -1);
                break;
            }
        }

        return dir == LineDir.Vertical ? endCoord.row - startCoord.row + 1 : endCoord.col - startCoord.col + 1;
    }

    /// <summary>
    /// 흑돌의 [row, col] 위치 착수가 4·4 금수인지 탐색
    /// </summary>
    /// <returns>
    /// <para>true: 4·4 금수, [row, col] 착수 시 2개 이상의 4가 만들어짐</para>
    /// <para>false: 4·4 금수가 아님, 1개 이하의 4가 만들어짐</para>
    /// </returns>
    private static bool Is44(Board board, int row, int col)
    {
        int count4 = 0;

        for (LineDir dir = 0; dir < LineDir.Max; ++dir)
        {
            count4 += Count4(board, row, col, dir);
            if (count4 > 1) return true;
        }

        return false;
    }

    /// <summary>
    /// 열린 4로 이어지는 위치 [row, col]이 흑돌의 4·4 금수인지 탐색
    /// </summary>
    /// <param name="board">오목판 정보 개체</param>
    /// <param name="row">열린 4로 이어지는 위치 row</param>
    /// <param name="col">열린 4로 이어지는 위치 col</param>
    /// <param name="passing">열린 4가 이어지는 방향(해당 방향은 탐색 제외)</param>
    /// <returns>
    /// <para>true : 4·4 금수, [row, col] 위치에서 추가적인 4가 만들어짐</para>
    /// <para>false : 4·4 금수가 아님, 해당 방향의 열린 4 하나만 만들어짐</para>
    /// </returns>
    private static bool Is44(Board board, int row, int col, LineDir passing)
    {
        for (LineDir dir = 0; dir < LineDir.Max; ++dir)
        {
            if (dir == passing) continue;
            if (Count4(board, row, col, dir) > 0) return true;
        }

        return false;
    }

    /// <summary>
    /// [row, col] 위치 착수 시 지정된 방향으로 몇 개의 4가 만들어지는지 탐색
    /// </summary>
    /// <returns> 0 or 1 or 2 </returns>
    private static int Count4(Board board, int row, int col, LineDir dir)
    {
        var startCoord = (row, col);
        var endCoord = (row, col);

        while (CoordinateMove(ref startCoord, dir, -1))
        {
            if (board[startCoord.row, startCoord.col] != StoneColorType.Black)
            {
                CoordinateMove(ref startCoord, dir, +1);
                break;
            }
        }

        while (CoordinateMove(ref endCoord, dir, +1))
        {
            if (board[endCoord.row, endCoord.col] != StoneColorType.Black)
            {
                CoordinateMove(ref endCoord, dir, -1);
                break;
            }
        }

        int lineCount = dir == LineDir.Vertical
            ? endCoord.row - startCoord.row + 1
            : endCoord.col - startCoord.col + 1;
        
        if (lineCount == 4)
        {
            // 둘 중 한쪽 이상 열려 있는지
            if ((CoordinateMove(ref startCoord, dir, -1) &&
                 board[startCoord.row, startCoord.col] == StoneColorType.Empty &&
                 (CoordinateMove(ref startCoord, dir, -1) is false ||
                  board[startCoord.row, startCoord.col] != StoneColorType.Black)) ||
                (CoordinateMove(ref endCoord, dir, +1) &&
                 board[endCoord.row, endCoord.col] == StoneColorType.Empty &&
                 (CoordinateMove(ref endCoord, dir, +1) is false ||
                  board[endCoord.row, endCoord.col] != StoneColorType.Black)))
            {
                return 1;
            }

            // 둘 다 닫혀있다면
            return 0;
        }

        // 여기서부터는 lineCount <= 3인 경우
        int count4 = 0;
        int remaining = 4 - lineCount; // 4가 되기 위해 필요한 앞뒤 돌 수

        // startCoord 방향 틈 4 탐색하기
        if (CoordinateMove(ref startCoord, dir, -remaining - 1))
        {
            bool flag = true;

            for (int count = 0; count < remaining; ++count)
            {
                if (board[startCoord.row, startCoord.col] != StoneColorType.Black)
                {
                    flag = false;
                    break;
                }

                CoordinateMove(ref startCoord, dir, +1);
            }

            if (flag && board[startCoord.row, startCoord.col] == StoneColorType.Empty &&
                (CoordinateMove(ref startCoord, dir, -remaining - 1) is false ||
                 board[startCoord.row, startCoord.col] != StoneColorType.Black))
            {
                ++count4;
            }
        }

        // endCoord 방향 틈 4 탐색하기
        if (CoordinateMove(ref endCoord, dir, +remaining + 1))
        {
            bool flag = true;

            for (int count = 0; count < remaining; ++count)
            {
                if (board[endCoord.row, endCoord.col] != StoneColorType.Black)
                {
                    flag = false;
                    break;
                }

                CoordinateMove(ref endCoord, dir, -1);
            }

            if (flag && board[endCoord.row, endCoord.col] == StoneColorType.Empty &&
                (CoordinateMove(ref endCoord, dir, +remaining + 1) is false ||
                 board[endCoord.row, endCoord.col] != StoneColorType.Black))
            {
                ++count4;
            }
        }

        return count4;
    }

    /// <summary>
    /// [row, col] 위치가 흑돌의 3·3 금수인지 탐색
    /// </summary>
    /// <param name="board">오목판 정보 개체</param>
    /// <param name="row">탐색 위치 row</param>
    /// <param name="col">탐색 위치 col</param>
    /// <param name="passing">열린 4 위치 탐색 시 입력하는 선택 매개변수, 탐색 제외 방향</param>
    /// <returns>
    /// <para>true: 3·3 금수, [row, col] 착수 시 2개 이상의 3이 만들어짐</para>
    /// <para>false: 3·3 금수가 아님, 1개 이하의 3이 만들어짐</para>
    /// </returns>
    private static bool Is33(Board board, int row, int col, LineDir passing = LineDir.None)
    {
        int count3 = 0;
        Open4Place open4Place = default;
        bool isOpen4PlaceChecked = false;

        for (LineDir dir = 0; dir < LineDir.Max; ++dir)
        {
            if (dir == passing) continue;

            CaseOf3 result = Judge3Place(board, row, col, dir);
            if (result != CaseOf3.Not)
            {
                ++count3;

                if (count3 == 2 && isOpen4PlaceChecked is false &&
                    IsForbidden(board, open4Place.row1, open4Place.col1, open4Place.dir) &&
                    (open4Place.type != CaseOf3.Solid ||
                     IsForbidden(board, open4Place.row2, open4Place.col2, open4Place.dir)))
                {
                    count3 = 1;
                }

                open4Place = SearchOpen4Place(board, row, col, result, dir);

                if (count3 == 2)
                {
                    if (IsForbidden(board, open4Place.row1, open4Place.col1, open4Place.dir) &&
                        (open4Place.type != CaseOf3.Solid ||
                         IsForbidden(board, open4Place.row2, open4Place.col2, open4Place.dir)))
                    {
                        isOpen4PlaceChecked = true;
                        count3 = 1;
                        continue;
                    }

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 열린 4로 이어지는 위치가 금수인지 탐색
    /// </summary>
    /// <param name="board">오목판 정보 개체</param>
    /// <param name="row">열린 4로 이어지는 위치 row</param>
    /// <param name="col">열린 4로 이어지는 위치 col</param>
    /// <param name="passing">열린 4가 이어지는 방향(해당 방향은 탐색 제외)</param>
    private static bool IsForbidden(Board board, int row, int col, LineDir passing)
    {
        JudgeType omokJudge = JudgeBlackOmok(board, row, col, passing);
        if (omokJudge == JudgeType.Gomoku) return false;
        return omokJudge == JudgeType.Forbidden || Is44(board, row, col, passing) || Is33(board, row, col, passing);
    }

    /// <summary>
    /// [row, col] 위치에서 dir 방향으로 3이 만들어질 때, 해당 3이 열린 4로 이어지는 좌표의 정보를 반환
    /// </summary>
    /// <param name="board">오목판 정보 개체</param>
    /// <param name="row">3이 만들어지는 위치 row</param>
    /// <param name="col">3이 만들어지는 위치 col</param>
    /// <param name="caseOf3">만들어지는 3의 종류</param>
    /// <param name="dir">만들어지는 3의 방향</param>
    private static Open4Place SearchOpen4Place(Board board, int row, int col, CaseOf3 caseOf3, LineDir dir)
    {
        Open4Place open4Place = new Open4Place(dir, caseOf3);
        var coord = (row, col);

        switch (caseOf3)
        {
            case CaseOf3.Solid:
                while (CoordinateMove(ref coord, dir, -1))
                {
                    if (board[coord.row, coord.col] == StoneColorType.Empty)
                        break;
                }

                open4Place.InputCoord1(coord);
                CoordinateMove(ref coord, dir, +4);
                open4Place.InputCoord2(coord);
                break;
            case CaseOf3.PrevInDirectClosed:
                while (CoordinateMove(ref coord, dir, +1))
                {
                    if (board[coord.row, coord.col] == StoneColorType.Empty)
                        break;
                }

                open4Place.InputCoord1(coord);
                break;
            case CaseOf3.NextInDirectClosed:
                while (CoordinateMove(ref coord, dir, -1))
                {
                    if (board[coord.row, coord.col] == StoneColorType.Empty)
                        break;
                }

                open4Place.InputCoord1(coord);
                break;
            case CaseOf3.PrevBroken:
                goto case CaseOf3.NextInDirectClosed;
            case CaseOf3.NextBroken:
                goto case CaseOf3.PrevInDirectClosed;
        }

        return open4Place;
    }

    /// <summary>
    /// [row, col] 위치 착수 시 지정된 방향으로 3이 만들어지는지 탐색
    /// </summary>
    /// <returns>만들어지는 3의 종류를 의미하는 enum값</returns>
    private static CaseOf3 Judge3Place(Board board, int row, int col, LineDir dir)
    {
        int blackCount = 1;
        CaseOf3 nowShape = CaseOf3.Solid;
        var coord = (row, col);

        // 앞쪽 탐색
        while (true)
        {
            if (blackCount > 3 || CoordinateMove(ref coord, dir, -1) is false)
            {
                return CaseOf3.Not;
            }

            StoneColorType nowStone = board[coord.row, coord.col];

            if (nowStone == StoneColorType.White) return CaseOf3.Not;

            if (nowStone == StoneColorType.Black)
            {
                ++blackCount;
                continue;
            }

            #region 탐색 중 빈 칸을 만난 경우

            // 빈 칸 앞쪽에 공간이 없을 때
            if (CoordinateMove(ref coord, dir, -1) is false)
            {
                if (nowShape == CaseOf3.Solid)
                {
                    nowShape = CaseOf3.PrevInDirectClosed;
                }

                // 틈 3(Broken)이라면 PrevIc는 의미가 없음 
                break;
            }

            nowStone = board[coord.row, coord.col];

            // 이미 틈 3(Broken)인데 다시 빈 칸이 나온 경우
            if (nowShape >= CaseOf3.PrevBroken)
            {
                // 3 패턴이 나오더라도 닫힌 4로 이어지게 됨.
                if (nowStone == StoneColorType.Black)
                    return CaseOf3.Not;

                // 그밖의 경우는 뒷쪽 탐색으로 넘어가서 판단
                break;
            }

            if (nowStone == StoneColorType.White)
            {
                nowShape = CaseOf3.PrevInDirectClosed;
                break;
            }

            if (nowStone == StoneColorType.Black)
            {
                ++blackCount;
                nowShape = CaseOf3.PrevBroken;
                continue;
            }

            // 세 칸 앞에 흑돌이 있는 경우 = PrevIc 상황
            if (CoordinateMove(ref coord, dir, -1) && board[coord.row, coord.col] == StoneColorType.Black)
            {
                nowShape = CaseOf3.PrevInDirectClosed;
            }

            // 두 칸 연속 빈 칸이면 앞쪽 탐색 종료
            break;

            #endregion
        }

        // 뒷쪽 탐색
        coord = (row, col);
        while (true)
        {
            if (blackCount > 3 || CoordinateMove(ref coord, dir, +1) is false)
            {
                return CaseOf3.Not;
            }

            StoneColorType nowStone = board[coord.row, coord.col];

            if (nowStone == StoneColorType.White) return CaseOf3.Not;

            if (nowStone == StoneColorType.Black)
            {
                ++blackCount;
                continue;
            }

            #region 탐색 중 빈 칸을 만난 경우

            // 빈 칸 뒷쪽에 공간이 없을 때
            if (CoordinateMove(ref coord, dir, +1) is false)
            {
                if (blackCount < 3) return CaseOf3.Not;

                if (nowShape == CaseOf3.Solid) return CaseOf3.NextInDirectClosed;

                if (nowShape >= CaseOf3.PrevBroken) return nowShape;

                // nowShape == CaseOf3.PrevIc, 양쪽이 Ic가 되므로 3이 아님
                return CaseOf3.Not;
            }

            nowStone = board[coord.row, coord.col];

            // 이미 틈 3(Broken)인데 다시 빈 칸이 나온 경우
            if (nowShape >= CaseOf3.PrevBroken)
            {
                // 조건식 : 3 패턴이 아니거나 닫힌 4로 이어지는지?
                return blackCount < 3 || nowStone == StoneColorType.Black ? CaseOf3.Not : nowShape;
            }

            if (nowStone == StoneColorType.White)
            {
                // NextIc 상황이므로 양쪽이 모두 Ic면 3이 아님
                return blackCount < 3 || nowShape == CaseOf3.PrevInDirectClosed ? CaseOf3.Not : CaseOf3.NextInDirectClosed;
            }

            if (nowStone == StoneColorType.Black)
            {
                ++blackCount;
                nowShape = CaseOf3.NextBroken;
                continue;
            }

            // 두 칸 연속 비어있는 경우
            if (blackCount < 3) return CaseOf3.Not;

            // 세 칸 뒤에 흑돌이 있는 경우 = NextIc 상황
            if (CoordinateMove(ref coord, dir, +1) && board[coord.row, coord.col] == StoneColorType.Black)
            {
                return nowShape == CaseOf3.PrevInDirectClosed ? CaseOf3.Not : CaseOf3.NextInDirectClosed;
            }

            // nowShape is Solid or PrevIc
            return nowShape;

            #endregion
        }
    }
}