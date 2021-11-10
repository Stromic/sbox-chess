namespace Chess
{
	using Sandbox;
	using Sandbox.UI;
	using Sandbox.UI.Construct;

	public class Controls : Panel
	{
		private Button select_white { get; set; }
		private Button select_black { get; set; }
		private Panel container;

		public Controls()
		{
			StyleSheet.Load( "/ui/Controls.scss" );

			Add.Label( "< Select a Team", "select-team" );

			container = Add.Panel( "controls-container" );

			container.Add.Label( "Controls", "overhead" );
			select_white = container.Add.ButtonWithConsoleCommand( "Select White", "select_team 1" );
			select_black = container.Add.ButtonWithConsoleCommand( "Select Black", "select_team 2" );
		}

		public override void Tick()
		{
			SetClass("hide", ChessGame.Current.Playing );
			container.SetClass( "controls-visible", !ChessGame.Current.Playing );

			select_white.SetClass( "selected", ChessGame.Current.white_player.IsValid() );
			select_black.SetClass( "selected", ChessGame.Current.black_player.IsValid() );


			base.Tick();
		}
	}
}
