namespace YoungManGomoku_WebServer.SingletoneManager.Interface
{
	public interface IUIDProvider
	{
		uint GenerateUID32();
		ulong GenerateUID64();
	}

}
