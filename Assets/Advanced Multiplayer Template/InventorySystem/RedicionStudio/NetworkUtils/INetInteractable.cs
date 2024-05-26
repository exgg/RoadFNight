namespace RedicionStudio.NetworkUtils {

	public interface INetInteractable<T> {

		void OnServerInteract(T player);
		void OnClientInteract(T player);
		string GetInfoText();
	}
}
