using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GiboFileManager
{
    public readonly struct GiboTitleData
    {
        public string FileName { get; }
        public DateTime DateTime { get; }
        public string BlackPlayer { get; }
        public string WhitePlayer { get; }
        public string Result { get; }

        public GiboTitleData(string fileName, DateTime dateTime, string blackPlayer, string whitePlayer, string result)
        {
            FileName = fileName;
            DateTime = dateTime;
            BlackPlayer = blackPlayer;
            WhitePlayer = whitePlayer;
            Result = result;
        }
    }
    
    private const int META_LINE_COUNT = 3;
    
    public static string GiboFileName { private get; set; }
    
    private static string GiboFolderPath { get; }
    private static string GiboFilePath => Path.Combine(GiboFolderPath, GiboFileName);
    private static bool IsGiboFolderExists => Directory.Exists(GiboFolderPath);
    private static bool IsGiboFileExists => File.Exists(GiboFilePath);
    /* GiboFileName이 생성자 초기화값(String.Empty) 그대로라면?
     * IsGiboFileExists는 File.Exists(Path.Combine(GiboFolderPath, ""))와 동일
     * 이는 File.Exists(GiboFolderPath)와 동일하게 평가되며,
     * GiboFolderPath의 폴더가 실제로 존재하더라도 false로 평가됨. (파일이 존재하는 게 아니므로)
     * 따라서 GiboFileName이 따로 입력되지 않았다면 IsGiboFileExists는 false임 */
    
    public static event Action<string> FailedSaveRecord;
    
    static GiboFileManager()
    {
        GiboFileName = String.Empty;
        
#if UNITY_STANDALONE || UNITY_EDITOR
        GiboFolderPath = Path.Combine(Environment.CurrentDirectory, "Gibo");
#elif UNITY_ANDROID
        GiboFolderPath = Path.Combine(Application.persistentDataPath, "Gibo");
#endif
    }

    // goto 대안 1 : 함수 초입에 모든 변수 default로 초기화 (**)
    // goto 대안 2 : 모든 매개변수 struct로 묶어서 하나로 만들기 (*****)
    // goto 대안 3 : 콜백 구조 (??)
    public static bool TryReadGiboFile(out (int row, int col)[] moveStoneData, out DateTime dateTime,
        out BasicPlayerData blackData, out BasicPlayerData whiteData, out string result)
    {
        if (IsGiboFileExists is false)
        {
            Debug.LogError("지정된 기보 파일이 존재하지 않음!");
            goto ReadFailed;
        }
        
        string[] giboFileLines = File.ReadAllLines(GiboFilePath);

        if (giboFileLines.Length <= META_LINE_COUNT)
        {
            Debug.LogError("기보 파일의 길이가 충분하지 않음!");
            goto ReadFailed;
        }
        
        #region 첫 번째 줄 처리 : dateTime, 총 수순, 결과
        string[] firstLine = giboFileLines[0].Split(',');

        if (firstLine.Length != 3 || int.TryParse(firstLine[1], out int lastTurn) is false ||
            lastTurn <= 0 || Board.BoardSize * Board.BoardSize < lastTurn)
        {
            Debug.LogError("기보 파일의 메타 정보 양식이 잘못되었음!");
            goto ReadFailed;
        }
        
        if (giboFileLines.Length != lastTurn + META_LINE_COUNT)
        {
            Debug.LogError("기보 파일의 길이가 메타 정보와 일치하지 않음!");
            goto ReadFailed;
        }
        
        if (DateTime.TryParse(firstLine[0], out dateTime) is false)
        {
            Debug.LogError("기보 파일의 dateTime 양식이 올바르지 않음");
            goto ReadFailed;
        }

        result = firstLine[2];
        #endregion
        
        # region 흑백 유저 정보 처리 : 2~3번째 줄
        string[] blackInforms = giboFileLines[1].Split(',');
        string[] whiteInforms = giboFileLines[2].Split(',');

        if (blackInforms.Length != 5 || whiteInforms.Length != 5)
        {
            Debug.LogError("유저 정보 양식이 올바르지 않음!");
            goto ReadFailed;
        }

        if (uint.TryParse(blackInforms[1], out uint blackWin) && uint.TryParse(blackInforms[2], out uint blackDraw) &&
            uint.TryParse(blackInforms[3], out uint blackLose) && float.TryParse(blackInforms[4], out float blackRating) &&
            uint.TryParse(whiteInforms[1], out uint whiteWin)  && uint.TryParse(whiteInforms[2], out uint whiteDraw) &&
            uint.TryParse(whiteInforms[3], out uint whiteLose) && float.TryParse(whiteInforms[4], out float whiteRating))
        {
            blackData = new BasicPlayerData(blackInforms[0], blackWin, blackDraw, blackLose, blackRating);
            whiteData = new BasicPlayerData(whiteInforms[0], whiteWin, whiteDraw, whiteLose, whiteRating);
        }
        else
        {
            Debug.LogError("유저 정보 양식이 올바르지 않음!");
            goto ReadFailed;
        }
        #endregion
        
        #region 착수 정보 처리 : 4번째 줄 이후
        moveStoneData = new (int row, int col)[lastTurn];

        for (int turn = 0; turn < lastTurn; ++turn)
        {
            string[] turnTexts = giboFileLines[turn + META_LINE_COUNT].Split(',');
            if (turnTexts.Length == 2 &&
                int.TryParse(turnTexts[0], out int row) && int.TryParse(turnTexts[1], out int col) &&
                0 <= row && row <= Board.MaxCoord && 0 <= col && col <= Board.MaxCoord)
            {
                moveStoneData[turn] = (row, col);
            }
            else
            {
                Debug.LogError("좌표 양식이 잘못되었음!");
                goto ReadFailed;
            }
        }
        #endregion
        
        return true;
        
        ReadFailed:
        moveStoneData = null;
        dateTime = default;
        blackData = default;
        whiteData = default;
        result = null;
        return false;
    }
    
    public static void ReadGiboList(out List<GiboTitleData> giboList)
    {
        giboList = new List<GiboTitleData>();
        
        try
        {
            if (IsGiboFolderExists is false) return;

            string[] giboFiles = Directory.GetFiles(GiboFolderPath, "*.gibo");
            // 읽기 실패하는 기보 파일이 있을 수 있으므로 List 자료구조가 유효함
            giboList.Capacity = giboFiles.Length;

            foreach (string fileName in giboFiles)
            {
                using StreamReader reader = new StreamReader(fileName);
                
                string titleLine = reader.ReadLine();
                string blackInformLine = reader.ReadLine();
                string whiteInformLine = reader.ReadLine();

                if (titleLine == null || blackInformLine == null || whiteInformLine == null)
                    continue;

                string[] titleInforms = titleLine.Split(',');
                string[] blackInforms = blackInformLine.Split(',');
                string[] whiteInforms = whiteInformLine.Split(',');

                if (titleInforms.Length != 3 || blackInforms.Length != 5 || whiteInforms.Length != 5 ||
                    DateTime.TryParse(titleInforms[0], out DateTime dateTime) is false)
                    continue;

                giboList.Add(new GiboTitleData(fileName, dateTime,
                    blackPlayer: blackInforms[0], whitePlayer: whiteInforms[0], result: titleInforms[2]));
            }
            
            // 최신 기보를 앞쪽으로 정렬
            giboList.Sort(comparison: (a,b) => b.DateTime.CompareTo(a.DateTime));
        }
        catch (Exception e)
        {
            Debug.LogError($"기보 리스트 읽기 실패! : {e}");
            /* 읽어오던 도중에 예외가 터지면 그 전까지 읽었던 내용들은 유지됨
             * giboList를 null로 바꿔주지 않는 것은 의도된 설계
             * Why? 성공적으로 읽은 부분들은 사용자에게 보여줘도 문제되지 않음 */
        }
    }
    
    public static void CreateGiboFile(List<(int row, int col)> record, BasicPlayerData blackData, BasicPlayerData whiteData, string result)
    {
        try
        {
            // 기보 폴더가 없으면 생성
            if (IsGiboFolderExists is false)
            {
                Directory.CreateDirectory(GiboFolderPath);
            }

            GiboFileName = $"{DateTime.Now:yyMMddHHmmss}-{blackData.name},{whiteData.name}.gibo";

            if (IsGiboFileExists)
            {
                Debug.LogError("지정된 이름의 기보 파일이 이미 존재함!");
                FailedSaveRecord?.Invoke("Gibo File Name Duplicated");
                return;
            }
            
            // finally 블럭에서 Dispose를 적는 대신 using으로 처리
            using StreamWriter writer = new StreamWriter(GiboFilePath);
            
            writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{record.Count},{result}");
            writer.WriteLine($"{blackData.name},{blackData.win},{blackData.draw},{blackData.lose},{blackData.rating}");
            writer.WriteLine($"{whiteData.name},{whiteData.win},{whiteData.draw},{whiteData.lose},{whiteData.rating}");

            foreach ((int row, int col) in record)
            {
                writer.WriteLine($"{row},{col}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"기보 저장 실패! : {e}");
            FailedSaveRecord?.Invoke("Gibo File Save Failed");
        }
    }

    /// <summary> 현재 세팅되어 있는 GiboFileName 파일을 삭제 </summary>
    /// <returns>
    /// <para> true: 해당 파일이 이미 존재하지 않거나 삭제에 성공함 </para>
    /// <para> false: 삭제 시도 중 예외 발생 </para>
    /// </returns> 
    public static bool TryDeleteGiboFile()
    {
        try
        {
            if (IsGiboFileExists) File.Delete(GiboFilePath);
            else Debug.LogWarning("이미 파일이 존재하지 않음!");
            
            // 예외 발생 이외에는 모두 true 반환
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"삭제 과정에서 예외 발생: {e}");
            return false;
        }
    }
}
