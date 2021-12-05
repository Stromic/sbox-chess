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

		[Net, Change]
		public int TeamTurn { get; set; } = 1;
		
		[Net]
		public bool Playing { get; set; }

		[Net]
		public ChessPiece white_king { get; set; }

		[Net]
		public ChessPiece black_king { get; set; }

		private ChessPiece LastMoved { get; set; }

		private bool debugging = false;

		public void OnTeamTurnChanged( int oldval, int newval )
		{
			if ( IsClient )
				return;

			WinnerCheck();
		}

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
					var pos = new Vector3( -420f + (120f * iup), 420f - (120f * iside), 1293f );

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
						var cell = new GridBox() { Position = new Vector3( -420f + (120f * iup), 420f - (120f * iside), 1293f ), upint = iup + 1, sideint = iside + 1 };

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
			return (PiecePositions[up][side]);
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

		public bool IsCellMarked( int up, int side )
		{
			if ( up <= 0 && side <= 0 )
				return false;

			var cell = ChessGame.Current.Cells[up][side];

			return cell.Marked;
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
				var cur_up = ent.virtualized_up > 0 ? ent.virtualized_up : ent.UpInt;
				var cur_side = ent.virtualized_side > 0 ? ent.virtualized_side : ent.SideInt ;

				if ( cur_up == up && cur_side == side && !ent.Killed )
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

			if ( black )
				piece.SetBlack();

			piece.SetCell( cell_up, cell_side );
			piece.SetType( type );
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

		public void Notify(string msg)
		{
			foreach ( ChessPlayer ply in Entity.All.OfType<ChessPlayer>() )
			{
				ply.DoNotify( To.Single(ply), msg );
			}
		}
		
		public void ResetGame()
		{
			SpawnPieces();
			Playing = false;
			TeamTurn = 1;
			white_player = null;
			black_player = null;
		}

		public async void WinnerCheck()
		{
			await Task.Delay(100);

			var endGame = false;

			foreach ( var king in Entity.All.OfType<ChessPiece>() )
			{
				if ( king.PieceType != 6 )
					continue;

				var hasMoves = king.GetSafeMoves().Count > 0;
				var canBeSaved = false;
				var inCheck = king.IsDangered;

				if ( hasMoves )
					continue;

				foreach ( var piece in Entity.All.OfType<ChessPiece>() )
				{
					if ( piece.Team != king.Team || king == piece )
						continue;

					var shieldMoves = piece.CanShieldKingMoves();
					if ( shieldMoves.Count > 0 || piece.CanSaveKing() )
					{
						canBeSaved = true;
						break;
					}
				}

				if ( !hasMoves && !canBeSaved )
				{
					if ( inCheck )
					{
						Notify( $"Checkmate! {(king.Team == 1 ? "Black" : "White")} team has won." );
					}
					else
					{
						Notify( $"Stalemate! Tied." );
					}

					endGame = true;
					break;
				}
			}

			if ( endGame )
			{
				ResetGame();
			}
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
				{
					Particles smoke = Particles.Create( "particles/explosion_smoke.vpcf" );
					smoke.SetPosition(0, occupant.Position);
					Sound.FromEntity( "chess_breakpiece", occupant );

					occupant.Kill(); 
				}

				ent.SetCell( up, side );

				foreach ( var piece in Entity.All.OfType<ChessPiece>() )
				{
					if ( piece.PieceType != 6 )
						continue;

					piece.IsDangered = piece.InDanger().IsValid();

					piece.GlowActive = piece.IsDangered;
					piece.GlowColor = Color.Red;

				}

				if ( !ent.HandleAfterMove() )
					return;

				game.TeamTurn = game.TeamTurn == 1 ? 2 : 1;

				if ( game.LastMoved.IsValid())
                {
					game.LastMoved.GlowActive = false;
				}

				game.LastMoved = ent;
				game.LastMoved.GlowActive = true;
				game.LastMoved.GlowColor = "#34a1eb";
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

			if ( game.white_player.IsValid() && game.black_player.IsValid() )
			{
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

			game.ResetGame();
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

			foreach ( var ent in Entity.All.OfType<ChessPiece>() )
			{
				if ( ent.PieceType != 6 )
					continue;

				ent.IsDangered = ent.InDanger().IsValid();

				ent.GlowActive = ent.IsDangered;
				ent.GlowColor = Color.Red;

			}

			game.TeamTurn = game.TeamTurn == 1 ? 2 : 1;
		}

		// TEST CMDS
		[ServerCmd( "toggle_playing" )]
		public static void TogglePlaying()
		{
			var game = ChessGame.Current;

			if ( !game.debugging )
				return;

			game.Playing = !ChessGame.Current.Playing;
		}

		[ServerCmd( "notify" )]
		public static void Notifitest(string msg)
		{
			var game = ChessGame.Current;

			if ( !game.debugging )
				return;

			game.Notify(msg);
		}

		[ServerCmd( "test_checkmate" )]
		public static void CheckMateTest()
		{
			var game = ChessGame.Current;

			if ( !game.debugging )
				return;

			game.WinnerCheck();
		}

		[ServerCmd( "promotion_test" )]
		public static void PromotionTest(int todo)
		{
			var game = ChessGame.Current;

			if ( !game.debugging )
				return;

			var client = ConsoleSystem.Caller;
			var pawn = client?.Pawn as ChessPlayer;

			pawn.SetPromotionScreen( Convert.ToBoolean(todo) );
		}

		[Event.Hotload]
		public void TestShit()
		{
			var startPos = new Vector3(0f,0f,2000f);
			var endPos = new Vector3( 0f, 0f, 0f );

			var tr = Trace.Ray( startPos, endPos ).Run();

			Log.Info(tr.EndPos);
		}
	}
}
