namespace Chess
{
	using Sandbox;
	using Sandbox.UI;

	public partial class ChessUI : HudEntity<RootPanel>
	{
		public ChessUI()
		{
			RootPanel.AddChild<Controls>();
			RootPanel.AddChild<HUD>();
			RootPanel.AddChild<PawnSelector>();
			RootPanel.AddChild<Notifications>();
			RootPanel.AddChild<ChatBox>();
		}
	}
}
