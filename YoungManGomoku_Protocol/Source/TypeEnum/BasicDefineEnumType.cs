namespace YoungManGomoku_Protocol.Source.TypeEnum
{
    public enum Stone
    {
        Empty,
        Black,
        White
    }

    public enum ProfileImageType
    {
        None,
        StudentBoy,
        StudentGirl,
        GentleMan,
        Lady,
        GrandFather,
        GrandMather,
        MAXCOUNT
    }

    public enum StoneSkinType
    {
        None,
        // 추가바람
        MAXCOUNT
    }

    public enum BoardSkinType
    {
        None,
        // 추가바람
        MAXCOUNT
    }



    public enum AuthLevel
    {
        Ban = -1,
        Common,
        QA,
        GameMaster
    }
}
