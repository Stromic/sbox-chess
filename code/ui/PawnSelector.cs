namespace Chess
{
	using Sandbox;
	using Sandbox.UI;
	using Sandbox.UI.Construct;

	public class PawnSelector : Panel
	{
		public PawnSelector()
		{
			StyleSheet.Load( "/ui/PawnSelector.scss" );

			var container = Add.Panel( "pawnselect" );
			container.Add.Label( "Well-Deserved Promotion", "button-title" );

			var button_container = container.Add.Panel("button-container");
			button_container.Add.ButtonWithConsoleCommand( "Queen", "promote_piece 5" );
			button_container.Add.ButtonWithConsoleCommand( "Bishop", "promote_piece 4" );
			button_container.Add.ButtonWithConsoleCommand( "Rook", "promote_piece 2" );
			button_container.Add.ButtonWithConsoleCommand( "Knight", "promote_piece 3" );
		}

		[Event( "SetPromotionScreen" )]
		public void SetPromotionScreen( bool active )
		{
			SetClass( "active", active );
		}

		public override void Tick()
		{
			base.Tick();
		}
	}
}
