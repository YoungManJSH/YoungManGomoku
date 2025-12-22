namespace YoungManGomoku_WebServer.SingletoneManager
{
	public interface IUIDProvider
	{
		uint GenerateUID32();
		ulong GenerateUID64();
	}

}
