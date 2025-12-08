namespace YoungManGomoku_WebServer
{
	public static class ServerManager
	{
		private static UIDGenerator uidGenerator;

		static ServerManager()
		{
			uidGenerator = new UIDGenerator();
		}

		public static uint GenerateUID32() => uidGenerator.GenerateUID32();
		
		public static ulong GenerateUID64() => uidGenerator.GenerateUID64();


	}
}
