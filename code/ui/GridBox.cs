namespace Chess
{
	using Sandbox;
	using Sandbox.UI;

	public class GridBox : WorldPanel
	{
		public bool IsHovered { get; set; }
		public int upint { get; set; } = 1;
		public int sideint { get; set; } = 1;
		
		[Sandbox.Net]
		public bool Marked { get; set; }
		public int size = 2000;

		public GridBox()
		{
			StyleSheet.Load( "/ui/GridBox.scss" );

			PanelBounds = new Rect( (float)(-size * .5), (float)(-size * .5), size, size );

			Rotation = Rotation.From( 90f, 0f, 0f );

			AddClass( "world-grid" );
		}

		public override void Tick()
		{
			var tr = Trace.Ray( Input.Cursor, 3500 ).Run();

			var game = ChessGame.Current;
			var ply = Local.Pawn as ChessPlayer;

			IsHovered = game.Playing ? tr.EndPos.Distance( ChessGame.Current.GetPiecePosition( upint, sideint ) ) <= 60 : false;

			if ( IsHovered )
			{
				game.HoveredCell = this;
			}

			SetClass( "hovered", game.SelectedCell == this || IsHovered );

			if ( !Marked )
			{
				SetClass( "marked", IsHovered );
				SetClass( "kill", false );
			}

			var king = ply.King;
			if ( game.TeamTurn == ply.Team && king.IsValid() )
			{
				bool isKingGrid = (upint == king.UpInt && sideint == king.SideInt);

				if ( isKingGrid && king.IsDangered )
				{
					SetClass( "kill", true );
				}
			}

			if ( game.HoveredCell == this && !IsHovered )
			{
				game.HoveredCell = null;
			}

			base.Tick();
		}
	}
}
