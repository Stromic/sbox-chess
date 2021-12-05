namespace Chess
{
	using Sandbox;
	using Sandbox.UI;
	using Sandbox.UI.Construct;
	using System.Linq;

	public class HUD : Panel
	{
		private Label black_score { get; set; }
		private Label white_score { get; set; }
		private Button forfeit { get; set; }
		private bool forfeitPressed = false;
		private Label whos_turn { get; set; }

		public HUD()
		{
			StyleSheet.Load( "/ui/HUD.scss" );

			var whiteside = Add.Panel();
			whiteside.AddClass( "scores" );
			whiteside.Add.Label( "White" );
			white_score = whiteside.Add.Label( "16", "score" );

			var controls = Add.Panel( "controls" );
			whos_turn = controls.Add.Label( "White's Turn!", "turn" );
			forfeit = controls.Add.ButtonWithConsoleCommand( "Forfeit", "forfeit" );

			var blackside = Add.Panel();
			blackside.AddClass( "scores" );
			blackside.Add.Label( "Black" );
			black_score = blackside.Add.Label( "16", "score" );
		}

		public override void Tick()
		{
			int amountWhite = 0;
			int amountBlack = 0;

			foreach ( ChessPiece ent in Entity.All.OfType<ChessPiece>() ) // Maybe optimize?
			{
				if ( ent.Killed )
					continue;

				if ( ent.Team == 2 )
				{ amountBlack = amountBlack + 1; }
				else
				{ amountWhite = amountWhite + 1; }
			}

			if ( Input.Down( InputButton.Attack1 ) ) // pointer-events didnt seem to work as intented?
			{
				if ( forfeit.HasHovered && !forfeitPressed )
				{
					forfeitPressed = true;

				}
			}
			else
			{
				if ( forfeitPressed && forfeit.HasHovered )
				{
					ConsoleSystem.Run( "forfeit" );
				}

				forfeitPressed = false;
			}

			whos_turn.SetText( (ChessGame.Current.TeamTurn == 1 ? "White" : "Black") + "'s Turn!" );

			white_score.SetText( amountWhite.ToString() );
			black_score.SetText( amountBlack.ToString() );

			SetClass( "hud-visible", ChessGame.Current.Playing );

			base.Tick();
		}
	}
}
