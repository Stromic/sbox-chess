namespace Chess
{
	using Sandbox;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[Library( "chess", Title = "Chess" )]
	public partial class ChessGame : Game
	{
		public Dictionary<int, Dictionary<int, GridBox>> Cells;
		public Dictionary<int, Dictionary<int, Vector3>> PiecePositions;
		public Dictionary<int, Dictionary<int, ChessPiece>> CellOccupants;
		public GridBox HoveredCell { get; set; }
		public static new ChessGame Current { get; protected set; }
		public GridBox SelectedCell { get; set; }

		[Net]
		public ChessPlayer white_player { get; set; }
		[Net]
		public ChessPlayer black_player { get; set; }

		[Net]
		public int TeamTurn { get; set; } = 1;
		
		[Net]
		public bool Playing { get; set; }


		public ChessGame()
		{
			Current = this;
			Cells = new Dictionary<int, Dictionary<int, GridBox>>();
			PiecePositions = new Dictionary<int, Dictionary<int, Vector3>>();

			for ( int iup = 0; iup < 8; iup++ )
			{
				var dict = new Dictionary<int, Vector3>();

				for ( int iside = 0; iside < 8; iside++ )
				{
					var pos = new Vector3( -420f + (120f * iside), 420f - (120f * iup), 34f );

					dict[iside + 1] = pos;
				}

				PiecePositions[iup + 1] = dict;
			}

			if ( IsClient )
			{
				for ( int iup = 0; iup < 8; iup++ )
				{
					var dict = new Dictionary<int, GridBox>();

					for ( int iside = 0; iside < 8; iside++ )
					{
						var cell = new GridBox() { Position = new Vector3( -480f + (119.8f * iup), 479.9f - (119.8f * iside), 34f ), upint = iup + 1, sideint = iside + 1 };

						dict[iside + 1] = cell;
					}

					Cells[iup + 1] = dict;
				}

				new ChessUI();
			} else
			{
				SpawnPieces();
			}
		}

		public Vector3 GetPiecePosition( int up, int side )
		{
			return (PiecePositions[side][up]);
		}

		public void SetMarkedCell( int up, int side, bool enabled = false, bool kill = false )
		{
			if ( up <= 0 && side <= 0 )
				return;

			var cell = ChessGame.Current.Cells[up][side];
			cell.Marked = enabled;
			cell.SetClass( "marked", enabled );
			cell.SetClass( "kill", kill );
		}

		public GridBox GetCell( int up, int side )
		{
			return Cells[up][side];
		}

		public ChessPiece GetOccupant(int up, int side)
		{
			ChessPiece occupant = null;
			foreach ( ChessPiece ent in Entity.All.OfType<ChessPiece>())
			{
				if ( ent.UpInt == up && ent.SideInt == side )
				{
					occupant = ent;
					break;
				}
			}

			return occupant;
		}

		public bool IsCellOccupied( int up, int side )
		{
			if ( up > 8 || side > 8 || up < 1 || side < 1 )
				return true;

			ChessPiece ent = GetOccupant( up, side );

			return ent.IsValid();
		}

		public void UnmarkCells()
		{
			for ( int iup = 0; iup < 8; iup++ )
			{
				for ( int iside = 0; iside < 8; iside++ )
				{
					GridBox cell = Cells[iup + 1][iside + 1];
					cell.Marked = false;
				}
			}
		}

		private void SpawnPiece(int type, bool black, int cell_up, int cell_side)
		{
			var piece = Library.Create<Entity>( "ent_chesspiece" ) as ChessPiece;
			piece.SetCell( cell_up, cell_side );
			piece.SetType( type );

			//if (type == 6 )
			//{
			//	(black ? black_player : white_player).King = piece;
			//}

			if ( black )
				piece.SetBlack();
		}

		public void SpawnPieces()
		{
			foreach(var piece in Entity.All.OfType<ChessPiece>()){
				piece.Delete();
			}

			////////////////
			// Black Pieces
			////////////////
			
			// Pawns
			for ( int i = 0; i < 8; i++ )
			{
				SpawnPiece( 1, true, 2, i + 1 );
			}

			// Rooks
			SpawnPiece( 2, true, 1, 1 );
			SpawnPiece( 2, true, 1, 8 );

			// Knights
			SpawnPiece( 3, true, 1, 2 );
			SpawnPiece( 3, true, 1, 7 );

			// Bishops
			SpawnPiece( 4, true, 1, 3 );
			SpawnPiece( 4, true, 1, 6 );

			// King & Queen
			SpawnPiece( 5, true, 1, 4 );
			SpawnPiece( 6, true, 1, 5 );


			////////////////
			// White Pieces
			////////////////

			// Pawns
			for ( int i = 0; i < 8; i++ )
			{
				SpawnPiece( 1, false, 7, i + 1 );
			}

			// Rooks
			SpawnPiece( 2, false, 8, 1 );
			SpawnPiece( 2, false, 8, 8 );

			// Knights
			SpawnPiece( 3, false, 8, 2 );
			SpawnPiece( 3, false, 8, 7 );

			// Bishops
			SpawnPiece( 4, false, 8, 3 );
			SpawnPiece( 4, false, 8, 6 );

			// King & Queen
			SpawnPiece( 5, false, 8, 4 );
			SpawnPiece( 6, false, 8, 5 );
		}

		public override void ClientJoined( Client client )
		{
			base.ClientJoined( client );

			var player = new ChessPlayer();
			client.Pawn = player;

			player.Respawn();
		}

		[ServerCmd( "make_move" )]
		public static void MakeMove( int sel_up, int sel_side, int up, int side )
		{
			var client = ConsoleSystem.Caller;
			var pawn = client?.Pawn as ChessPlayer;

			if ( !pawn.IsValid() )
				return;

			var game = ChessGame.Current;
			ChessPiece ent = game.GetOccupant( sel_up, sel_side );
		
			if ( !ent.IsValid() )
				return;

			bool isTurn = game.TeamTurn == ent.Team;

			if ( ent.Team != pawn.Team)
				return;

			if ( isTurn && ent.CanMove( up, side ) )
			{
				ent.FirstMove = false;

				var occupant = game.GetOccupant( up, side );

				if ( occupant.IsValid() )
					occupant.Delete();

				ent.SetCell( up, side );

				if (!ent.HandleAfterMove())
					return;

				game.TeamTurn = game.TeamTurn == 1 ? 2 : 1;
			}
		}

		[ServerCmd( "select_team" )]
		public static void SelectTeam( int team )
		{
			var client = ConsoleSystem.Caller;
			var pawn = client?.Pawn as ChessPlayer;
			var game = ChessGame.Current;

			if ( !pawn.IsValid() || game.Playing )
				return;

			if ( game.white_player == pawn )
				game.white_player = null;

			if ( game.black_player == pawn )
				game.black_player = null;

			if ( !game.white_player.IsValid() && team == 1)
			{
				game.white_player = pawn;
				pawn.Team = team;
			}

			if ( !game.black_player.IsValid() && team == 2 )
			{
				game.black_player = pawn;
				pawn.Team = team;
			}

			if ( game.white_player.IsValid() && game.black_player.IsValid() || true)
			{
				//ChessGame.Current.SpawnPieces();
				game.Playing = true;
			}

			foreach ( var ent in Entity.All.OfType<ChessPiece>() )
			{
				if (ent.Team == pawn.Team && ent.PieceType == 6 )
				{
					pawn.King = ent;
					break;
				}
			}
		}

		[ServerCmd( "forfeit" )]
		public static void Forfeit()
		{
			var client = ConsoleSystem.Caller;
			var pawn = client?.Pawn as ChessPlayer;

			if ( !pawn.IsValid() )
				return;

			var game = ChessGame.Current;

			if ( game.white_player != pawn && game.black_player != pawn )
				return;

			game.white_player = null;
			game.black_player = null;
			game.Playing = false;
			game.TeamTurn = 1;
		}

		[ServerCmd( "promote_piece" )]
		public static void PromotePiece(int piece)
		{
			var client = ConsoleSystem.Caller;
			var pawn = client?.Pawn as ChessPlayer;
			var game = ChessGame.Current;

			if ( game.white_player != pawn && game.black_player != pawn || !pawn.PromotingPiece.IsValid() || piece == 1 || piece == 6)
				return;

			pawn.PromotingPiece.SetType( piece );

			pawn.SetPromotionScreen( false );

			game.TeamTurn = game.TeamTurn == 1 ? 2 : 1;
		}

		// TEST CMDS
		[ServerCmd( "toggle_playing" )]
		public static void TogglePlaying()
		{
			ChessGame.Current.Playing = !ChessGame.Current.Playing;
		}

		[ServerCmd( "promotion_test" )]
		public static void PromotionTest(int todo)
		{
			var client = ConsoleSystem.Caller;
			var pawn = client?.Pawn as ChessPlayer;

			pawn.SetPromotionScreen( Convert.ToBoolean(todo) );
		}
	}
}
