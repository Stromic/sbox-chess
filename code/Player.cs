using Sandbox;
using System.Collections.Generic;

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

			EnableDrawing = false; // TODO: Move the player into a chair and have it so they are sitting by a table playing chess.

			base.Respawn();
		}

		[ClientRpc]
		public void SetPromotionScreen( bool active )
		{
			Event.Run( "SetPromotionScreen", active );
		}

		[ClientRpc]
		public void DoNotify( string msg )
		{
			Event.Run( "ChessNotify", msg );
		}

		private int ConcatInt( int a, int b ) // Yeah very nice
		{
			return int.Parse( a.ToString() + b.ToString() );
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

						GridBox hoveredCell = game.HoveredCell;

						if ( hoveredCell == null )
						{
							game.SelectedCell = null;
							game.UnmarkCells();
							return;
						}

						bool isMarkedCell = game.IsCellMarked( hoveredCell.upint, hoveredCell.sideint );

						game.UnmarkCells();

						ChessPiece ent = game.GetOccupant( hoveredCell.upint, hoveredCell.sideint );

						bool isSameTeam = ent.IsValid() ? ent.Team == ply.Team : false;

						if ( isSameTeam )
							game.SelectedCell = null;

						var sel_cell = game.SelectedCell;
						ChessPiece sel_ent = sel_cell != null ? game.GetOccupant( sel_cell.upint, sel_cell.sideint ) : null;

						var KingsDanger = ply.King?.InDanger();

						var ShieldKingBlockers = KingsDanger.IsValid() && ent.IsValid() ? ent.CanShieldKingMoves() : null;
						bool canSaveKing = KingsDanger.IsValid() && (ent.IsValid() && (ent.CanSaveKing()));
						bool isMyTurn = game.TeamTurn == ply.Team;
						bool wantsToMove = sel_cell != null && isMarkedCell;
						bool canShieldKing = ShieldKingBlockers?.Count > 0;
						bool kingInDanger = KingsDanger.IsValid();
						bool isKing = ent.IsValid() ? ent.PieceType == 6 && ent.Team == ply.Team : false ;

						if (((ent.IsValid() && ent.PieceType == 6 && ent.GetSafeMoves().Count <= 0) || (kingInDanger && !canShieldKing && !canSaveKing) || !isSameTeam ) && (!wantsToMove && (!isKing || ent.GetSafeMoves().Count <= 0)) || !isMyTurn )
						{
							game.SelectedCell = null;
							return;
						}

						if ( canSaveKing )
							game.SetMarkedCell( KingsDanger.UpInt, KingsDanger.SideInt, true, true );

						if ( ent.IsValid() )
						{
							game.SelectedCell = hoveredCell;
							game.SetMarkedCell( hoveredCell.upint, hoveredCell.sideint, true );
						}

						if ( KingsDanger.IsValid() && ShieldKingBlockers?.Count > 0 )
						{
							foreach ( KeyValuePair<int, bool> move in ShieldKingBlockers )
							{
								string num_str = move.Key.ToString();
								int up = (int)(num_str[0]) - 48;
								int side = (int)(num_str[1]) - 48;

								game.SetMarkedCell( up, side, true, false );
							}

							return;
						}

						if ( sel_cell != null )
						{
							game.SelectedCell = null;

							if ( sel_ent.IsValid() && isMarkedCell )
							{
								ConsoleSystem.Run( "make_move", sel_cell.upint, sel_cell.sideint, hoveredCell.upint, hoveredCell.sideint );

								return;
							}
						}
						
						if ( ent.IsValid() )
						{
							if (ent.PieceType == 6 )
							{
								ent.GetSafeMoves( true );
							}
							else if ( !canSaveKing )
							{
								ent.GetMoves( true );
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
