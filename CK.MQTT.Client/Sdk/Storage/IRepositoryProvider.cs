namespace CK.MQTT.Sdk.Storage
{
	internal interface IRepositoryProvider
	{
		IRepository<T> GetRepository<T> () where T : IStorageObject;
	}
}
