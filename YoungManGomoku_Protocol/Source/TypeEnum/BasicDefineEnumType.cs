namespace YoungManGomoku_Protocol.TypeEnum.PlayerData
{
	public enum ItemType
	{
		None = 0,
		ProfileImage = 1,
		BoardSkin = 2,
		StoneSkin = 3,
		MAXCOUNT
	}

	public enum ProfileImageType
    {
        None,
        StudentBoy,
        StudentGirl,
        GentleMan,
        Maam,
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
        GameMaster, // GM
        Adminstrator
    }
}
