public class UIButton : UIClickable {

	public System.Action onPressed;

	protected override void OnPressed() {
		onPressed.Invoke();
	}
}
