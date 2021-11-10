using Sandbox;

namespace Chess
{
	public partial class ChessPlayer : Player
	{
		private bool Pressed = false;
		private bool PressedReload = false;
		public ChessPiece PromotingPiece { get; set; }

		[Net]
		public ChessPiece King { get; set; }

		[Net]
		public int Team { get; set; } = 0;

		public override void Respawn()
		{
			SetModel( "models/citizen/citizen.vmdl" );

			Controller = new WalkController();
			Animator = new StandardPlayerAnimator();
			Camera = new ChessCamera();

			base.Respawn();
		}

		[ClientRpc]
		public void SetPromotionScreen( bool active )
		{
			Event.Run( "SetPromotionScreen", active );
		}

		public override void Simulate( Client cl )
		{
			base.Simulate( cl );

			SimulateActiveChild( cl, ActiveChild );

			if ( IsClient )
			{
				if (Input.Down( InputButton.Reload ) )
				{
					if ( !PressedReload ) {
						PressedReload = true;

						ChessCamera cam = Camera as ChessCamera;

						var next = cam.CameraMode + 1;
						cam.CameraMode = next > 2 ? 0 : next;
					}
				} else
				{
					PressedReload = false;
				}

				if ( Input.Down( InputButton.Attack1 ) )
				{
					if ( !Pressed )
					{
						Pressed = true;

						var game = ChessGame.Current;
						var ply = Local.Pawn as ChessPlayer;

						game.UnmarkCells();

						GridBox HoveredCell = game.HoveredCell;

						if ( HoveredCell == null )
						{
							game.SelectedCell = null;
							return;
						}	

						ChessPiece ent = game.GetOccupant( HoveredCell.upint, HoveredCell.sideint );

						var sel_cell = game.SelectedCell;

						var KingsDanger = ply.King?.InDanger();

						bool CanSaveKing = KingsDanger.IsValid() && (ent.IsValid() && ent.CanSaveKing());

						if ( sel_cell == null && ent.IsValid() && ent.Team != ply.Team || game.TeamTurn != ply.Team || (KingsDanger.IsValid() && ent.IsValid() && !CanSaveKing && (ent.PieceType != 6 || ent.GetSafeMoves().Count <= 0)) )
						{
							game.SelectedCell = null;
							return;
						}

						if ( sel_cell != null )
						{
							ChessPiece sel_ent = game.GetOccupant( sel_cell.upint, sel_cell.sideint );

							if (sel_ent.IsValid() && sel_ent.CanMove( HoveredCell.upint, HoveredCell.sideint ) )
							{
								ConsoleSystem.Run( "make_move", sel_cell.upint, sel_cell.sideint, HoveredCell.upint, HoveredCell.sideint );

								game.SelectedCell = null;

								return;
							}
						}

						game.SelectedCell = HoveredCell;

						game.SetMarkedCell( HoveredCell.upint, HoveredCell.sideint, true );
						
						if ( ent.IsValid() )
						{
							if (ent.PieceType == 6 )
							{
								ent.GetSafeMoves( true );
							}
							else
							{
								if ( CanSaveKing ) {
									game.SetMarkedCell( KingsDanger.UpInt, KingsDanger.SideInt, true, true );
								} 
								else
								{
									ent.GetMoves( true );
								}
							}
						}
					}
				}
				else
				{
					Pressed = false;
				}
			}
		}
	}
}
